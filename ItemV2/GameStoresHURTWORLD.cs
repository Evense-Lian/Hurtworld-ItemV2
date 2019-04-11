//Reference: UnityEngine.UI
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using Steamworks;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("GameStoreV2", "Varzhak and Sstine", "1.3.3", ResourceId = 715)]
    class GameStoresHURTWORLD : HurtworldPlugin
    {
        private string Request => $"https://gamestores.ru/api/?shop_id={Config["SHOP.ID"]}&secret={Config["SECRET.KEY"]}&server={Config["SERVER.ID"]}";
        private List<Dictionary<string, object>> Stats = new List<Dictionary<string, object>>();
        private List<Dictionary<string, object>> Leaves = new List<Dictionary<string, object>>();

        #region [Override] Load default configurations
        protected override void LoadDefaultConfig()
        {
            Config["SHOP.ID"] = "0";
            Config["SERVER.ID"] = "0";
            Config["SECRET.KEY"] = "KEY";
            Config["TOP.USERS"] = false;
        }
        #endregion

        #region[HookMethod] OnServerInitialized
        private void OnServerInitialized()
        {
            if (Config["SECRET.KEY"].ToString().Contains("KEY"))
            {
                Debug.LogError("Plugin isn't configured");
            }
            else
            {
                webrequest.EnqueueGet($"{this.Request}&info=true", (code, response) =>
                {
                    switch (code)
                    {
                        case 0:
                            Debug.LogError("Api does not responded to a request");
                            break;
                        case 200:
                            Debug.LogWarning("Plugin loaded");
                            break;
                        case 404:
                            Debug.LogError("Response code: 404, please check your configurations");
                            break;
                    }
                }, this);
            }
        }
        #endregion

        #region[Method] Executing - WebRequest callback handler
        private void Executing(PlayerSession Player, string response, int code)
        {
            switch (code)
            {
                case 0:
                    Debug.LogError("Api does not responded to a request");
                    break;
                case 200:
                    Dictionary<string, object> Response = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, new KeyValuesConverter());
                    if (Response != null)
                    {
                        switch (Convert.ToInt32(Response["code"]))
                        {
                            case 100:
                                int i = 0;
                                List<object> data = Response["data"] as List<object>;
                                foreach (object pair in data)
                                {
                                    if (i >= 14)
                                    {
                                        hurt.SendChatMessage(Player, null, $"Вы не можете получить больше 14 предметов за раз.");
                                        return;
                                    }

                                    Dictionary<string, object> iteminfo = pair as Dictionary<string, object>;

                                    if (iteminfo.ContainsKey("command"))
                                    {
                                        string command = iteminfo["command"].ToString().Replace('\n', '|').Trim('\"').Replace("%steamid%", Player.SteamId.m_SteamID.ToString()).Replace("%username%", Player.Identity.Name);
                                        String[] CommandArray = command.Split('|');
                                        foreach (var substring in CommandArray)
                                        {
                                            if (substring.Contains("additem"))
                                            {
                                                if (GetFreeSlots(Player) == 0)
                                                {
                                                    hurt.SendChatMessage(Player, null, $"В инвентаре недостаточно места для получения <color=lime>\"{iteminfo["name"]}\"</color>");
                                                    return;
                                                }
                                                i++;
                                            }

                                            ConsoleManager.Instance.ExecuteCommand(substring);
                                        }
                                        hurt.SendChatMessage(Player, null, $"Получен товар из магазина: <color=lime>\"{iteminfo["name"]}\"</color>.");
                                        SendGived(new Dictionary<string, string>() { { "gived", "true" }, { "id", $"{iteminfo["id"]}" } }, Player);
                                    }
                                    else
                                    {
                                        hurt.SendChatMessage(Player, null, $"Предмет не выдан. Обратитесь к админу для помощи.");
                                        Puts($"[StoreError] Предмет не выдан! {iteminfo["name"]}");
                                        SendGived(new Dictionary<string, string>() { { "gived", "true" }, { "id", $"{iteminfo["id"]}" } }, Player);
                                    }



                                    //int ItemID = Convert.ToInt32(iteminfo["item_id"]);
                                    //int Amount = Convert.ToInt32(iteminfo["amount"]);

                                    //hurt.SendChatMessage(Player, null, $"Получен товар из магазина: <color=lime>\"{iteminfo["name"]}\"</color> в количестве <color=lime>{Amount}</color> шт.");
                                    //SendGived(new Dictionary<string, string>() { { "gived", "true" }, { "id", $"{iteminfo["id"]}" } }, Player);

                                    /* if (Player.WorldPlayerEntity.GetComponent<Inventory>().GiveItemServer(Item))
                                    {
                                        hurt.SendChatMessage(Player, null, $"Получен товар из магазина: <color=lime>\"{iteminfo["name"]}\"</color> в количестве <color=lime>{Amount}</color> шт.");
                                        SendGived(new Dictionary<string, string>() { {"gived", "true"}, {"id", $"{iteminfo["id"]}"} }, Player);
                                    } else {
                                        hurt.SendChatMessage(Player, null, $"В инвентаре недостаточно места для получения <color=lime>\"{iteminfo["name"]}\"</color>");
                                        return;
                                    } */

                                }
                                break;
                            case 104:
                                hurt.SendChatMessage(Player, null, $"Ваша корзина пуста!");
                                break;
                        }
                    }
                    else
                        Debug.LogWarning(response);
                    break;
                case 404:
                    Debug.LogError("Response code: 404, please check your configurations");
                    break;
            }
        }
        #endregion

        #region [Method] SendResult - Send WebRequest result
        private void SendResult(Dictionary<string, string> Args) => SendRequest(Args);
        #endregion

        #region[Method] SendRequest - Send request to GameStore API
        private void SendRequest(Dictionary<string, string> Args, PlayerSession Player = null)
        {
            string request = $"{Request}&{string.Join("&", Args.Select(x => x.Key + "=" + x.Value).ToArray())}";
            webrequest.EnqueueGet(request, (code, res) => { if (Player != null) Executing(Player, res, code); }, this);
        }
        #endregion

        #region[Method] SendGived - Send request about givint item to GameStore API
        private void SendGived(Dictionary<string, string> Args, PlayerSession Player = null)
        {
            string Request = $"{this.Request}&{string.Join("&", Args.Select(x => x.Key + "=" + x.Value).ToArray())}";
            webrequest.EnqueueGet(Request, (code, res) => { if (Player != null) TestRequestSent(Player, res, code, Args); }, this);
        }
        #endregion     

        #region[Method] TestRequestSent - Check send request
        private void TestRequestSent(PlayerSession Player, string response, int code, Dictionary<string, string> Args)
        {
            if (code == 200)
            {
                Dictionary<string, object> Resp = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, new KeyValuesConverter());
                if (Resp["result"].ToString() != "success")
                {
                    Debug.LogError("Api do not responded to request. Trying again (Player received items but it was not recorded)");
                    SendGived(Args, Player);
                }
            }
            else
            {
                Debug.LogError("Api do not responded to request. Trying again (Player received items but it was not recorded)");
                SendGived(Args, Player);
            }
        }
        #endregion   

        #region[ChatCommand] /store
        [ChatCommand("store")]
        private void cmdStore(PlayerSession Player, string command, string[] args)
        { SendRequest(new Dictionary<string, string>() { { "items", "true" }, { "steam_id", $"{Player.SteamId.m_SteamID}" } }, Player); }
        #endregion


        #region[Method] GetConnectionSeconds
        public int GetConnectionSeconds(float connectionTime) => (int)(Time.realtimeSinceStartup - connectionTime);
        #endregion

        #region[Method] GetNameOfObject
        public static string GetNameOfObject(GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            string name = ManagerInstance.GetDescriptionKey(obj);
            return name.Length > 3 ? name.Remove(name.Length - 3, 3) : name;
        }
        #endregion

        #region[Method] FindPlayer
        public static PlayerSession FindPlayer(object initials)
        {
            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var session = pair.Value;
                if (!session.IsLoaded) continue;

                if (session.Identity.Name.ToLower() == initials.ToString().ToLower())
                    return session;
            }
            return null;
        }
        #endregion

        #region[HookMethod] OnEntityDeath
        [HookMethod("OnPlayerDeath")]
        private void OnPlayerDeath(PlayerSession victim, EntityEffectSourceData source)
        {
            if (Convert.ToBoolean(Config["TOP.USERS"]))
            {
                Dictionary<string, object> args = new Dictionary<string, object>();

                PlayerSession killer = FindPlayer(GetNameOfObject(source.EntitySource));
                //PlayerSession killer = null;

                if (killer != null)
                {
                    args["player_id"] = killer.SteamId.m_SteamID.ToString();
                }
                else
                {
                    args["player_id"] = "1";
                }

                args["victim_id"] = victim.SteamId.m_SteamID.ToString();
                args["type"] = "kill";

                args["time"] = Convert.ToInt32((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();

                Stats.Add(args);
            }
        }
        #endregion

        #region Kaidoz

        object OnServerCommand(string cmd, string[] args)
        {
            if (cmd != "additem")
                return null;

            try
            {
                string itemname = string.Empty;
                var st = args.Skip(2).ToList();
                for (int d = 0; d < st.Count(); d++)
                {
                    itemname += st[d] + " ";
                }
                itemname = itemname.Replace("@", string.Empty);
                itemname = itemname.Remove(itemname.Length - 1);
                Puts(itemname);
                var ItemMgr = Singleton<GlobalItemManager>.Instance;
                var item = getItemFromName(itemname);
                ItemMgr.GiveItem(Player.Find(args[0]).Player, item, Convert.ToInt32(args[1]));
            }
            catch
            {
                Puts("Неправильно набрана команда в магазине! Пример: additem %steamid% 2 Items/Raw Materials/Amber");
            }
            return true;
        }

        ItemObject GetItem(string name, int count)
        {
            var imng = Singleton<GlobalItemManager>.Instance;
            var item = getItemFromName(name);
            var obj = imng.CreateItem(item, count);
            return obj;
        }

        public ItemGeneratorAsset getItemFromName(string name)
        {
            foreach (var it in GlobalItemManager.Instance.GetGenerators())
                if (name == it.Value.GetNameKey() || name == it.Value.name)
                    return GlobalItemManager.Instance.GetGenerators()[it.Value.GeneratorId];

            return GlobalItemManager.Instance.GetGenerators()[1];
        }

        #endregion

        #region[HookMethod] OnPlayerDisconnected
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(PlayerSession player)
        {
            if (Convert.ToBoolean(Config["TOP.USERS"]))
            {
                Dictionary<string, object> args = new Dictionary<string, object>();

                args["player_id"] = player.SteamId.m_SteamID;
                args["played"] = GetConnectionSeconds(player.ConnectionTime);
                args["username"] = player.Identity.Name;
                args["time"] = Convert.ToInt32((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();

                Leaves.Add(args);

                if (Leaves.Count >= 10)
                    SendLeavesInfo();
            }
        }
        #endregion

        #region[HookMethod] OnServerSave
        [HookMethod("OnServerSave")]
        private void OnServerSave()
        {
            SendKillsInfo();
            SendLeavesInfo();
        }
        #endregion

        #region[HookMethod] SendKillsInfo
        private void SendKillsInfo()
        {
            if (Convert.ToBoolean(Config["TOP.USERS"]))
            {
                if (Config["SERVER.ID"].ToString() == "0")
                {
                    Debug.LogWarning("Need set SERVER.ID in configurations to send info for top players");
                }
                else
                {
                    for (int i = 0; i < (int)(Stats.Count / 20) + 1; i++)
                    {
                        if (Stats.Count > 0)
                        {
                            List<Dictionary<string, object>> Temp = new List<Dictionary<string, object>>();
                            int range = Stats.Count > 20 ? 20 : Stats.Count;
                            Temp = Stats.GetRange(0, range);
                            Stats.RemoveRange(0, range);

                            string request = $"{Request}&json=true&data={JsonConvert.SerializeObject(Temp)}";

                            //Debug.LogWarning(request);
                            webrequest.EnqueueGet(request, (code, res) =>
                            {
                                switch (code)
                                {
                                    case 0:
                                        Debug.LogError("Api does not responded to a request");
                                        break;
                                    case 200:
                                        break;
                                    case 404:
                                        Debug.LogError("Response code: 404, please check your configurations");
                                        break;
                                }
                            }, this);
                        }

                    }
                    Stats.Clear();
                }
            }
        }
        #endregion

        #region[HookMethod] SendLeavesInfo
        private void SendLeavesInfo()
        {
            if (Convert.ToBoolean(Config["TOP.USERS"]))
            {
                if (Config["SERVER.ID"].ToString() == "0")
                {
                    Debug.LogWarning("Need set SERVER.ID in configurations to send info for top players");
                }
                else
                {
                    for (int i = 0; i < (int)(Leaves.Count / 20) + 1; i++)
                    {
                        if (Leaves.Count > 0)
                        {
                            List<Dictionary<string, object>> Temp = new List<Dictionary<string, object>>();
                            int range = Leaves.Count > 20 ? 20 : Leaves.Count;
                            Temp = Leaves.GetRange(0, range);
                            Leaves.RemoveRange(0, range);


                            string request = $"{Request}&action=leaves&type=json&data={JsonConvert.SerializeObject(Temp)}";

                            //Debug.LogWarning(request);
                            webrequest.EnqueueGet(request, (code, res) =>
                            {
                                switch (code)
                                {
                                    case 0:
                                        Debug.LogError("Api does not responded to a request");
                                        break;
                                    case 200:
                                        break;
                                    case 404:
                                        Debug.LogError("Response code: 404, please check your configurations");
                                        break;
                                }
                            }, this);
                        }

                    }
                    Leaves.Clear();
                }
            }
        }
        #endregion

        #region Helper

        private int GetFreeSlots(PlayerSession session)
        {
            var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            int slots = 0;
            for (int d = 0; d < inv.Capacity - 1; d++)
            {
                if (inv.GetSlot(d) == null)
                    slots++;
            }
            return slots;
        }

        #endregion

        public class Debug
        {
            public static void LogWarning(object message) => UnityEngine.Debug.LogWarning(CreateLog(message));
            public static void LogError(object message) => UnityEngine.Debug.LogError(CreateLog(message));
            private static string CreateLog(object message) => $"[{DateTime.Now.TimeOfDay.ToString().Split('.')[0]}] [GameStores]: {message}";
        }
    }
}