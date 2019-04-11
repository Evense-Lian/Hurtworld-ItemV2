using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

//////--------------------------------------------------------------------------------//
//////            Автор плагина Vladimir.Kzi / Plugin autor Vladimir.Kzi              //
//////--------------------------------------------------------------------------------//
//////  Поддержка данного плагина: www.oxide-russia.ru and www.vk.com/deviantplugins  //
//////--------------------------------------------------------------------------------//
//////  Support for this plugin: www.oxide-russia.ru и www.vk.com/deviantplugins      //
//////--------------------------------------------------------------------------------//

namespace Oxide.Plugins
{
    [Info("EventFarming", "Vladimir.Kzi", "0.0.8")]
    [Description("Farming resources mini-event(AutoEvent).")]
    class EventFarming : HurtworldPlugin
    {

        //////////////////////////////////////////////////////////////////////////////////////////
        // Классы
        //////////////////////////////////////////////////////////////////////////////////////////

        private ConfigData configData;
        public bool farmStarted;
        public DateTime startTime;
        public Dictionary<ulong, int> playersFarming = new Dictionary<ulong, int>();
        private Dictionary<string, DateTime> playersWin = new Dictionary<string, DateTime>();
        private FarmEvent currentFarmEvent = new FarmEvent();

        class ConfigData
        {
            public bool autoStart { get; set; }
            public float intervalAutoStart { get; set; }
            public float chatProgressInterval { get; set; }
            public int minPlayers { get; set; }
            public bool giveGift { get; set; }
            public bool giveRandomAllGift { get; set; }
            public bool showWinnerInChat { get; set; }
            public bool writeWinLog { get; set; }
            public bool showWinConsole { get; set; }
            public List<GiftItems> listGiftItems { get; set; }
            public List<FarmEvents> listFarmEvents { get; set; }
        }

        class GiftItems
        {
            public string itemName;
            public string ItemIDNameGuID;
            public int itemCount;
        }

        class FarmEvents
        {
            public string Name;
            public string Item;
            public float Time;
            public int Farm;
            public int Places;
        }

        class FarmEvent
        {
            public string Name;
            public string Item;
            public Timer Time;
            public Timer Progress;
            public int Farm;
            public int Places;
        }

        class ItemsList
        {
            public ItemGeneratorAsset ItemGen;
            public string ItemName;
            public int ItemId;
            public string ItemGuID;
        }

        List<ItemsList> allItemsList = new List<ItemsList>();

        //////////////////////////////////////////////////////////////////////////////////////////
        // Загрузка плагина
        //////////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            LoadVariables();
            LoadMessages();

            RegisterPerm("admin");

        }

        ////////////////////////////////////////////////////////////////////////////////////////
        ///     Конфиг
        ////////////////////////////////////////////////////////////////////////////////////////

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new configuration file for " + this.Title);
            var config = new ConfigData
            {
                autoStart = true,
                intervalAutoStart = 60f,
                chatProgressInterval = 2f,
                minPlayers = 5,
                giveGift = true,
                giveRandomAllGift = true,
                showWinnerInChat = true,
                writeWinLog = false,
                showWinConsole = false,
                listGiftItems = new List<GiftItems>
                {
                    {
                        new GiftItems
                        {
                            itemName = "C4",
                            ItemIDNameGuID = "267",
                            itemCount = 1,
                        }
                    },
                    {
                        new GiftItems
                        {
                            itemName = "Детонатор",
                            ItemIDNameGuID = "d31480f29cb72f944b0c1eea1caf81b5",
                            itemCount = 1,
                        }
                    },
                    {
                        new GiftItems
                        {
                            itemName = "AR RedDot Vista",
                            ItemIDNameGuID = "AR RedDot Vista",
                            itemCount = 1,
                        }
                    }
                },
                listFarmEvents = new List<FarmEvents>
                {
                    {
                        new FarmEvents
                        {
                            Name = "wood150",
                            Item = "Items/Raw Materials/Wood Log",
                            Time = 5f,
                            Farm = 150,
                            Places = 3
                        }
                    },
                    {
                        new FarmEvents
                        {
                            Name = "wood300",
                            Item = "Items/Raw Materials/Wood Log",
                            Time = 10f,
                            Farm = 300,
                            Places = 3
                        }
                    },
                    {
                        new FarmEvents
                        {
                            Name = "stone100",
                            Item = "Items/Raw Materials/Stone",
                            Time = 5f,
                            Farm = 100,
                            Places = 3
                        }
                    },
                    {
                        new FarmEvents
                        {
                            Name = "stone200",
                            Item = "Items/Raw Materials/Stone",
                            Time = 10f,
                            Farm = 200,
                            Places = 3
                        }
                    }
                }
            };
            SaveConfig(config);
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        ///     Языковые Сообщения
        ////////////////////////////////////////////////////////////////////////////////////////

