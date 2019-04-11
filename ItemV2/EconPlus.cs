using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("EconPlus", "Kaidoz", "1.0.1")]
    [Description("Система экономики для кланов")]
    public class EconPlus : HurtworldPlugin
    {
        [PluginReference]
        private Plugin ClansRewards;

        #region Class

        public class Item
        {
            [JsonProperty("Цена")]
            public int prize;

            [JsonProperty("Command")]
            public CMD cmd;

            public class CMD
            {
                [JsonProperty("Enabled")]
                public bool usecommand;

                [JsonProperty("Command")]
                public string command;
            }

            [JsonProperty("NameInStore")]
            public string nameInStore;

            [JsonProperty("nameOrFullName")]
            public string nameOrFullName;

            [JsonProperty("Count")]
            public int count;
        }

        #endregion

        Dictionary<string, float> clansMoney;

        Dictionary<string, List<Item>> storeData;

        void LoadData()
        {
            clansMoney = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, float>>("EconPlus/clansMoney");
        }

        void SaveData()
        {
            Config.WriteObject(Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, float>>("EconPlus/clansMoney"), false, $"{Interface.Oxide.DataDirectory}/EconPlus/clansMoney.jsonBackup");
            Interface.Oxide.DataFileSystem.WriteObject("EconPlus/clansMoney", clansMoney);
        }

        [HookMethod("OnServerSave")]
        void OnServerSave()
        {
            SaveData();
        }

        [HookMethod("Loaded")]
        void Loaded()
        {
            ClansRewards = plugins.Find("ClansRewards");
            LoadData();
            LoadConfig();
        }

        void LoadPerm()
        {
            if (!permission.PermissionExists(Name.ToLower() + ".admin"))
                permission.RegisterPermission(Name.ToLower() + ".admin", this);
        }

        private new void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                storeData = Config.ReadObject<Dictionary<string, List<Item>>>();
                if (storeData == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(storeData, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file for EconPlus...");
            storeData = new Dictionary<string, List<Item>>()
            {
                {
                    "Weapons",
                    new List<Item>()
                    {
                        new Item()
                        {
                            prize = 105,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1,
                            nameInStore = "AR15",
                            nameOrFullName = "AR15"
                        },
                        new Item()
                        {
                            prize = 95,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1,
                            nameInStore = "AWM",
                            nameOrFullName = "AWM"
                        },
                        new Item()
                        {
                            prize = 65,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1,
                            nameInStore = "Shotgun",
                            nameOrFullName = "Shotgun"
                        },
                    }
                },
                {
                    "Raid",
                    new List<Item>()
                    {
                        new Item()
                        {
                            prize = 350,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1,
                            nameInStore = "RaidDrill",
                            nameOrFullName = "RaidDrill"
                        },
                        new Item()
                        {
                            prize = 550,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1,
                            nameInStore = "Detonator Cap",
                            nameOrFullName = "Detonator Cap"
                        }
                    }
                },
                {
                    "Build",
                    new List<Item>()
                    {
                        new Item()
                        {
                            prize = 60,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1000,
                            nameInStore = "Stone Brick(1000x)",
                            nameOrFullName = "Stone Brick"
                        },
                        new Item()
                        {
                            prize = 100,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1000,
                            nameInStore = "Armor Panel(1000x)",
                            nameOrFullName = "Armor Panel"
                        },
                        new Item()
                        {
                            prize = 30,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 1000,
                            nameInStore = "Wood Plank(1000x)",
                            nameOrFullName = "Wood Plank"
                        }
                    }
                },
                {
                    "Help",
                    new List<Item>()
                    {
                        new Item()
                        {
                            prize = 10,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 10,
                            nameInStore = "Bandage(10x)",
                            nameOrFullName = "Bandage"
                        }
                    }
                },
                {
                    "Other",
                    new List<Item>()
                    {
                        new Item()
                        {
                            prize = 20,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 100,
                            nameInStore = "Gasoline(100x)",
                            nameOrFullName = "Gasoline"
                        },
                        new Item()
                        {
                            prize = 20,
                            cmd = new Item.CMD()
                            {
                                usecommand = false,
                                command = ""
                            },
                            count = 100,
                            nameInStore = "Coal(500x)",
                            nameOrFullName = "Coal"
                        }
                    }
                }
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(storeData);

        string prefix = "<color=#00FF6F>[$$$]</color> ";

        #region Test

        [ChatCommand("balanceadmin")]
        void Econ2Balance(PlayerSession session, string cmd, string[] args)
        {
            if (!session.IsAdmin)
                return;

            if (session.Identity.Clan != null)
            {
                clansMoney[session.Identity.Clan.ClanGuid] += 1000f;

                SendChat(session, prefix + "Баланс клана: " + clansMoney[session.Identity.Clan.ClanGuid]);
                SendChat(session, "Для покупки используйте: " + "/shop");
                SendChat(session, "Помощь по кланам: " + "/clanhelp");
            }
            else
                SendChat(session, "Вы не имеете клана!");
        }

        #endregion

        [ChatCommand("balance")]
        void EconBalance(PlayerSession session, string cmd, string[] args)
        {
            if (session.Identity.Clan != null)
            {

                SendChat(session, prefix + "Баланс клана: " + clansMoney[session.Identity.Clan.ClanGuid]);
                SendChat(session, "Для покупки используйте: " + "/shop");
            }
            else
                SendChat(session, "Вы не имеете клана!");
        }

        [ChatCommand("shop")]
        void EconBuy(PlayerSession session, string cmd, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    int ct = 0;
                    if (int.TryParse(args[0], out ct))
                    {
                        if (ct < 0 || ct >= storeData.Count)
                        {
                            SendChat(session, prefix + "Отсуствует категория: " + args[0]);
                            return;
                        }
                        List<Item> list_ = storeData.Values.ToList()[ct];
                        string msg_ = string.Empty;
                        for (int a = 0; a < list_.Count(); a++)
                        {
                            msg_ += $"[{a}] " + list_[a].nameInStore + " - " + list_[a].prize + "<color=#00EE30>$</color>";
                            if (a != list_.Count() - 1)
                                msg_ += "\n";
                        }
                        SendChat(session, prefix + "Предметы " + storeData.ElementAt(ct).Key + ":\n" + msg_);

                    }
                    else
                    {
                        SendChat(session, prefix + "Для просмотра категории введите номер категории");
                    }
                    break;
                case 2:
                    if (!checkClan(session))
                        return;
                    int rd = 0;
                    int pd = 0;
                    if (int.TryParse(args[0], out rd))
                    {
                        if (rd < 0 || rd >= storeData.Count)
                        {
                            SendChat(session, prefix + "Отсуствует категория: " + args[0]);
                            return;
                        }
                        if (int.TryParse(args[1], out pd))
                        {
                            if (pd < 0 || pd >= storeData.ElementAt(rd).Value.Count)
                            {
                                SendChat(session, prefix + "Предмета с номер: " + args[1] + " не существует");
                                return;
                            }
                            Item it = storeData.ElementAt(rd).Value[pd];
                            if((clansMoney[session.Identity.Clan.ClanGuid] - it.prize)<0)
                            {
                                SendChat(session, prefix + "Не хватает денег: " + (it.prize -  clansMoney[session.Identity.Clan.ClanGuid]));
                                return;
                            }
                            if (!it.cmd.usecommand)
                            {
                                GiveItem(session, it.nameOrFullName, it.count);
                            }
                            else
                            {
                                ConsoleManager.Instance.ExecuteCommand(it.cmd.command.Replace("%steamid%", session.SteamId.ToString()));
                            }
                            clansMoney[session.Identity.Clan.ClanGuid] -= it.prize;
                            SendChat(session, prefix + "Товар " + it.nameInStore + " выдан");
                            return;
                        }
                    }
                    SendChat(session, prefix + "Синтакс: shop <номер_категории> <номер_предмета>");
                    break;
                default:
                    // для защиты 
                    string countStoreData = "{DarkPluginsId}";
                    var list = storeData.Keys.ToList();
                    string msg = string.Empty;
                    for (int a = 0; a < list.Count(); a++)
                    {
                        msg += $"[{a}] " + list[a];
                        if (a != list.Count() - 1)
                            msg += "\n";
                    }
                    // Для защиты
                    countStoreData = countStoreData.Replace("{0}", "1");
                    SendChat(session, prefix + "Все категории" + ":\n" + msg);
                    SendChat(session, prefix + "Синтакс: shop <номер_категории> <номер_предмета>");
                    break;
            }
        }

        [ChatCommand("clanhelp")]
        private void clanhelp(PlayerSession session, string cmd, string[] args)
        {
            if (!ClansRewards)
                return;

            int max = (int)ClansRewards.Call("getMaxTotem");
            string text = $"Захватывайте точки, чтобы получить баланс\n За баланс можно приобрести товары\nЕсли тотемов больше чем {max}, то добыча будет понижаться";
            SendChat(session, prefix + text);
        }

        #region Hooks

        [HookMethod("OnClanCreated")]
        void OnClanCreated(Clan clan)
        {
            if (!clansMoney.ContainsKey(clan.ClanGuid))
            {
                clansMoney.Add(clan.ClanGuid, 0);
                SaveData();
            }
        }

        #endregion

        #region Api

        void GiveReward(Clan clan, float amount)
        {
            if (!clansMoney.ContainsKey(clan.ClanGuid))
            {
                clansMoney.Add(clan.ClanGuid, amount);
            }
            else
            {
                clansMoney[clan.ClanGuid] += amount;
            }
        }

        #endregion

        #region Helpers

        bool checkClan(PlayerSession session)
        {
            if (session.Identity.Clan == null)
            {
                SendChat(session, prefix + "У вас нет клана");
                return false;
            }
            if (session.Identity.Clan.GetOwner() == (ulong)session.SteamId)
                return true;

            SendChat(session, prefix + "Вы не владелец/офицер клана");

            return false;
        }

        void GiveItem(PlayerSession session, string name, int count)
        {
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            itemmanager.GiveItem(session.Player, getItemFromName(name), count);
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var item in GlobalItemManager.Instance.GetGenerators())
            {
                if (item.Value.name == name || item.Value.GeneratorId.ToString() == name)
                    return item.Value;
            }
            return GlobalItemManager.Instance.GetGenerators()[2];
        }

        void SendChat(PlayerSession session, string message) => hurt.SendChatMessage(session, null, message);

        #endregion

    }
}
