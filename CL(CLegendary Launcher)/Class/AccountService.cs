using CL_CLegendary_Launcher_.Models;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using MojangAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CL_CLegendary_Launcher_.Class
{
    public class AccountService
    {
        private readonly ProfileManagerService _profileManager;
        private readonly JELoginHandler _loginHandler;

        public MSession CurrentSession { get; private set; }
        public ProfileItem CurrentProfile { get; private set; }

        public AccountService(ProfileManagerService profileManager)
        {
            _profileManager = profileManager;
            _loginHandler = JELoginHandlerBuilder.BuildDefault();
        }

        public List<ProfileItem> GetProfiles()
        {
            return _profileManager.LoadProfiles();
        }

        public async Task<bool> SelectProfileAsync(ProfileItem profile)
        {
            try
            {
                CurrentSession = await _profileManager.CreateSessionForProfileAsync(profile, _loginHandler);
                CurrentProfile = profile;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Не вдалося активувати профіль: {ex.Message}");
            }
        }

        public void DeleteProfile(ProfileItem profile)
        {
            var profiles = GetProfiles();
            var target = profiles.FirstOrDefault(p => p.UUID == profile.UUID && p.NameAccount == profile.NameAccount);

            if (target != null)
            {
                profiles.Remove(target);
                _profileManager.SaveProfiles(profiles);

                if (CurrentProfile != null && CurrentProfile.UUID == profile.UUID)
                {
                    CurrentSession = null;
                    CurrentProfile = null;
                    if (profile.TypeAccount == AccountType.Microsoft)
                    {
                        _loginHandler.Signout();
                    }
                }
            }
        }
        public async Task<ProfileItem> AddMicrosoftAccountAsync()
        {
            var session = await _loginHandler.AuthenticateInteractively();
            var mojangApi = new Mojang(new HttpClient());

            bool ownsGame = await mojangApi.CheckGameOwnership(session.AccessToken);
            if (!ownsGame) throw new Exception("На цьому акаунті не куплено Minecraft.");

            var profile = new ProfileItem
            {
                NameAccount = session.Username,
                UUID = session.UUID,
                ImageUrl = $"https://mc-heads.net/avatar/{session.UUID}",
                AccessToken = session.AccessToken,
                TypeAccount = AccountType.Microsoft
            };

            _profileManager.SaveProfile(profile);
            return profile;
        }

        public async Task<ProfileItem> AddOfflineAccountAsync(string nickname)
        {
            string uuid;
            try
            {
                Mojang mojang = new Mojang(new HttpClient());
                var res = await mojang.GetUUID(nickname);
                uuid = res.UUID;
            }
            catch
            {
                uuid = Guid.NewGuid().ToString();
            }

            var profile = new ProfileItem
            {
                NameAccount = nickname,
                UUID = uuid,
                AccessToken = "-",
                ImageUrl = "pack://application:,,,/Assets/big-steve-face-2002298922 2.png",
                TypeAccount = AccountType.Offline
            };

            _profileManager.SaveProfile(profile);
            return profile;
        }

        public async Task<ProfileItem> AddLittleSkinAccountAsync(string login, string password)
        {
            var session = await _profileManager.LoginLittleSkinAsync(login, password);

            var profile = new ProfileItem
            {
                NameAccount = session.Username,
                UUID = session.UUID,
                AccessToken = session.AccessToken,
                ImageUrl = "pack://application:,,,/Assets/LittleSkinAccount.png",
                TypeAccount = AccountType.LittleSkin
            };

            _profileManager.SaveProfile(profile);
            return profile;
        }
    }
}
