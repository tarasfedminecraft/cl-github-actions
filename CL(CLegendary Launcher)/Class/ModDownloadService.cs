using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using CurseForge.APIClient.Models.Files;
using CurseForge.APIClient.Models.Mods;
using CurseForge.APIClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;
using System.Net.Http;

namespace CL_CLegendary_Launcher_.Class
{
    public class ModSearchResult
    {
        public string ModId { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string Author { get; set; }
        public string Downloads { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedDate { get; set; }
        public string Site { get; set; }
        public int CF_FileId { get; set; }
    }

    public class ModVersionInfo
    {
        [JsonProperty("project_id")]
        public string ModId { get; set; }
        [JsonProperty("id")]
        public string VersionId { get; set; }
        [JsonProperty("version_number")]
        public string VersionName { get; set; }
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
        public string Site { get; set; }
        [JsonProperty("game_versions")]
        public List<string> GameVersions { get; set; } = new List<string>();
        [JsonProperty("loaders")]
        public List<string> Loaders { get; set; } = new List<string>();
        [JsonProperty("version_type")]
        public string VersionType { get; set; }
    }

    public class ModDownloadService
    {
        public CL_Main_ _main;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        private static ApiClient _cfApiClient;

        private static readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(5);
        private readonly JsonSerializerSettings _modrinthSettings;

        static ModDownloadService()
        {
            _cfApiClient = new ApiClient(Secrets.CurseForgeKey);
        }

        public ModDownloadService(CL_Main_ main)
        {
            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"CL-Legendary-Launcher/{Assembly.GetExecutingAssembly().GetName().Version}");
            }

            _modrinthSettings = new JsonSerializerSettings();
            _modrinthSettings.Converters.Add(new ModrinthVersionConverter());
            _main = main;
        }
        public async Task<List<ModSearchResult>> SearchModsAsync(string query, string site, string loader, int modType, int offset = 0)
        {
            if (site == "Modrinth")
                return await SearchModrinthAsync(query, loader, modType, offset);
            else
                return await SearchCurseForgeAsync(query, loader, modType, offset);
        }

        public async Task<List<ModVersionInfo>> GetVersionsAsync(ModSearchResult mod)
        {
            if (mod.Site == "Modrinth")
                return await GetModrinthVersionsAsync(mod.ModId);
            else
                return await GetCurseForgeVersionsAsync(int.Parse(mod.ModId));
        }

