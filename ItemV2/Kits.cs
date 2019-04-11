using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Kits", "Reneb", "1.0.5")]
    [Description("Create kits of items for players to use.")]
    /* ////////
	// Update for ItemV2 by Vladimir.Kzi version is 1.0.10, plugin author Reneb
	// Support for this ItemV2 plugin on www.oxide-russia.ru
	// Поддержка ItemV2 плагина на www.oxide-russia.ru
	//////// */
    class Kits : HurtworldPlugin
    {
        class ItemsList
        {
            public ItemGeneratorAsset ItemGen;
            public string ItemName;
            public int ItemId;
            public string ItemGuID;
        }

        List<ItemsList> allItemsList = new List<ItemsList>();

        private ConfigData configData;

        class ConfigData
        {
            public int itemIdentificationSystem { get; set; }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Plugin initialization
        //////////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            LoadVariables();
            LoadData();
            LoadMessages();
            configData.itemIdentificationSystem = 2;
            try
            {
                kitsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("Kits_Data");
            }
            catch
            {
                kitsData = new Dictionary<ulong, Dictionary<string, KitData>>();
            }
        }

        void OnServerInitialized()
        {
            InitializePermissions();
            AllItemsList();
        }

        void InitializePermissions()
        {
            foreach (var kit in storedData.Kits.Values)
            {
                if (string.IsNullOrEmpty(kit.permission)) continue;
                permission.RegisterPermission(kit.permission, this);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////
        ///     Конфиг
        ////////////////////////////////////////////////////////////////////////////////////////

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new configuration file for " + this.Title);
            var config = new ConfigData
            {
                itemIdentificationSystem = 3 // 0 - ItemGuID ; 1 - ItemId ; 2 ItemNAME
            };
            SaveConfig(config);
        }

        void LoadMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"MSG_Prefix", "<color=#7FFF00>[KIT]</color>"},
                {"MSG_NoPermission", "<color=red>You don't have access to this command.</color>"},
                {"MSG_KITRedeem","<color=green>Kit redeem.</color>"},
                {"MSG_KITNotExist", "<color=green>{kitname}</color> <color=red>not available!</color>"},
                {"MSG_Days", "d."},
                {"MSG_Hours", "h."},
                {"MSG_Minutes", "m."},
                {"MSG_Seconds", "s."},
                {"MSG_KITLeftUse", "left"},
                {"MSG_KITNoExistNow","<color=red>You are not allowed to kit at this moment!</color>"},
                {"MSG_KITNoLevel","<color=red>You don't have the level to use this kit!</color>"},
                {"MSG_KITNoPerm","<color=red>You do not have permissions to use this kit!</color>"},
                {"MSG_KITOut", "<color=red>You have already used it!</color>"},
                {"MSG_KITWait","<color=orange>You need to wait <color=red>{time}</color>, to use this kit.</color>"},
                {"MSG_KITEdition_1", "permission \"permission name\" => set the permission needed to get this kit"},
                {"MSG_KITEdition_2", "description \"description text here\" => set a description for this kit"},
                {"MSG_KITEdition_3", "authlevel XXX"},
                {"MSG_KITEdition_4", "cooldown XXX"},
                {"MSG_KITEdition_5", "max XXX"},
                {"MSG_KITEdition_6", "items => set new items for your kit (will copy your inventory)"},
                {"MSG_KITEdition_7", "hide TRUE/FALSE => dont show this kit in lists (EVER)"},
                {"MSG_KITForList", "<color=green>{name}</color> - <color=orange>{description}</color> - <color=red>{reason}</color>"},
                {"MSG_KITPlayer_0", "Commands:"},
                {"MSG_KITPlayer_1", "/kit => to get the list of kits"},
                {"MSG_KITPlayer_2", "/kit KITNAME => to redeem the kit"},
                {"MSG_KITAdmin_1", "/kit add KITNAME => add a kit"},
                {"MSG_KITAdmin_2", "/kit remove KITNAME => remove a kit"},
                {"MSG_KITAdmin_3", "/kit edit KITNAME => edit a kit"},
                {"MSG_KITAdmin_4", "/kit list => get a raw list of kits (the real full list)"},
                {"MSG_KITAdmin_5", "/kit give PLAYER/STEAMID KITNAME => give a kit to a player"},
                {"MSG_KITAdmin_6", "/kit resetkits => deletes all kits"},
                {"MSG_KITAdmin_7", "/kit resetdata => reset player data"},
                {"MSG_KITList", "<color=green>{name}</color> - <color=orange>{description}</color>"},
                {"MSG_KITResetAll", "Resetted all kits and player data!"},
                {"MSG_KITResetPlayer", "Resetted all player data"},
                {"MSG_KITPlayerReset", "Data player '{player}' has been reset!"},
                {"MSG_KITPlayerNoReset", "Player '{player}' has not data!"},
                {"MSG_KITNewExists", "This kit already exists"},
                {"MSG_KITNew", "You've created a new kit: {name}"},
                {"MSG_KITCmdGive", "/kit give PLAYER/STEAMID KITNAME"},
                {"MSG_KITCmdGiveNoPlayer", "No players found"},
                {"MSG_KITCmdGiveMultiple", "Multiple players found"},
                {"MSG_KITCmdNotExists", "This kit doesn't seem to exist!"},
                {"MSG_KITCmdGiven", "You gave {player} the kit: {kitname}"},
                {"MSG_KITCmdGivenReceive", "You've received the kit {1} from {0}!"},
                {"MSG_KITCmdEdit", "You are now editing the kit: {kitname}"},
                {"MSG_KITCmdRemove", "{kitname} was removed"},
                {"MSG_KITCmdNotInEdit", "You are not creating or editing a kit"},
                {"MSG_KITCmdEditDirty", "There was an error while getting this kit, was it changed while you were editing it?"},
                {"MSG_KITCmdItemsCopy","The items were copied from your inventory"},
                {"MSG_KITCmdBadARG", "{arg} is not a valid argument."},
                {"MSG_KITListAll", "List:"}
            }, this, "en");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"MSG_Prefix", "<color=#7FFF00>[KIT]</color>"},
                {"MSG_NoPermission", "<color=red>У Вас нет прав для использования этой команды.</color>"},
                {"MSG_KITRedeem","<color=green>Набор получен.</color>"},
                {"MSG_KITNotExist", "<color=green>{kitname}</color> <color=red>недоступен!</color>"},
                {"MSG_Days", "д."},
                {"MSG_Hours", "ч."},
                {"MSG_Minutes", "м."},
                {"MSG_Seconds", "с."},
                {"MSG_KITLeftUse", "осталось"},
                {"MSG_KITNoExistNow","<color=red>Нельзя использовать набор в данный момент!</color>"},
                {"MSG_KITNoLevel","<color=red>У Вас нет уровня, чтобы использовать этот набор!</color>"},
                {"MSG_KITNoPerm","<color=red>У Вас нет разрешений использовать этот набор!</color>"},
                {"MSG_KITOut", "<color=red>Вы уже использовали его!</color>"},
                {"MSG_KITWait","<color=orange>Вы должны, подождать <color=red>{time}</color>, для использования набора.</color>"},
                {"MSG_KITEdition_1", "permission \"permission name\" => разрешение для получения данного набора"},
                {"MSG_KITEdition_2", "description \"description текст тут\" => описание набора"},
                {"MSG_KITEdition_3", "authlevel XXX"},
                {"MSG_KITEdition_4", "cooldown XXX"},
                {"MSG_KITEdition_5", "max XXX"},
                {"MSG_KITEdition_6", "items => записывает новые предметы для набора (копирует из инвентаря)"},
                {"MSG_KITEdition_7", "hide TRUE/FALSE => не показывать этот набор в списках (никогда)"},
                {"MSG_KITForList", "<color=green>{name}</color> - <color=orange>{description}</color> - <color=red>{reason}</color>"},
                {"MSG_KITPlayer_0", "Команды:"},
                {"MSG_KITPlayer_1", "/kit => список наборов"},
                {"MSG_KITPlayer_2", "/kit KITNAME => получить набор"},
                {"MSG_KITAdmin_1", "/kit add KITNAME => добавить набор"},
                {"MSG_KITAdmin_2", "/kit remove KITNAME => удалить набор"},
                {"MSG_KITAdmin_3", "/kit edit KITNAME => редактировать набор"},
                {"MSG_KITAdmin_4", "/kit list => полный список наборов"},
                {"MSG_KITAdmin_5", "/kit give PLAYER/STEAMID KITNAME => выдать набор игроку"},
                {"MSG_KITAdmin_6", "/kit resetkits => удалить все наборы"},
                {"MSG_KITAdmin_7", "/kit resetdata => удалить данные игроков"},
                {"MSG_KITList", "<color=green>{name}</color> - <color=orange>{description}</color>"},
                {"MSG_KITResetAll", "Данные всех игроков и наборов были сброшены!"},
                {"MSG_KITResetPlayer", "Все данные игроков были сброшены"},
                {"MSG_KITPlayerReset", "Данные игрока '{player}' были сброшены!"},
                {"MSG_KITPlayerNoReset", "У игрока '{player}' нет данных!"},
                {"MSG_KITNewExists", "С таким названием уже существует набор"},
                {"MSG_KITNew", "Вы создали новый набор: {name}"},
                {"MSG_KITCmdGive", "/kit give PLAYER/STEAMID KITNAME"},
                {"MSG_KITCmdGiveNoPlayer", "Игрок не найден"},
                {"MSG_KITCmdGiveMultiple", "Несколько игроков найдены"},
                {"MSG_KITCmdNotExists", "С таким названием наборов не существует!"},
                {"MSG_KITCmdGiven", "Вы дали {player} набор: {kitname}"},
                {"MSG_KITCmdGivenReceive", "Вы получили набор {0} от {1} наслаждайтесь!"},
                {"MSG_KITCmdEdit", "Вы редактируете: {kitname}"},
                {"MSG_KITCmdRemove", "{kitname} был удален"},
                {"MSG_KITCmdNotInEdit", "Вы не создаете и не редактируете набор"},
                {"MSG_KITCmdEditDirty", "При получении данный о наборе произошла ошибка, был ли он изменен, когда вы его редактировали?"},
                {"MSG_KITCmdItemsCopy","Предметы были скопированы из вашего инвентаря"},
                {"MSG_KITCmdBadARG", "{arg} неправельный аргумент."},
                {"MSG_KITListAll", "Список:"}
            }, this, "ru");
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Configuration
        //////////////////////////////////////////////////////////////////////////////////////////

        void OnPlayerRespawn(PlayerSession session)
        {
            if (!storedData.Kits.ContainsKey("autokit")) return;
            var thereturn = Interface.Oxide.CallHook("canRedeemKit", session, "autokit");
            if (thereturn == null)
            {
                var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                for (var j = 0; j < playerinv.Capacity; j++)
                {
                    if (playerinv.GetSlot(j) == null) continue;
                    if (playerinv.GetSlot(j).ItemId == null) continue;
                    Singleton<ClassInstancePool>.Instance.ReleaseInstanceExplicit(playerinv.GetSlot(j));
                    playerinv.SetSlot(j, null);
                }
                GiveKit(session, "autokit");
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Kit Creator
        //////////////////////////////////////////////////////////////////////////////////////////

        List<KitItem> GetPlayerItems(PlayerSession session)
        {
            var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();

            var kititems = new List<KitItem>();
            for (var i = 0; i < playerinv.Capacity; i++)
            {
                var item = playerinv.GetSlot(i);
                if (item?.Generator == null) continue;
                string IdGuIdName;
                switch (configData.itemIdentificationSystem) // 0 - ItemGuID ; 1 - ItemId ; 2 ItemNAME
                {
                    case 1:
                        IdGuIdName = item.Generator.GeneratorId.ToString();
                        break;
                    case 2:
                        IdGuIdName = item.Generator.name.ToString();
                        break;
                    default:
                        IdGuIdName = RuntimeHurtDB.Instance.GetGuid(item.Generator).ToString();
                        break;
                }
                kititems.Add(new KitItem
                {
                    guID = IdGuIdName,
                    amount = item.StackSize,
                    slot = i
                });
            }
            return kititems;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Kit Redeemer
        //////////////////////////////////////////////////////////////////////////////////////////

        private void TryGiveKit(PlayerSession session, string kitname)
        {
            var success = CanRedeemKit(session, kitname) as string;
            if (success != null)
            {
                ShowMsg(session, Msg("MSG_Prefix"), success);
                return;
            }
            success = GiveKit(session, kitname) as string;
            if (success != null)
            {
                ShowMsg(session, Msg("MSG_Prefix"), success);
                return;
            }
            ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITRedeem"));

            ProccessKitGiven(session, kitname);
        }

        void ProccessKitGiven(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return;

            var kitData = GetKitData(session.SteamId.m_SteamID, kitname);
            if (kit.max > 0) kitData.max += 1;

            if (kit.cooldown > 0) kitData.cooldown = CurrentTime() + kit.cooldown;
        }

        object GiveKit(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return Msg("MSG_KITNotExist").Replace("{kitname}", kitname);

            var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            var amanager = Singleton<AlertManager>.Instance;
            var itemmanager = Singleton<GlobalItemManager>.Instance;
            foreach (var kitem in kit.items)
            {
                if (playerinv.GetSlot(kitem.slot) == null)
                {
                    ItemGeneratorAsset generator = FindItem(kitem.guID.ToString());
                    if (generator == null)
                    {
                        PrintWarning("Something is wrong with the item:" + kitem.guID.ToString());
                        continue;
                    }
                    ItemObject item = itemmanager.CreateItem(generator, kitem.amount);
                    playerinv.SetSlot(kitem.slot, item);
                    amanager.ItemReceivedServer(item, item.StackSize, session.Player);
                    playerinv.Invalidate(false);
                }
                else
                {
                    ItemGeneratorAsset generator = FindItem(kitem.guID.ToString());
                    if (generator == null)
                    {
                        PrintWarning("Something is wrong with the item:" + kitem.guID.ToString());
                        continue;
                    }
                    itemmanager.GiveItem(session.Player, generator, kitem.amount);
                }
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Check Kits
        //////////////////////////////////////////////////////////////////////////////////////////

        bool isKit(string kitname) => !string.IsNullOrEmpty(kitname) && storedData.Kits.ContainsKey(kitname);

        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        bool CanSeeKit(PlayerSession session, string kitname, out string reason)
        {
            reason = string.Empty;
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return false;
            if (kit.hide) return false;
            if (kit.authlevel > 0)
                if (!session.IsAdmin) return false;
            if (!string.IsNullOrEmpty(kit.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), kit.permission)) return false;
            if (kit.max > 0)
            {
                var left = GetKitData(session.SteamId.m_SteamID, kitname).max;
                if (left >= kit.max)
                {
                    reason += "0 " + Msg("MSG_KITLeftUse") + "";
                    return false;
                }
                reason += $"{(kit.max - left)} " + Msg("MSG_KITLeftUse") + "";
            }
            if (kit.cooldown > 0)
            {
                var cd = GetKitData(session.SteamId.m_SteamID, kitname).cooldown;
                var ct = CurrentTime();
                if (cd > ct && cd != 0.0)
                {
                    reason += $"{ConvertSecondsToDate(Math.Abs(Math.Ceiling(cd - ct)))}";
                    return false;
                }
            }
            return true;
        }

        object CanRedeemKit(PlayerSession session, string kitname)
        {
            Kit kit;
            if (string.IsNullOrEmpty(kitname) || !storedData.Kits.TryGetValue(kitname, out kit)) return Msg("MSG_KITNotExist").Replace("{kitname}", kitname);

            var thereturn = Interface.Oxide.CallHook("canRedeemKit", session, kitname);
            if (thereturn != null)
            {
                if (thereturn is string) return thereturn;
                return Msg("MSG_KITNoExistNow");
            }

            if (kit.authlevel > 0)
                if (!session.IsAdmin) return Msg("MSG_KITNoLevel");

            if (!string.IsNullOrEmpty(kit.permission))
                if (!permission.UserHasPermission(session.SteamId.ToString(), kit.permission))
                    return Msg("MSG_KITNoPerm");

            var kitData = GetKitData(session.SteamId.m_SteamID, kitname);
            if (kit.max > 0)
                if (kitData.max >= kit.max) return Msg("MSG_KITOut");

            if (kit.cooldown > 0)
            {
                var ct = CurrentTime();
                if (kitData.cooldown > ct && kitData.cooldown != 0.0)
                    return Msg("MSG_KITWait").Replace("{time}", ConvertSecondsToDate(Math.Abs(Math.Ceiling(kitData.cooldown - ct))));
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Kit Class
        //////////////////////////////////////////////////////////////////////////////////////

        class KitItem
        {
            public string guID;
            public int amount;
            public int slot;
        }

        class Kit
        {
            public string name;
            public string description;
            public int max;
            public double cooldown;
            public int authlevel;
            public bool hide;
            public string permission;
            public List<KitItem> items = new List<KitItem>();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Data Manager
        //////////////////////////////////////////////////////////////////////////////////////

        void SaveKitsData() => Interface.Oxide.DataFileSystem.WriteObject("Kits_Data", kitsData);

        StoredData storedData;
        Dictionary<ulong, Dictionary<string, KitData>> kitsData;

        class StoredData
        {
            public Dictionary<string, Kit> Kits = new Dictionary<string, Kit>();
        }
        class KitData
        {
            public int max;
            public double cooldown;
        }
        void ResetData()
        {
            kitsData.Clear();
            SaveKitsData();
        }

        void Unload() => SaveKitsData();
        void OnServerSave() => SaveKitsData();

        void SaveKits() => Interface.Oxide.DataFileSystem.WriteObject("Kits", storedData);

        void LoadData()
        {
            var kits = Interface.Oxide.DataFileSystem.GetFile("Kits");
            try
            {
                kits.Settings.NullValueHandling = NullValueHandling.Ignore;
                storedData = kits.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
            kits.Settings.NullValueHandling = NullValueHandling.Include;
        }

        KitData GetKitData(ulong userID, string kitname)
        {
            Dictionary<string, KitData> kitDatas;
            if (!kitsData.TryGetValue(userID, out kitDatas)) kitsData[userID] = kitDatas = new Dictionary<string, KitData>();
            KitData kitData;
            if (!kitDatas.TryGetValue(kitname, out kitData)) kitDatas[kitname] = kitData = new KitData();
            return kitData;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Kit Editor
        //////////////////////////////////////////////////////////////////////////////////////

        readonly Dictionary<ulong, string> kitEditor = new Dictionary<ulong, string>();

        //////////////////////////////////////////////////////////////////////////////////////
        // Console Command
        //////////////////////////////////////////////////////////////////////////////////////

        List<PlayerSession> FindPlayer(string arg)
        {
            var listPlayers = new List<PlayerSession>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);
            var lowerarg = arg.ToLower();

            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var session = pair.Value;
                if (!session.IsLoaded) continue;
                if (steamid != 0L)
                    if (session.SteamId.m_SteamID == steamid)
                    {
                        listPlayers.Clear();
                        listPlayers.Add(session);
                        return listPlayers;
                    }
                var lowername = session.Identity.Name.ToLower();
                if (lowername.Contains(lowerarg)) listPlayers.Add(session);
            }
            return listPlayers;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Command
        //////////////////////////////////////////////////////////////////////////////////////

        bool HasAccess(PlayerSession session) => session.IsAdmin;

        void SendListKitEdition(PlayerSession session)
        {
            ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITPlayer_0"));
            ShowMsg(session, null, Msg("MSG_KITEdition_1"));
            ShowMsg(session, null, Msg("MSG_KITEdition_2"));
            ShowMsg(session, null, Msg("MSG_KITEdition_3"));
            ShowMsg(session, null, Msg("MSG_KITEdition_4"));
            ShowMsg(session, null, Msg("MSG_KITEdition_5"));
            ShowMsg(session, null, Msg("MSG_KITEdition_6"));
            ShowMsg(session, null, Msg("MSG_KITEdition_7"));
        }

        [ChatCommand("kits")]
        void cmdKits(PlayerSession session, string command, string[] args)
        {
            var reason = string.Empty;
            ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITListAll"));
            foreach (var pair in storedData.Kits)
            {
                var cansee = CanSeeKit(session, pair.Key, out reason);
                if (!cansee && string.IsNullOrEmpty(reason)) continue;
                ShowMsg(session, null, Msg("MSG_KITForList").Replace("{name}", pair.Value.name).Replace("{description}", pair.Value.description).Replace("{reason}", reason));
            }
            return;
        }

        [ChatCommand("kit")]
        void cmdKit(PlayerSession session, string command, string[] args)
        {
            if (args.Length == 0)
            {
                var reason = string.Empty;
                ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITListAll"));
                foreach (var pair in storedData.Kits)
                {
                    var cansee = CanSeeKit(session, pair.Key, out reason);
                    if (!cansee && string.IsNullOrEmpty(reason)) continue;
                    ShowMsg(session, null, Msg("MSG_KITForList").Replace("{name}", pair.Value.name).Replace("{description}", pair.Value.description).Replace("{reason}", reason));
                }
                return;
            }
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "help":
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITPlayer_0"));
                        ShowMsg(session, null, Msg("MSG_KITPlayer_1"));
                        ShowMsg(session, null, Msg("MSG_KITPlayer_2"));
                        if (!HasAccess(session)) return;
                        ShowMsg(session, null, Msg("MSG_KITAdmin_1"));
                        ShowMsg(session, null, Msg("MSG_KITAdmin_2"));
                        ShowMsg(session, null, Msg("MSG_KITAdmin_3"));
                        ShowMsg(session, null, Msg("MSG_KITAdmin_4"));
                        ShowMsg(session, null, Msg("MSG_KITAdmin_5"));
                        ShowMsg(session, null, Msg("MSG_KITAdmin_6"));
                        ShowMsg(session, null, Msg("MSG_KITAdmin_7"));
                        break;
                    case "add":
                    case "remove":
                    case "edit":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_NoPermission")); return; }
                        ShowMsg(session, Msg("MSG_Prefix"), $"/kit {args[0]} KITNAME");
                        break;
                    case "give":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_NoPermission")); return; }
                        ShowMsg(session, Msg("MSG_Prefix"), "/kit give PLAYER/STEAMID KITNAME");
                        break;
                    case "list":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_NoPermission")); return; }
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITListAll"));
                        foreach (var kit in storedData.Kits.Values) ShowMsg(session, null, Msg("MSG_KITList").Replace("{name}", kit.name).Replace("{description}", kit.description));
                        break;
                    case "items":
                        break;
                    case "resetkits":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_NoPermission")); return; }
                        storedData.Kits.Clear();
                        kitEditor.Clear();
                        ResetData();
                        SaveKits();
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITResetAll"));
                        break;
                    case "resetdata":
                        if (!HasAccess(session)) { ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_NoPermission")); return; }
                        ResetData();
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITResetPlayer"));
                        break;
                    default:
                        TryGiveKit(session, args[0].ToLower());
                        break;
                }
                if (args[0] != "items") return;

            }
            if (!HasAccess(session)) { ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_NoPermission")); return; }

            string kitname;
            switch (args[0])
            {
                case "add":
                    kitname = args[1].ToLower();
                    if (storedData.Kits.ContainsKey(kitname))
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITNewExists"));
                        return;
                    }
                    storedData.Kits[kitname] = new Kit { name = args[1] };
                    kitEditor[session.SteamId.m_SteamID] = kitname;
                    ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITNew").Replace("{name}", args[1]));
                    SendListKitEdition(session);
                    break;
                case "give":
                    if (args.Length < 3)
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdGive"));
                        return;
                    }
                    kitname = args[2].ToLower();
                    if (!storedData.Kits.ContainsKey(kitname))
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdNotExists"));
                        return;
                    }
                    var findPlayers = FindPlayer(args[1]);
                    if (findPlayers.Count == 0)
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdGiveNoPlayer"));
                        return;
                    }
                    if (findPlayers.Count > 1)
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdGiveMultiple"));
                        return;
                    }
                    GiveKit(findPlayers[0], kitname);
                    ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdGiven").Replace("{player}", findPlayers[0].Identity.Name).Replace("{kitname}", storedData.Kits[kitname].name));
                    ShowMsg(findPlayers[0], Msg("MSG_Prefix"), string.Format(Msg("MSG_KITCmdGivenReceive"), session.Identity.Name, storedData.Kits[kitname].name));
                    break;
                case "edit":
                    kitname = args[1].ToLower();
                    if (!storedData.Kits.ContainsKey(kitname))
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdNotExists"));
                        return;
                    }
                    kitEditor[session.SteamId.m_SteamID] = kitname;
                    ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdEdit").Replace("{kitname}", kitname));
                    SendListKitEdition(session);
                    break;
                case "remove":
                    kitname = args[1].ToLower();
                    if (!storedData.Kits.Remove(kitname))
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdNotExists"));
                        return;
                    }
                    ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdRemove").Replace("{kitname}", kitname));
                    if (kitEditor[session.SteamId.m_SteamID] == kitname) kitEditor.Remove(session.SteamId.m_SteamID);
                    break;
                case "resetdata":
                    var fPlayers = FindPlayer(args[1]);
                    if (fPlayers.Count == 0)
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdGiveNoPlayer"));
                        return;
                    }
                    if (fPlayers.Count > 1)
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdGiveMultiple"));
                        return;
                    }
                    if (!kitsData.ContainsKey((ulong)fPlayers[0].SteamId))
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITPlayerNoReset").Replace("{player}", fPlayers[0].Identity.Name));
                        return;
                    }
                    else
                    {
                        kitsData.Remove((ulong)fPlayers[0].SteamId);
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITPlayerReset").Replace("{player}", fPlayers[0].Identity.Name));
                    }
                    break;
                default:
                    if (!kitEditor.TryGetValue(session.SteamId.m_SteamID, out kitname))
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdNotInEdit"));
                        return;
                    }
                    Kit kit;
                    if (!storedData.Kits.TryGetValue(kitname, out kit))
                    {
                        ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdEditDirty"));
                        return;
                    }
                    for (var i = 0; i < args.Length; i++)
                    {
                        object editvalue;
                        var key = args[i].ToLower();
                        switch (key)
                        {
                            case "items":
                                kit.items = GetPlayerItems(session);
                                ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdItemsCopy"));
                                continue;
                            case "name":
                                continue;
                            case "description":
                                editvalue = kit.description = args[++i];
                                break;
                            case "max":
                                editvalue = kit.max = int.Parse(args[++i]);
                                break;
                            case "cooldown":
                                editvalue = kit.cooldown = double.Parse(args[++i]);
                                break;
                            case "authlevel":
                                editvalue = kit.authlevel = int.Parse(args[++i]);
                                break;
                            case "hide":
                                editvalue = kit.hide = bool.Parse(args[++i]);
                                break;
                            case "permission":
                                editvalue = kit.permission = args[++i];
                                InitializePermissions();
                                break;
                            default:
                                ShowMsg(session, Msg("MSG_Prefix"), Msg("MSG_KITCmdBadARG").Replace("{arg}", args[i]));
                                continue;
                        }
                        ShowMsg(session, Msg("MSG_Prefix"), $"{key} set to {editvalue ?? "null"}");
                    }
                    break;
            }
            SaveKits();
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
                aitem = items.First(i => i.ItemName.ToLower() == itemNameOrIdOrGuid.ToLower());
                item = aitem.ItemGen;
            }
            else
            {
                item = null;
            }

            return item;
        }

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

        ////////////////////////////////////////
        ///     Время
        ////////////////////////////////////////

        private string ConvertSecondsToDate(double numOfSeconds)
        {
            int days = (int)(numOfSeconds / 3600) / 24;
            int hours = (int)(numOfSeconds / 3600);
            int minutes = (int)(numOfSeconds / 60) % 60;
            int seconds = (int)numOfSeconds % 60;

            string SecondsToDate = "";
            string sDays = "";
            string sHours = "";
            string sMinutes = "";
            string sSeconds = "";

            if (days > 0)
            {
                sDays = days.ToString() + Msg("MSG_Days") + " ";
            }

            if (hours > 0)
            {
                sHours = hours.ToString() + Msg("MSG_Hours") + " ";
            }

            if (minutes > 0)
            {
                sMinutes = minutes.ToString() + Msg("MSG_Minutes") + " ";
            }

            if (seconds > 0)
            {
                sSeconds = seconds.ToString() + Msg("MSG_Seconds") + " ";
            }

            SecondsToDate = sDays + sHours + sMinutes + sSeconds;
            if (SecondsToDate.EndsWith(" "))
                SecondsToDate = SecondsToDate.Substring(0, SecondsToDate.Length - 1);

            return SecondsToDate;
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
        void ShowMsg(PlayerSession session, string prefix, string msg) => hurt.SendChatMessage(session, prefix, msg);
        void BroadcastMsg(string prefix, string msg) => Server.Broadcast(msg, prefix);
    }
}
