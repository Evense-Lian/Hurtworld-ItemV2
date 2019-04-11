using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TeamLimiter", "Kaidoz", "1.0.1")]
    class TeamLimiter : HurtworldPlugin
    {

        public class Configuration
        {
            [JsonProperty("Max player in team")]
            public int MaxPlayerInTeam;

            [JsonProperty("Block authorize in totem another clan")]
            public bool blockAClans;
        }

        [HookMethod("LoadDefaultMessages")]
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"clanlimit","<color=red>[TeamLimit]</color> Only a maximum of {0} players per team is allowed."},
                {"stakelimit","<color=red>[TeamLimit]</color> Maximum authorized players!"},
                {"stakeanotherclan","<color=red>[TeamLimit]</color> There is a player from another clan!"}
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"clanlimit","<color=red>[TeamLimit]</color> Разрешена игра не более чем в {0} игроков в команде!"},
                {"stakelimit","<color=red>[TeamLimit]</color> Авторизовано максиомальное количество игроков!"},
                {"stakeanotherclan","<color=red>[TeamLimit]</color> Присутсвует игрок из другой команды!"}
            }, this, "ru");
        }

        Configuration _config;

        private new void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(_config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file for TeamLimiter...");
            _config = new Configuration()
            {
                blockAClans = true,
                MaxPlayerInTeam = 3
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        void Loaded()
        {
            LoadConfig();
        }

        string prefix = "<color=red>[TeamLimit]</color> ";

        object OnClanMemberApply(Clan clan, PlayerSession session)
        {
            if (clan.GetMemebers().Count()>=_config.MaxPlayerInTeam)
            {
                int maxplayers = clan.GetMemebers().Count() + 1;
                SendChat(session, Msg("clanlimit", session.SteamId.ToString()));
                return false;
            }
                

            return null;
        }

        object OnPlayerAuthorize(PlayerSession session, GameObject obj, OwnershipStakeServer ownerShip)
        {
            if (ownerShip.IsClanTotem)
                return null;

            if (_config.blockAClans && !checkAnotherClan(ownerShip, session.Identity.Clan))
            {
                SendChat(session, Msg("stakeanotherclan", session.SteamId.ToString()));
                return false;
            }

            if(ownerShip.AuthorizedPlayers.Count()>=_config.MaxPlayerInTeam)
            {
                int pl = ownerShip.AuthorizedPlayers.Count() + 1;
                SendChat(session, string.Format(Msg("stakelimit", session.SteamId.ToString()), _config.MaxPlayerInTeam + 1));
                return false;
            }


            return null;
        }

        bool checkAnotherClan(OwnershipStakeServer stake, Clan clan)
        {
            if (clan == null)
                return true;

            foreach(var pl in stake.AuthorizedPlayers)
            {
                if(pl.Clan!=null && pl.Clan!=clan)
                {
                    return false;
                }
            }
            return true;
        }

        string Msg(string msg, string SteamId = null)
        {
            return lang.GetMessage(msg, this, SteamId);
        }

        void SendChat(PlayerSession session, string message) => hurt.SendChatMessage(session, null, message);
    }
}