        public async Task DownloadModWithDependenciesAsync(
            ModVersionInfo version,
            int modType,
            CancellationToken token,
            IProgress<int> progress,
            string? customDestinationPath = null)
        {
            try
            {
                string modsFolder;

                if (string.IsNullOrEmpty(customDestinationPath))
                {
                    string folderName = modType switch
                    {
                        1 => "shaderpacks",
                        2 => "resourcepacks",
                        3 => "saves",
                        4 => "datapacks",
                        _ => "mods"
                    };
                    modsFolder = Path.Combine(Settings1.Default.PathLacunher, folderName);
                }
                else
                {
                    modsFolder = customDestinationPath;
                }

                Directory.CreateDirectory(modsFolder);

                if (string.IsNullOrEmpty(version.FileName) || string.IsNullOrEmpty(version.DownloadUrl))
                {
                    throw new Exception($"Не вдалося отримати ім'я файлу або URL. Можливо, файл було видалено.");
                }

                string mainFilePath = Path.Combine(modsFolder, version.FileName);

                if (!File.Exists(mainFilePath))
                {
                    await DownloadFileHelperAsync(version.DownloadUrl, mainFilePath, token, progress);
                }
                progress?.Report(100);

                if (modType == 3 || modType == 4) return;
                if (!Settings1.Default.ModDep) return;

                var dependencyUrls = await GetDependencyUrlsAsync(version);
                if (dependencyUrls == null || !dependencyUrls.Any()) return;

                List<Task> downloadTasks = new List<Task>();

                foreach (string url in dependencyUrls.Distinct())
                {
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        await _downloadSemaphore.WaitAsync(token);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            string depFileName = Path.GetFileName(new Uri(url).AbsolutePath);
                            string depFilePath = Path.Combine(modsFolder, depFileName);

                            if (!File.Exists(depFilePath))
                            {
                                await DownloadFileHelperAsync(url, depFilePath, token, null);
                            }
                        }
                        catch {  }
                        finally
                        {
                            _downloadSemaphore.Release(); 
                        }
                    }, token));
                }

                await Task.WhenAll(downloadTasks);
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка під час завантаження мода: {ex.Message}");
            }
        }

        #region Modrinth Logic
        private async Task<List<ModSearchResult>> SearchModrinthAsync(string query, string loader, int modType, int offset = 0)
        {
            string projectType = modType switch
            {
                1 => "shader",
                2 => "resourcepack",
                3 => "null",
                4 => "datapacks",
                _ => "mod"
            };

            var facets = new List<string>();

            if (modType == 4)
            {
                facets.Add("[%22project_type:mod%22]");
                facets.Add("[%22categories:datapack%22]");
            }
            else
            {
                facets.Add($"[%22project_type:{projectType}%22]");
                if (projectType == "mod")
                {
                    facets.Add($"[%22categories:{loader.ToLower()}%22]");
                }
            }

            string facetsString = string.Join(",", facets);
            string url = $"https://api.modrinth.com/v2/search?query={Uri.EscapeDataString(query)}&offset={offset}&facets=[{facetsString}]&limit=10";

            var response = await _httpClient.GetStringAsync(url);
            var result = JObject.Parse(response);
            var hits = result["hits"] as JArray;

            var list = new List<ModSearchResult>();
            if (hits == null) return list;

            foreach (var mod in hits)
            {
                list.Add(new ModSearchResult
                {
                    ModId = mod["project_id"]?.ToString(),
                    Slug = mod["slug"]?.ToString(),
                    Title = mod["title"]?.ToString(),
                    Description = mod["description"]?.ToString(),
                    IconUrl = mod["icon_url"]?.ToString(),
                    Author = mod["author"]?.ToString(),
                    Downloads = mod["downloads"]?.ToString(),
                    UpdatedDate = mod["date_modified"]?.ToString(),
                    CreatedDate = mod["date_created"]?.ToString(),
                    Site = "Modrinth"
                });
            }
            return list;
        }

        private async Task<List<ModVersionInfo>> GetModrinthVersionsAsync(string modId)
        {
            string url = $"https://api.modrinth.com/v2/project/{modId}/version";
            var response = await _httpClient.GetStringAsync(url);

            var versions = JsonConvert.DeserializeObject<List<ModVersionInfo>>(response, _modrinthSettings);

            if (versions == null) return new List<ModVersionInfo>();

            return versions
                .Where(v => v.VersionType == "release" || v.VersionType == "beta")
                .ToList();
        }
        #endregion

        #region CurseForge Logic
        private async Task<List<ModSearchResult>> SearchCurseForgeAsync(string query, string loader, int modType, int offset = 0)
        {
            var modLoaderType = loader switch
            {
                "Forge" => ModLoaderType.Forge,
                "Fabric" => ModLoaderType.Fabric,
                "Quilt" => ModLoaderType.Quilt,
                "NeoForge" => ModLoaderType.NeoForge,
                _ => ModLoaderType.Any
            };

            if (modType == 3 || modType == 4) modLoaderType = ModLoaderType.Any;

            int classId = modType switch
            {
                1 => 6552,
                2 => 12,
                3 => 17,
                4 => 6945,
                _ => 6
            };

            var response = (classId == 6)
                ? await _cfApiClient.SearchModsAsync(
                    gameId: 432, classId: classId, searchFilter: query,
                    modLoaderType: modLoaderType, pageSize: 10, sortField: ModsSearchSortField.Popularity, index: offset
                )
                : await _cfApiClient.SearchModsAsync(
                    gameId: 432, classId: classId, searchFilter: query,
                    pageSize: 10, sortField: ModsSearchSortField.Popularity, index: offset
                );

            var list = new List<ModSearchResult>();
            if (response?.Data == null) return list;

            foreach (var mod in response.Data)
            {
                list.Add(new ModSearchResult
                {
                    ModId = mod.Id.ToString(),
                    Slug = mod.Slug,
                    Title = mod.Name,
                    Description = mod.Summary,
                    IconUrl = mod.Logo?.Url,
                    Author = mod.Authors?.FirstOrDefault()?.Name,
                    Downloads = _main.FormatNumber(mod.DownloadCount.ToString()),
                    UpdatedDate = mod.DateModified.ToString("g"),
                    CreatedDate = mod.DateCreated.ToString("g"),
                    Site = "CurseForge",
                    CF_FileId = mod.MainFileId
                });
            }
            return list;
        }

        private async Task<List<ModVersionInfo>> GetCurseForgeVersionsAsync(int modId)
        {
            var response = await _cfApiClient.GetModFilesAsync(modId);
            var list = new List<ModVersionInfo>();
            if (response?.Data == null) return list;

            var releaseFiles = response.Data
                .Where(file => file.ReleaseType == FileReleaseType.Release ||
                               file.ReleaseType == FileReleaseType.Beta);

            foreach (var file in releaseFiles)
            {
                list.Add(new ModVersionInfo
                {
                    ModId = modId.ToString(),
                    VersionId = file.Id.ToString(),
                    VersionName = file.DisplayName,
                    FileName = file.FileName,
                    DownloadUrl = file.DownloadUrl,
                    Site = "CurseForge",
                    GameVersions = file.GameVersions.ToList(),
                    Loaders = file.GameVersions
                                  .Where(gv => gv == "Forge" || gv == "Fabric" || gv == "Quilt" || gv == "NeoForge")
                                  .Select(l => l.ToLower()).ToList(),
                    VersionType = file.ReleaseType.ToString()
                });
            }
            return list;
        }
        #endregion

        #region Dependency Logic
        private async Task<List<string>> GetDependencyUrlsAsync(ModVersionInfo parentMod)
        {
            if (parentMod.Site == "Modrinth")
                return await GetModrinthDependencyUrls(parentMod);
            else
                return await GetCurseForgeDependencyUrls(parentMod);
        }

        private async Task<List<string>> GetModrinthDependencyUrls(ModVersionInfo parentMod)
        {
            var urls = new List<string>();
            string gameVersion = parentMod.GameVersions.FirstOrDefault();
            string loader = parentMod.Loaders.FirstOrDefault();

            if (string.IsNullOrEmpty(loader) || string.IsNullOrEmpty(gameVersion))
                return urls;

            string url = $"https://api.modrinth.com/v2/version/{parentMod.VersionId}";
            var response = await _httpClient.GetStringAsync(url);
            var versionData = JObject.Parse(response);

            var dependencies = versionData["dependencies"] as JArray;
            if (dependencies == null) return urls;

            foreach (var dep in dependencies)
            {
                if (dep["dependency_type"]?.ToString() == "required")
                {
                    string depProjectId = dep["project_id"]?.ToString();
                    if (depProjectId == null) continue;

                    string depUrl = $"https://api.modrinth.com/v2/project/{depProjectId}/version?loaders=[%22{loader}%22]&game_versions=[%22{gameVersion}%22]";
                    try
                    {
                        var depResponse = await _httpClient.GetStringAsync(depUrl);
                        var depVersions = JArray.Parse(depResponse);

                        if (depVersions.Count > 0)
                        {
                            var fileUrl = depVersions.OfType<JObject>()
                                .SelectMany(v => v["files"] as JArray ?? new JArray())
                                .FirstOrDefault(f => f["url"] != null)?["url"]?.ToString();

                            if (fileUrl != null)
                                urls.Add(fileUrl);
                        }
                    }
                    catch (Exception ex) { Console.WriteLine($"Не вдалося знайти залежність (Modrinth) {depProjectId}: {ex.Message}"); }
                }
            }
            return urls;
        }

        private async Task<List<string>> GetCurseForgeDependencyUrls(ModVersionInfo parentMod)
        {
            var urls = new List<string>();
            string gameVersion = parentMod.GameVersions.FirstOrDefault(v => v.StartsWith("1."));

            if (string.IsNullOrEmpty(gameVersion)) return urls;

            var fileData = await _cfApiClient.GetModFileAsync(int.Parse(parentMod.ModId), int.Parse(parentMod.VersionId));
            if (fileData?.Data?.Dependencies == null) return urls;

            foreach (var dep in fileData.Data.Dependencies)
            {
                if (dep.RelationType == FileRelationType.RequiredDependency)
                {
                    try
                    {
                        string loader = parentMod.Loaders.FirstOrDefault();

                        var depFiles = (!string.IsNullOrEmpty(loader))
                            ? await _cfApiClient.GetModFilesAsync(
                                modId: dep.ModId,
                                gameVersion: gameVersion,
                                modLoaderType: (ModLoaderType)Enum.Parse(typeof(ModLoaderType), loader, true)
                            )
                            : await _cfApiClient.GetModFilesAsync(
                                modId: dep.ModId,
                                gameVersion: gameVersion
                            );
                        if (depFiles?.Data?.Count > 0)
                        {
                            var latestFile = depFiles.Data
                                .Where(f => f.ReleaseType == FileReleaseType.Release || f.ReleaseType == FileReleaseType.Beta)
                                .OrderByDescending(f => f.FileDate)
                                .FirstOrDefault();

                            if (latestFile?.DownloadUrl != null)
                                urls.Add(latestFile.DownloadUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Не вдалося знайти залежність (CurseForge) {dep.ModId}: {ex.Message}");
                    }
                }
            }
            return urls;
        }
        #endregion

        #region Download Helper
        private async Task DownloadFileHelperAsync(string url, string path, CancellationToken token, IProgress<int> progress)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                using var contentStream = await response.Content.ReadAsStreamAsync(token);

                string tempPath = path + ".tmp";

                var buffer = new byte[16384];

                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 16384, true))
                {
                    long totalRead = 0;
                    int bytesRead;
                    long lastReportedBytes = 0;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                        totalRead += bytesRead;

                        if (progress != null && totalBytes.HasValue && totalBytes > 0)
                        {
                            if (totalRead - lastReportedBytes > 102400 || totalRead == totalBytes)
                            {
                                lastReportedBytes = totalRead;
                                int percent = (int)(totalRead * 100 / totalBytes.Value);
                                progress.Report(percent);
                            }
                        }
                    }
                } 

                if (File.Exists(path)) File.Delete(path); 
                File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
                throw new Exception($"Не вдалося завантажити {Path.GetFileName(path)}. {ex.Message}");
            }
        }  
        #endregion
    }

    public class ModrinthVersionConverter : JsonConverter<ModVersionInfo>
    {
        public override ModVersionInfo ReadJson(JsonReader reader, Type objectType, ModVersionInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            JObject item = JObject.Load(reader);
            var versionInfo = new ModVersionInfo();

            serializer.Populate(item.CreateReader(), versionInfo);

            var files = item["files"] as JArray;
            if (files != null && files.Count > 0)
            {
                string foundUrl = null;
                string foundFileName = null;

                foreach (JObject file in files)
                {
                    string url = file["url"]?.ToString();
                    if (string.IsNullOrEmpty(url)) continue;

                    string filename = file["filename"]?.ToString();
                    bool isPrimary = file["primary"]?.Value<bool>() ?? false;

                    if (isPrimary)
                    {
                        foundUrl = url;
                        foundFileName = filename;
                        break;
                    }

                    if (foundUrl == null)
                    {
                        foundUrl = url;
                        foundFileName = filename;
                    }
                }

                versionInfo.DownloadUrl = foundUrl;
                versionInfo.FileName = foundFileName;
            }

            if (string.IsNullOrEmpty(versionInfo.FileName) && !string.IsNullOrEmpty(versionInfo.DownloadUrl))
            {
                if (Uri.TryCreate(versionInfo.DownloadUrl, UriKind.Absolute, out Uri uri))
                {
                    versionInfo.FileName = Path.GetFileName(uri.AbsolutePath);
                }
            }

            versionInfo.Site = "Modrinth";
            return versionInfo;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, ModVersionInfo value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}