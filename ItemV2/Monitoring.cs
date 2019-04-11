using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("Monitoring", "Kaidoz", "1.0.0")]
    [Description("Мониторинг для hurtworld.pro")]
    class Monitoring : CovalencePlugin
    {
        public class Stats
        {
            public string nameserver;

            public string address;

            public string port;

            public int online;

            public int maxplayer;

            public int steamcount;

            public int nosteamcount;

            public List<string> players = new List<string>();
        }

        void Loaded()
        {
            timer.Repeat(60f, 0, () =>
            {
                Send();
            });
        }

        Stats stats_;

        void Send()
        {
            Stats stats = GetStats();
            string post = JsonConvert.SerializeObject(stats);

            webrequest.Enqueue("http://hurtworld.pro/online/index.php?" + post, null, (code, response) =>
            {
            }, this);
        }

        Stats GetStats()
        {
            Stats stats = new Stats();
            /*
#if !RUST
                foreach (var pl in GameManager.Instance.GetSessions().Values)
                {
                    if (pl != null && pl.IsLoaded)
                    {
                        if (pl.AuthTicketBuffer.Length != 234)
                            stats.nosteamcount++;
                        else
                            stats.steamcount++;
                    }
                }
#else
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    if (pl.Connection.token.Length != 234)
                        stats.nosteamcount++;
                    else
                        stats.steamcount++;
                }
#endif*/
            foreach(var pl in players.Connected)
            {
                if(pl.IsConnected)
                    stats.players.Add(pl.Name);
            }
            stats.online = players.Connected.Count();
            stats.maxplayer = server.MaxPlayers;
            stats.address = server.Address.ToString();
            stats.port = server.Port.ToString();
            stats.nameserver = server.Name;
            return stats;
        }
    }
}
