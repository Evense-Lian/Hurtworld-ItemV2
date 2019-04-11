using System;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("MathPlus", "Kaidoz", "1.0.3")]
    [Description("Bot-Mathematician for Rust и HurtWorld ItemV2")]

    class MathPlus : CovalencePlugin
    {

        /* SUPPORT 
         *  Discord: Kaidoz#3059
         *  Steam: https://steamcommunity.com/id/ka1doz/
         * */


        #region Class

        public class Configuration
        {
            [JsonProperty("Repeat event(sec.)")]
            public float timer;

            [JsonProperty("Min. online")]
            public int countpl;

            [JsonProperty("Higher number - harder example")]
            public int rej;

            [JsonProperty("List rewards")]
            public List<Prize> prizes;

            public class Prize
            {
                [JsonProperty("ItemNameOrId")]
                public string item;

                [JsonProperty("Count")]
                public int count;

                [JsonProperty("Name")]
                public string name;
            }
        }

        #endregion

        void Loaded()
        {
            LoadConfig();
            timer.Repeat(_config.timer, 0, () =>
            {
                if (players.Connected.Count() >= _config.countpl)
                    startmath();
            });
        }

        #region Config

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
            PrintWarning("Creating new config file for MathPlus...");
            _config = new Configuration()
            {
                timer = 900,
                countpl = 15,
                rej = 1,
                prizes = new List<Configuration.Prize>()
                {
                    new Configuration.Prize()
                    {
                        item = "Mondinium Ore",
                        count = 10,
                        name = "Зеленая руда"
                    },
                    new Configuration.Prize()
                    {
                        item = "Gasoline",
                        count = 20,
                        name = "Бензин"
                    },
                    new Configuration.Prize()
                    {
                        item = "Amber",
                        count = 5,
                        name = "Янтарь"
                    }
                }
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        System.Random rnd = new System.Random();
        string answer = string.Empty;

        [Command("answer", "ans", "math")]
        private void mathcommand(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == answer)
                {
                    //try
                    //{
                    endmath(player);
                    //}
                    //catch { }
                }
                else
                    player.Reply("<color=green><b>[Математик]</b></color> Неверный ответ:c");
            }
            else
            {
                player.Reply("<color=green><b>[Математик]</b></color> Неправильно набрано. Пример: " + cmd + "228");
            }
        }

        void startmath()
        {
            var gen = generatos(_config.rej);
            answer = gen.First().Value.ToString();
            messageall($"<color=green> <b>[Математик] </b></color>Был сгенерирован пример! \n Найдите ответ: {gen.First().Key} \n Команда: /math ответ");
        }

        void endmath(IPlayer player)
        {
            answer = string.Empty;
            string steamid = player.Id;
            var prize = _config.prizes[rnd.Next(0, _config.prizes.Count() - 1)];
            string id = prize.item;
            int count = prize.count;
            string name = prize.name;
#if RUST
            BasePlayer basePlayer = BasePlayer.Find(player.Id);

            if(player_==null)
                return;

            Item item = ItemManager.Create(FindItem(itemNameOrId));
            if (item == null)
            {
                return false;
            }

            item.amount = amount;

            ItemContainer itemContainer = null;
            switch (container.ToLower())
            {
                case "belt":
                    itemContainer = basePlayer.inventory.containerBelt;
                    break;

                case "main":
                    itemContainer = basePlayer.inventory.containerMain;
                    break;
            }

            if (!basePlayer.inventory.GiveItem(item, itemContainer))
            {
                item.Remove();
            }
#else
            PlayerSession player_ = getSession(player.Name);
            if (player_ == null)
                return;
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            itemmanager.GiveItem(player_.Player, getItemFromName(id), count);
#endif
            messageall($"<color=green> <b>[Математик] </b></color>Победитель {player.Name} \n Он получил {name} в количестве {count}");
        }


        #region Helper

        void messageall(string msg)
        {
            foreach (var a in players.Connected)
            {
                a.Reply(msg);
            }
        }

#if RUST
        BasePlayer GetBasePlayer(string steamid)
        {
            var Online = BasePlayer.activePlayerList as List<BasePlayer>;
            foreach (BasePlayer player in Online)
            {
                if (player.OwnerID.ToString() == steamid)
                    return player;
            }

            return null;
        }
#else
        private PlayerSession getSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;

            foreach (var i in sessions)
            {
                if (i.Value.Identity.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }
#endif
        Dictionary<string, int> generatos(int rejim)
        {
            int a1 = rnd.Next(230, 500) * rejim;
            int a2 = rnd.Next(5, 15) * rejim;
            int a3 = rnd.Next(10, 15) * rejim;
            int a4 = rnd.Next(10, 50) * rejim;

            int answ = a1 - a2 * a3 + a4;

            return new Dictionary<string, int>()
            {
                {
                    $"{a1} - {a2} * {a3} + {a4}",
                    answ
                }
            };
        }

#if RUST
        private ItemDefinition FindItem(string itemNameOrId)
        {
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemNameOrId.ToLower());
            if (itemDef == null)
            {
                int itemId;
                if (int.TryParse(itemNameOrId, out itemId))
                {
                    itemDef = ItemManager.FindItemDefinition(itemId);
                }
            }
            return itemDef;
        }
#else
        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var item in GlobalItemManager.Instance.GetGenerators())
            {
                if (item.Value.name == name || item.Value.GeneratorId.ToString() == name)
                    return item.Value;
            }
            return GlobalItemManager.Instance.GetGenerators()[2];
        }
#endif
        private void GetConfig<T>(ref T variable, params string[] path)
        {
            if (path.Length == 0)
                return;

            if (Config.Get(path) == null)
            {
                Config.Set(path.Concat(new object[] { variable }).ToArray());
                PrintWarning($"Added field to config: {string.Join("/", path)}");
            }

            variable = (T)Convert.ChangeType(Config.Get(path), typeof(T));
        }

        #endregion

    }
}