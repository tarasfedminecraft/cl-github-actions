using CL_CLegendary_Launcher_.Windows;
using CmlLib.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CL_CLegendary_Launcher_.Class
{
    public class LastActionService
    {
        private readonly string _lastActionsPath;
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private CL_Main_ _main;

        public LastActionService(CL_Main_ main)
        {
            _main = main;
            _lastActionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "last_actions.json");
        }

        public async Task LoadLastActionsFromJsonAsync()
        {
            try
            {
                if (File.Exists(_lastActionsPath))
                {
                    string jsonContent = await File.ReadAllTextAsync(_lastActionsPath);
                    var actions = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonContent);

                    if (actions != null)
                    {
                        _main.Dispatcher.Invoke(() => _main.ServerMonitoring.Items.Clear());

                        foreach (var action in actions)
                        {
                            _main.AddActionToList(action); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Не вдалося згадати, що ми робили минулого разу.\n" +
                                    $"Файл історії, схоже, пошкоджений.\n\nДеталі: {ex.Message}",
                                    "Забудькуватість",
                                    MascotEmotion.Sad);
            }
        }

        public async Task AddLastActionAsync(Dictionary<string, string> action)
        {
            await _fileLock.WaitAsync();
            try
            {
                var actions = new List<Dictionary<string, string>>();

                if (File.Exists(_lastActionsPath))
                {
                    var jsonContent = await File.ReadAllTextAsync(_lastActionsPath);
                    actions = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonContent) ?? new List<Dictionary<string, string>>();
                }

                actions.Add(action);

                if (actions.Count > 5)
                {
                    actions = actions.Skip(actions.Count - 5).ToList();
                }

                var updatedJson = JsonConvert.SerializeObject(actions, Formatting.Indented);
                await File.WriteAllTextAsync(_lastActionsPath, updatedJson);

                _main.Dispatcher.Invoke(() =>
                {
                    _main.ServerMonitoring.Items.Clear();
                    foreach (var act in actions)
                        _main.AddActionToList(act);
                });
            }
            catch (Exception ex)
            {
                MascotMessageBox.Show(
                                    $"Ой! Я намагалася записати цю дію в історію, але щось пішло не так.\n{ex.Message}",
                                    "Помилка запису",
                                    MascotEmotion.Confused);
            }
            finally
            {
                _fileLock.Release();
            }
        }
    } 
}
