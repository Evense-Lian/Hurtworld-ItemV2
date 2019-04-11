using System.Collections.Generic;
using System.Linq;
using System;
using uLink;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GiveItem", "Noviets", "1.0.6")]
    [Description("Gives an item to anyone or everyone")]

    class GiveItem : HurtworldPlugin
    {

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","Нет прав"},
                {"playernotfound","Не удалось найти {Player}"},
                {"invalid","<i><color=cyan>|Выдача|</color></i> /g <ник> <ид> <кол>\n                /gall <ид> <кол>"},
                {"invaliditem","Неверный ид"},
                {"success","{Player} получил {ItemName} X {Amount}"}
            };

            lang.RegisterMessages(messages, this);
        }
        void Loaded()
        {
            AllItemsList();
            permission.RegisterPermission("giveitem.use", this);
            LoadDefaultMessages();
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        private PlayerSession GetSession(string source)
        {
            foreach (PlayerSession session in GameManager.Instance.GetSessions().Values)
            {
                if (session != null && session.IsLoaded)
                {
                    if (source.ToLower() == session.Identity.Name.ToLower())
                        return session;
                }
            }
            foreach (PlayerSession session in GameManager.Instance.GetSessions().Values)
            {
                if (session != null && session.IsLoaded)
                {
                    if (session.Identity.Name.ToLower().Contains(source.ToLower()))
                        return session;
                }
            }
            return null;
        }


        [ChatCommand("g")]
        void GiveItemCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "giveitem.use") || session.IsAdmin)
            {
                if (args.Length == 2)
                {
                    int ItemID = Convert.ToInt32(args[0]);
                    int Amount = Convert.ToInt32(args[1]);
                    var ItemMgr = Singleton<GlobalItemManager>.Instance;
                    var Inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                    var ItemName = GlobalItemManager.Instance.GetItem(ItemID);
                    if (ItemName != null)
                    {
                        //ItemMgr.GiveItem(target.Player, ItemMgr.GetItem(ItemID).Generator, Amount);
                        var item = FindItem(ItemID.ToString());
                        //GlobalItemManager.SpawnWorldItem(item, Inv);
                        //GlobalItemManager.Instance.GiveItemWithSpill(item, Inv);
                        ItemMgr.GiveItem(session.Player, item, Amount);
                        SendNotification(session, (Msg("success").Replace("{Amount}", Amount.ToString()).Replace("{ItemName}", item.GetNameKey()).Replace("{Player}", session.Identity.Name)));
                    }
                    else
                        SendNotification(session, Msg("invaliditem"));
                }
                else
                    hurt.SendChatMessage(session, null, Msg("invalid"));
            }
            else
                hurt.SendChatMessage(session, null, Msg("nopermission"));
        }

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

        class ItemsList
        {
            public ItemGeneratorAsset ItemGen;
            public string ItemName;
            public int ItemId;
            public string ItemGuID;
        }

        List<ItemsList> allItemsList = new List<ItemsList>();

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

        [ChatCommand("gall")]
        void GiveAllCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "giveitem.use") || session.IsAdmin)
            {
                if (args.Length >= 2)
                {
                    int Range = 99999;
                    foreach (PlayerSession ses in GameManager.Instance.GetSessions().Values)
                    {

                        if (ses != null && ses.IsLoaded)
                        {
                            var Inv = ses.WorldPlayerEntity.GetComponent<PlayerInventory>();
                            if (Vector3.Distance(ses.WorldPlayerEntity.transform.position, session.WorldPlayerEntity.transform.position) <= Range)
                            {
                                Dictionary<int, global::ItemGeneratorAsset> itemGenerators = global::Singleton<global::GlobalItemManager>.Instance.ItemGenerators;
                                int count = int.Parse(args[2]);
                                int num7 = (args.Length > 3) ? int.Parse(args[3]) : 0;
                                int rarity2 = (args.Length > 4) ? int.Parse(args[4]) : 0;
                                int num8 = 0;
                                if (!int.TryParse(args[1], out num8))
                                {
                                    List<KeyValuePair<int, global::ItemGeneratorAsset>> list3 = Enumerable.ToList<KeyValuePair<int, global::ItemGeneratorAsset>>(Enumerable.Where<KeyValuePair<int, global::ItemGeneratorAsset>>(global::Singleton<global::GlobalItemManager>.Instance.GetGenerators(), (KeyValuePair<int, global::ItemGeneratorAsset> x) => x.Value.DataProvider.NameKey.ToLower().Contains(args[1]) && x.Value != null));
                                    foreach (KeyValuePair<int, global::ItemGeneratorAsset> keyValuePair2 in list3)
                                    {
                                        global::Singleton<global::GlobalItemManager>.Instance.GiveItemClient(keyValuePair2.Value, count, (ushort)num7, rarity2);
                                    }
                                    if (list3.Count == 0)
                                    {
                                        Debug.Log("No items found under the name " + args[1]);
                                    }
                                }
                                else
                                {
                                    global::Singleton<global::GlobalItemManager>.Instance.GiveItemClient(itemGenerators[num8], count, (ushort)num7, rarity2);
                                }
                            }
                        }
                    }
                }
                else
                    hurt.SendChatMessage(session, null, Msg("invalid"));
            }
            else
                hurt.SendChatMessage(session, null, Msg("nopermission"));
        }
        [ChatCommand("itemid")]
        void itemIDCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "giveitem.use") || session.IsAdmin)
            {
                var ItemMgr = Singleton<GlobalItemManager>.Instance;
                var i = 0;
                while (i < 350)
                {
                    var ItemName = GlobalItemManager.Instance.GetItem(i);
                    if (ItemName != null)
                    {
                        var CleanName = ItemName.GetNameKey().Replace("Items/", "").Replace("AmmoType/", "").Replace("Machines/", "");
                        if (CleanName.ToLower().Contains(args[0].ToLower()))
                            SendNotification(session, "<color=orange>" + CleanName + "</color>  ID:<color=yellow> " + i + "</color>");
                    }
                    i++;
                }
            }
        }
        void SendNotification(PlayerSession session, string message) => AlertManager.Instance.GenericTextNotificationServer(message, session.Player);
    }
}