        void LoadMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"msg_Prefix", "<color=lime>[Farming]</color>"},
                {"msg_NoPermission", "<color=red>You do not have permission for this command.</color>"},
                {"msg_Progress", "Current event <color=red>progress</color>:"},
                {"msg_Finnish", "Event <color=red>ended</color>, results:"},
                {"msg_Winners", "<color=orange>{num}</color>. <color=green>{name}</color> finished at <color=lightblue>{time}</color>!"},
                {"msg_Winner", "<color=green>{name}</color> receives a prize <color=red>{gift}</color>(<color=orange>x{cout}</color>)!"},
                {"msg_Farming", "<color=orange>{num}</color>. <color=green>{name}</color> farmed <color=lightblue>{amount}</color>!"},
                {"msg_Finished", "<color=green>{name}</color> finished at <color=lightblue>{time}</color>!"},
                {"msg_Start", "Event started, you should farm <color=lightblue>{amount}</color>x of <color=green>{item}</color>!"},
                {"msg_Stop", "<color=red>Event stopped!</color>"},
                {"msg_Started", "<color=red>Already started!</color>"},
                {"msg_Stopped", "<color=red>Already stopped!</color>"},
                {"msg_NoEvent", "<color=red>No event with this name!</color>"},
                {"msg_Commands", "Commands:"},
                {"msg_CMDStart", "<color=red>/farm start</color> - Start event."},
                {"msg_CMDStartName", "<color=red>/farm start <name></color> - Start event with name."},
                {"msg_CMDStop", "<color=red>/farm stop</color> - Stop event."},
                {"msg_Minutes", " m."},
                {"msg_Seconds", " s."},
                {"Stone", "Stone"},
                {"Wood Log", "Wood"},
                {"Flint", "Flint"},
                {"Limestone", "Limestone"},
                {"Clay", "Clay"},
                {"Coal", "Coal"},
                {"Amber", "Amber"},
                {"Iron Ore", "Iron"},
                {"Ultranium Ore", "Ultranium"},
                {"Mondinium Ore", "Mondinium"},
                {"Titranium Ore", "Titranium"},
                {"Tree Bark", "Tree Bark"}
            }, this, "en");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"msg_Prefix", "<color=lime>[Фермер]</color>"},
                {"msg_NoPermission", "<color=red>У Вас нет прав для использования этой команды.</color>"},
                {"msg_Progress", "<color=red>Прогресс</color> ивента:"},
                {"msg_Finnish", "Ивент <color=red>закончен</color>, результаты:"},
                {"msg_Winners", "<color=orange>{num}</color>. <color=green>{name}</color> нафармил за <color=lightblue>{time}</color>!"},
                {"msg_Winner", "<color=green>{name}</color> получает приз <color=red>{gift}</color>(<color=orange>x{cout}</color>)!"},
                {"msg_Farming", "<color=orange>{num}</color>. <color=green>{name}</color> нафармил <color=lightblue>{amount}</color>!"},
                {"msg_Finished", "<color=green>{name}</color> нафармил за <color=lightblue>{time}</color>!"},
                {"msg_Start", "Ивент запущен, вы должны нафармить <color=lightblue>{amount}</color> шт - <color=green>{item}</color>!"},
                {"msg_Stop", "<color=red>Ивент остановлен!</color>"},
                {"msg_Started", "<color=red>Уже запущен!</color>"},
                {"msg_Stopped", "<color=red>Еще не запущен!</color>"},
                {"msg_NoEvent", "<color=red>Нет фарм ивента с таким именем!</color>"},
                {"msg_Commands", "Команды:"},
                {"msg_CMDStart", "<color=red>/farm start</color> - Запустить ивент."},
                {"msg_CMDStartName", "<color=red>/farm start <name></color> - Запустить ивент по имени."},
                {"msg_CMDStop", "<color=red>/farm stop</color> - Остановить ивент."},
                {"msg_Minutes", " минут(ы)"},
                {"msg_Seconds", " секунд(ы)"},
                {"Stone", "Камня"},
                {"Wood Log", "Дерева"},
                {"Flint", "Кремня"},
                {"Limestone", "Известняка"},
                {"Clay", "Глины"},
                {"Coal", "Угля"},
                {"Amber", "Янтаря"},
                {"Iron Ore", "Железа"},
                {"Ultranium Ore", "Синьки"},
                {"Mondinium Ore", "Зеленки"},
                {"Titranium Ore", "Краснухи"},
                {"Tree Bark", "Коры"}
            }, this, "ru");
        }

        void OnServerInitialized()
        {
            farmStarted = false;

            AllItemsList();

            if (configData.autoStart)
            {
                timer.Repeat(configData.intervalAutoStart * 60, 0, cmdFarming);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Комманды
        //////////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("farm")]
        void cmdFarm(PlayerSession player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                if (HasPerm(player.SteamId, "admin") || player.IsAdmin)
                {
                    SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_Commands", player.SteamId.ToString()));
                    SendChatMessage(player, null, GetMsg("msg_CMDStart", player.SteamId.ToString()));
                    SendChatMessage(player, null, GetMsg("msg_CMDStartName", player.SteamId.ToString()));
                    SendChatMessage(player, null, GetMsg("msg_CMDStop", player.SteamId.ToString()));
                    return;
                }
                else
                {
                    SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_NoPermission", player.SteamId.ToString()));
                    return;
                }
            }
            var Action = args[0];
            switch (Action)
            {
                case "start":
                    if (HasPerm(player.SteamId, "admin") || player.IsAdmin)
                    {
                        if (args.Length == 2)
                        {
                            if (!farmStarted)
                            {
                                if (getFarming(args[1].ToString()) == null)
                                {
                                    SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_NoEvent", player.SteamId.ToString()));
                                    return;
                                }
                                cmdFarmingStart(args[1].ToString());
                            }
                            else
                            {
                                SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_Started", player.SteamId.ToString()));
                                return;
                            }
                        }
                        else
                        {
                            if (!farmStarted)
                            {
                                cmdFarmingStart();
                            }
                            else
                            {
                                SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_Started", player.SteamId.ToString()));
                                return;
                            }
                        }
                    }
                    else
                    {
                        SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_NoPermission", player.SteamId.ToString()));
                        return;
                    }
                    break;
                case "stop":
                    if (HasPerm(player.SteamId, "admin") || player.IsAdmin)
                    {
                        if (farmStarted)
                        {
                            cmdFarmingStop();
                        }
                        else
                        {
                            SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_Stopped", player.SteamId.ToString()));
                            return;
                        }
                    }
                    else
                    {
                        SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_NoPermission", player.SteamId.ToString()));
                        return;
                    }
                    break;
                default:
                    if (HasPerm(player.SteamId, "admin") || player.IsAdmin)
                    {
                        SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_Commands", player.SteamId.ToString()));
                        SendChatMessage(player, null, GetMsg("msg_CMDStart", player.SteamId.ToString()));
                        SendChatMessage(player, null, GetMsg("msg_CMDStartName", player.SteamId.ToString()));
                        SendChatMessage(player, null, GetMsg("msg_CMDStop", player.SteamId.ToString()));
                        return;
                    }
                    else
                    {
                        SendChatMessage(player, GetMsg("msg_Prefix"), GetMsg("msg_NoPermission", player.SteamId.ToString()));
                        return;
                    }
                    break;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Запуск для авто-ивента
        ////////////////////////////////////////////////////////////////////////////////////////

        void cmdFarming()
        {
            if (!getMinPlayers())
            {
                return;
            }

            cmdFarmingStart();
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Запускаем ивент
        ////////////////////////////////////////////////////////////////////////////////////////

        void cmdFarmingStart(string name = null)
        {
            FarmEvents fevent;

            if (farmStarted)
            {
                return;
            }

            if (name != null)
            {
                fevent = getFarming(name);
            }
            else
            {
                int rand = UnityEngine.Random.Range(0, configData.listFarmEvents.Count);
                fevent = configData.listFarmEvents[rand];
            }

            currentFarmEvent.Name = fevent.Name;
            currentFarmEvent.Item = fevent.Item;
            currentFarmEvent.Farm = fevent.Farm;
            currentFarmEvent.Places = fevent.Places;
            currentFarmEvent.Time = timer.Once(fevent.Time * 60, () => { cmdFarmingStop(); });
            currentFarmEvent.Progress = timer.Every(configData.chatProgressInterval * 60, () => { cmdFarmingStat(); });
            BroadcastChat(GetMsg("msg_Prefix"), GetMsg("msg_Start").Replace("{item}", GetMsg(currentFarmEvent.Item.ToString().Split('/').Last())).Replace("{amount}", currentFarmEvent.Farm.ToString()));
            farmStarted = true;
            startTime = DateTime.Now;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Останавливаем Ивент
        //////////////////////////////////////////////////////////////////////////////////////////

        void cmdFarmingStop(bool finnish = false)
        {
            if (finnish || playersWin.Count > 0)
            {
                cmdFarmingStat(true);
                if (configData.giveGift)
                {
                    GiftItems ItemGift = null;
                    if (!configData.giveRandomAllGift)
                    {
                        int rand = UnityEngine.Random.Range(0, configData.listGiftItems.Count);
                        ItemGift = configData.listGiftItems[rand];
                    }

                    foreach (var win in playersWin)
                    {
                        if (IsValidSession(FindSession(win.Key.ToString())))
                        {
                            if (configData.giveRandomAllGift)
                            {
                                int rand = UnityEngine.Random.Range(0, configData.listGiftItems.Count);
                                ItemGift = configData.listGiftItems[rand];
                            }
                            string pName = "";
                            pName = FindSession(win.Key.ToString()).Identity.Name;

                            if ((bool)this.GiveItem(FindSession(win.Key.ToString()), ItemGift.ItemIDNameGuID, ItemGift.itemCount))
                            {
                                if (configData.showWinnerInChat)
                                {
                                    BroadcastChat(GetMsg("msg_Prefix"), GetMsg("msg_Winner").Replace("{name}", pName).Replace("{gift}", ItemGift.itemName.ToString()).Replace("{cout}", ItemGift.itemCount.ToString()));
                                }
                                if (configData.writeWinLog)
                                {
                                    LogToFile("WinLog", $"[" + System.DateTime.Now + "] " + pName + " (" + FindSession(win.Key.ToString()).SteamId + ") GIFT: " + ItemGift.itemName.ToString() + " IDorNAMEorGUID: " + ItemGift.ItemIDNameGuID.ToString() + " COUNT:" + ItemGift.itemCount.ToString(), this, true);
                                }
                                if (configData.showWinConsole)
                                {
                                    Puts(pName + " (" + FindSession(win.Key.ToString()).SteamId + ") GIFT: " + ItemGift.itemName.ToString() + " IDorNAMEorGUID: " + ItemGift.ItemIDNameGuID.ToString() + " COUNT:" + ItemGift.itemCount.ToString());
                                }
                            }
                        }
                    }

                }
            }
            else
            {
                BroadcastChat(GetMsg("msg_Prefix"), GetMsg("msg_Stop"));
            }
            currentFarmEvent.Time.Destroy();
            currentFarmEvent.Progress.Destroy();
            currentFarmEvent = new FarmEvent();
            playersFarming.Clear();
            playersWin.Clear();
            farmStarted = false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Добыча ресурсов игроком
        ////////////////////////////////////////////////////////////////////////////////////////

        void OnDispenserGather(GameObject resourceNode, HurtMonoBehavior player, List<ItemObject> items)
        {
            string Name = GetNameOfObject(resourceNode);
            Name = Name.Replace("(P)", "");
            PlayerSession session = FindSession(Name);
            if (farmStarted && !playersWin.ContainsKey(Name))
            {
                foreach (var item in items)
                {
                    if (item == null) return;
                    if (item.GetNameKey().ToString() == currentFarmEvent.Item.ToString())
                    {
                        if (!playersFarming.ContainsKey((ulong)session.SteamId))
                        {
                            playersFarming.Add((ulong)session.SteamId, item.StackSize);
                        }
                        else
                        {
                            playersFarming[(ulong)session.SteamId] += item.StackSize;
                        }

                        if (playersFarming[(ulong)session.SteamId] >= currentFarmEvent.Farm)
                        {
                            if (playersWin.Count < currentFarmEvent.Places)
                            {
                                playersWin.Add(Name, DateTime.Now);
                                TimeSpan ts = DateTime.Now - startTime;
                                BroadcastChat(GetMsg("msg_Prefix"), GetMsg("msg_Finished").Replace("{name}", Name).Replace("{time}", ConvertSecondsToDate(ts.TotalSeconds)));
                                playersFarming.Remove((ulong)session.SteamId);
                                return;
                            }
                            else
                            {
                                cmdFarmingStop(true);
                                return;
                            }
                        }
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Отображения прогресса и результата ивента
        ////////////////////////////////////////////////////////////////////////////////////////

        void cmdFarmingStat(bool finnish = false)
        {
            if (finnish)
            {
                BroadcastChat(GetMsg("msg_Prefix"), GetMsg("msg_Finnish"));
            }
            else
            {
                if (playersFarming.Count > 0 || playersWin.Count > 0)
                {
                    BroadcastChat(GetMsg("msg_Prefix"), GetMsg("msg_Progress"));
                }
            }
            int i = 1;
            foreach (var wins in playersWin)
            {
                TimeSpan ts = wins.Value - startTime;
                BroadcastChat(null, GetMsg("msg_Winners").Replace("{num}", i.ToString()).Replace("{name}", wins.Key).Replace("{time}", ConvertSecondsToDate(ts.TotalSeconds)));
                i++;
            }
            foreach (var farm in playersFarming.OrderByDescending(p => p.Value).Take(currentFarmEvent.Places - i + 1))
            {
                if (IsValidSession(FindSession(farm.Key.ToString())))
                {
                    string pName = "";
                    pName = FindSession(farm.Key.ToString()).Identity.Name;

                    BroadcastChat(null, GetMsg("msg_Farming").Replace("{num}", i.ToString()).Replace("{name}", pName).Replace("{amount}", farm.Value.ToString()));
                    i++;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Создаем список всех предметов( Один раз за запуск сервера )
        ////////////////////////////////////////////////////////////////////////////////////////

        void AllItemsList()
        {
            foreach (var item in GlobalItemManager.Instance.GetGenerators())
            {
                if (item.Value?.name != null)
                {
                    ItemsList aitem = new ItemsList();
                    aitem.ItemGen = item.Value;
                    aitem.ItemName = item.Value.name;
                    aitem.ItemId = item.Value.GeneratorId;
                    aitem.ItemGuID = RuntimeHurtDB.Instance.GetGuid(item.Value);

                    allItemsList.Add(aitem);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Ищем нужный предмет в списке всех предметов
        ////////////////////////////////////////////////////////////////////////////////////////

        ItemGeneratorAsset FindItem(string itemNameOrIdOrGuid)
        {
            List<ItemsList> items = allItemsList;
            ItemsList aitem;
            ItemGeneratorAsset item;
            int itemId;

            if (int.TryParse(itemNameOrIdOrGuid, out itemId))
            {
                aitem = items.First(i => i.ItemId == itemId);
                item = aitem.ItemGen;
            }
            else if ((from x in items where x.ItemGuID == itemNameOrIdOrGuid select x).Count() > 0)
            {
                aitem = items.First(i => i.ItemGuID.Contains(itemNameOrIdOrGuid));
                item = aitem.ItemGen;
            }
            else if ((from x in items where x.ItemName.ToLower() == itemNameOrIdOrGuid.ToLower() select x).Count() > 0)
            {
                aitem = items.First(i => i.ItemName.ToLower().Contains(itemNameOrIdOrGuid.ToLower()));
                item = aitem.ItemGen;
            }
            else
            {
                item = null;
            }

            return item;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Выдаем игроку нужный предмет
        ////////////////////////////////////////////////////////////////////////////////////////

        object GiveItem(PlayerSession session, string itemNameOrIdOrGuid, int amount = 1)
        {
            string itemName = itemNameOrIdOrGuid;
            PlayerInventory inventory = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            ItemGeneratorAsset generator = FindItem(itemNameOrIdOrGuid);

            if (generator == null)
            {
                PrintWarning("Something is wrong with the item:" + itemNameOrIdOrGuid);
                return false;
            }

            ItemObject itemObj;
            if (generator.IsStackable())
            {
                itemObj = GlobalItemManager.Instance.CreateItem(generator, amount);
                if (!inventory.GiveItemServer(itemObj))
                {
                    GlobalItemManager.SpawnWorldItem(itemObj, inventory);
                }
            }
            else
            {
                int amountGiven = 0;
                while (amountGiven < amount)
                {
                    itemObj = GlobalItemManager.Instance.CreateItem(generator);
                    if (!inventory.GiveItemServer(itemObj))
                    {
                        GlobalItemManager.SpawnWorldItem(itemObj, inventory);
                    }
                    amountGiven++;
                }
            }

            return true;
        }

        ////////////////////////////////////////
        ///     Время
        ////////////////////////////////////////

        private string ConvertSecondsToDate(double numOfSeconds)
        {
            int minutes = (int)(numOfSeconds / 60) % 60;
            int seconds = (int)numOfSeconds % 60;

            string SecondsToDate = "";
            string sMinutes = "";
            string sSeconds = "";

            if (minutes > 0)
            {
                sMinutes = minutes.ToString() + GetMsg("msg_Minutes") + " ";
            }

            if (seconds > 0)
            {
                sSeconds = seconds.ToString() + GetMsg("msg_Seconds");
            }

            SecondsToDate = sMinutes + sSeconds;
            if (SecondsToDate.EndsWith(" "))
                SecondsToDate = SecondsToDate.Substring(0, SecondsToDate.Length - 1);

            return SecondsToDate;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Достаем все данные нужного нам ивента фарма
        ////////////////////////////////////////////////////////////////////////////////////////

        FarmEvents getFarming(string name)
        {
            FarmEvents farme = (from x in configData.listFarmEvents where x.Name == name select x).FirstOrDefault();
            return farme;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        ///     АПИ / API
        ////////////////////////////////////////////////////////////////////////////////////////

        bool isEventFarming()
        {
            return farmStarted;
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Проверяем достаточное ли количество игроков онлайн 
        ////////////////////////////////////////////////////////////////////////////////////////

        bool getMinPlayers()
        {
            var allPlayers = GameManager.Instance.GetSessions().Values.ToList();
            if (allPlayers.Count() > configData.minPlayers)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Узнаем название(имя) игрового обекта
        ////////////////////////////////////////////////////////////////////////////////////////

        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        //    Находим игровую сессию по нику или стим ид
        ////////////////////////////////////////////////////////////////////////////////////////

        private PlayerSession FindSession(string nameOrIdOrIp)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;
            foreach (var i in sessions)
            {

                if (nameOrIdOrIp.Equals(i.Value.Identity.Name, StringComparison.OrdinalIgnoreCase) ||
                    nameOrIdOrIp.Equals(i.Value.SteamId.ToString()) || nameOrIdOrIp.Equals(i.Key.ipAddress))
                {
                    session = i.Value;
                    break;
                }

            }
            return session;
        }

        ////////////////////////////////////////
        ///     Проверка Сессии
        ////////////////////////////////////////

        public bool IsValidSession(PlayerSession session)
        {
            return session?.SteamId != null && session.IsLoaded && session.Identity.Name != null && session.Identity != null &&
                   session.WorldPlayerEntity?.transform?.position != null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator) => string.Join(seperator, list.Skip(first).ToArray());

        //////////////////////////////////////////////////////////////////////////////////////////
        /////      Функции Данных конфига, "запись(замена)" и "чтение"...
        //////////////////////////////////////////////////////////////////////////////////////////

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        /////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////
        ///     Разрешения (permissions, пермишены)
        ////////////////////////////////////////////////////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            uid = uid.ToString();
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        ///     ЧАТ
        //////////////////////////////////////////////////////////////////////////////////////

        string GetMsg(string key, string userID = "") => lang.GetMessage(key, this, userID);

        void BroadcastChat(string prefix, string msg) => Server.Broadcast(msg, prefix);

        void SendChatMessage(PlayerSession player, string prefix, string msg) => hurt.SendChatMessage(player, prefix, msg);

    }
}