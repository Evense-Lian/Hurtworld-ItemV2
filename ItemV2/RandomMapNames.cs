using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("RandomMapNames", "Kaidoz", "1.0.1")]
    class RandomMapNames : HurtworldPlugin
    {
        public class Configuration
        {
            public int time;
            public List<string> mapnames;
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
            PrintWarning("Creating new config file for RandomMapNames...");
            _config = new Configuration()
            {
                time = 60,
                mapnames = new List<string>()
                {
                    "Kaidoz",
                    "oxide-russia.ru"
                }
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        System.Random rnd = new System.Random();
        int st = 0;
        int d = 0;

        void OnServerInitialized()
        {
            LoadConfig();
            timer.Every(1f, () => 
            {
                try
                {
                    if (st > _config.time)
                    {
                        d++;
                        if (d == _config.mapnames.Count())
                            d = 0;
                        st = 0;
                    }

                    st++;
                    string name = _config.mapnames[d];
                    Steamworks.SteamGameServer.SetMapName(name);
                }
                catch { }
            });
        }
    }
}
