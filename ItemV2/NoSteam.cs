using Newtonsoft.Json;
using Oxide.Core;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("NoSteam", "Kaidoz", "1.0.3")]
    class NoSteam : HurtworldPlugin
    {
        public class CFG
        {
            [JsonProperty("Allowed only excludes")]
            public bool enabledExclude;

            [JsonProperty("Exclude steamid")]
            public List<ulong> exclude;

            [JsonProperty("Kick message")]
            public string kickMessage;
        }

        

        void Loaded()
        {
            LoadConfig();
        }

        #region Config

        CFG cfg;

        private new void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<CFG>();
                if (cfg == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(cfg, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file for NoSteam...");
            cfg = new CFG()
            {
                enabledExclude = false,
                exclude = new List<ulong>()
                {
                    76561198874111111
                },
                kickMessage = "Отпишите в группу для получения доступа!"
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(cfg);

        #endregion

        #region API

        void AddExclude(ulong steamid)
        {
            cfg.exclude.Add(steamid);
            SaveConfig();
        }

        #endregion

        object OnSteamAuth(PlayerSession session)
        {
            if (checkAuth(session))
            {
                if (Singleton<SteamworksManagerClient>.Instance.BeginAuthSession(session.AuthTicketBuffer, session.SteamId, -1) == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK)
                    return null;

                if(cfg.enabledExclude && !cfg.exclude.Contains((ulong)session.SteamId))
                {
                    GameManager.Instance.StartCoroutine(GameManager.Instance.DisconnectPlayerSync(session.Player, cfg.kickMessage));
                    return null;
                }
                    
                Puts(session.SteamId + " OnSteamAuth succes!");
                session.ValidationResponse = EAuthSessionResponse.k_EAuthSessionResponseOK;
                session.OwnerSteamId = session.SteamId.ToString();
                return false;
            }
            return null;
        }

        object OnEacAuth(PlayerSession session)
        {
            if (checkAuth(session))
            {
                return false;
            }
            return null;
        }

        bool checkAuth(PlayerSession session)
        {
            if (session.AuthTicketBuffer.Length == 234 || session.AuthTicketBuffer.Length == 240)
                return true;

            return false;
        }
    }
}
