// Reference: UnityEngine.UI
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("LootChests", "Noviets", "1.0.0")]
    [Description("LootChests for ItemV2")]


    /* SUPPORT 
    *  Site: oxide-russia.ru
    *  Steam: https://steamcommunity.com/id/ka1doz/
    * */

    class LootChests : HurtworldPlugin
    {
        List<HNetworkView> ChestList = new List<HNetworkView>();
        GlobalItemManager GIM = Singleton<GlobalItemManager>.Instance;
        public Items items = new Items();

        public class Items
        {
            public Dictionary<string, int> common = new Dictionary<string, int>();
            public Dictionary<string, int> uncommon = new Dictionary<string, int>();
            public Dictionary<string, int> rare = new Dictionary<string, int>();
        }

        void Loaded()
        {
            ChestSpawns();
            permission.RegisterPermission("lootchests.admin", this);
            items = Interface.GetMod().DataFileSystem.ReadObject<Items>("LootChests");

            if (items.common.Count == 0)
            {
                items.common.Add("2e718220fde28dd4d8ec5ef1c101a9e2", 1);
                SaveItemList();
            }
            if (items.uncommon.Count == 0)
            {
                items.uncommon.Add("2e718220fde28dd4d8ec5ef1c101a9e2", 1);
                SaveItemList();
            }
            if (items.rare.Count == 0)
            {
                items.rare.Add("2e718220fde28dd4d8ec5ef1c101a9e2", 1);
                SaveItemList();
            }
        }

        void SaveItemList()
        {
            Interface.GetMod().DataFileSystem.WriteObject("LootChests", items);
        }

        protected override void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NoPermission","<color=#ffa500>LootChests</color>: You dont have Permission to do this! (LootChests.admin)"},
                {"SpawnFail","Spawn stopped. Too many invalid spawn locations. Please check your spawn configs"},
                {"Error","<color=#ffa500>LootChests</color>: Incorrect use of the command. Use the chat command: /lootchests addspawn [radius]"},
                {"NoItems","There's no items in the item list. Add new items in the datafile. Then: /reload LootChests"},
                {"Save","<color=#ffa500>LootChests</color>: Item List has been saved"},
                {"Spawned","<color=#ffa500>LootChests</color>: Loot Chests have Spawned"},
                {"Despawned","<color=#ffa500>LootChests</color>: Loot Chests have Despawned"},
                {"ChestSpawnError","<color=#ffa500>LootChests</color>: Error: Unable to spawn the chest as it did not exist. If you have WeaponsCrateMod set to true, check that it is installed."}
            };

            lang.RegisterMessages(messages, this);
        }
        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
        protected override void LoadDefaultConfig()
        {
            var Locs = new List<object>() { "-2800, 200, -1000, 20" };
            if (Config["StartPoints"] == null) Config.Set("StartPoints", Locs);
            if (Config["ChestSpawnCount"] == null) Config.Set("ChestSpawnCount", 20);
            if (Config["SecondsForSpawn"] == null) Config.Set("SecondsForSpawn", 7200);
            if (Config["SecondsTillDestroy"] == null) Config.Set("SecondsTillDestroy", 1800);
            if (Config["ItemsPerChest"] == null) Config.Set("ItemsPerChest", 1);
            if (Config["ShowSpawnMessage"] == null) Config.Set("ShowSpawnMessage", true);
            if (Config["ShowDespawnMessage"] == null) Config.Set("ShowDespawnMessage", true);
            SaveConfig();
        }
        [ChatCommand("lootchests")]
        void LootChestCommand(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(session.Identity.SteamId.ToString(), "LootChests.admin") && !session.IsAdmin)
            {
                Player.Message(session, Msg("NoPermission", session.Identity.SteamId.ToString()));
                return;
            }
            if (args.Length == 2)
            {
                if (args[0].ToLower() == "addspawn")
                {
                    float radius = Convert.ToSingle(args[1]);
                    if (radius != null)
                    {
                        var LocList = Config.Get<List<string>>("StartPoints");
                        var ploc = session.WorldPlayerEntity.transform.position;
                        LocList.Add(ploc.x + ", " + ploc.y + ", " + ploc.z + ", " + radius);
                        Puts("ADDED: {" + ploc.x + ", " + ploc.y + ", " + ploc.z + ", " + radius + "}");
                        Player.Message(session, "ADDED: {" + ploc.x + ", " + ploc.y + ", " + ploc.z + ", " + radius + "}");
                        Config.Set("StartPoints", LocList);
                        SaveConfig();
                        return;
                    }
                }
            }
            Player.Message(session, Msg("Error", session.Identity.SteamId.ToString()));
        }
        void ChestSpawns()
        {
            timer.Repeat(Convert.ToSingle(Config["SecondsForSpawn"]), 0, () =>
            {
                if ((bool)Config["ShowSpawnMessage"])
                    Server.Broadcast(Msg("Spawned"));
                var LocList = Config.Get<List<string>>("StartPoints");
                int count = Convert.ToInt32(Config["ChestSpawnCount"]);
                int iPC = Convert.ToInt32(Config["ItemsPerChest"]);
                foreach (string Loc in LocList)
                {
                    int i = 0;
                    int fail = 0;
                    string[] XYZ = Loc.ToString().Split(',');
                    Vector3 position = new Vector3(Convert.ToSingle(XYZ[0]), Convert.ToSingle(XYZ[1]), Convert.ToSingle(XYZ[2]));
                    float radius = Convert.ToSingle(XYZ[3]);
                    while (i < count)
                    {
                        if (fail > count * 3) { Puts(Msg("SpawnFail")); return; }
                        Vector3 randposition = new Vector3((position.x + UnityEngine.Random.Range(-radius, radius)), (position.y + 450f), (position.z + UnityEngine.Random.Range(-radius, radius)));
                        RaycastHit hitInfo;
                        if (Physics.Raycast(randposition, Vector3.down, out hitInfo))
                        {
                            Quaternion rotation = Quaternion.Euler(0.0f, (float)UnityEngine.Random.Range(0f, 360f), 0.0f);
                            rotation = Quaternion.FromToRotation(Vector3.down, hitInfo.normal) * rotation;
                            if (!hitInfo.collider.gameObject.name.Contains("UV") && !hitInfo.collider.gameObject.name.Contains("Cliff") && !hitInfo.collider.gameObject.name.Contains("Zone") && !hitInfo.collider.gameObject.name.Contains("Cube") && !hitInfo.collider.gameObject.name.Contains("Build") && !hitInfo.collider.gameObject.name.Contains("Road") && !hitInfo.collider.gameObject.name.Contains("MeshColliderGroup"))
                            {
                                GameObject Obj = Singleton<HNetworkManager>.Instance.NetInstantiate(uLink.NetworkPlayer.server, Singleton<RuntimeAssetManager>.Instance.RefList.LootCachePrefab, hitInfo.point, Quaternion.identity, GameManager.GetSceneTime());
                                if (Obj != null)
                                {
                                    Inventory inv = Obj.GetComponent<Inventory>() as Inventory;
                                    if (inv.Capacity < iPC)
                                        inv.SetCapacity(iPC);
                                    var nmv = Obj.HNetworkView();
                                    ChestList.Add(nmv);
                                    GiveItems(inv);
                                    Destroy(nmv);
                                    i++;
                                }
                                else
                                {
                                    Server.Broadcast(Msg("ChestSpawnError"));
                                    return;
                                }
                                i++;
                            }
                            else
                                fail++;
                        }
                        else
                            fail++;
                    }
                }
                if ((bool)Config["ShowDespawnMessage"])
                {
                    timer.Once(Convert.ToSingle(Config["SecondsTillDestroy"]), () =>
                    {
                        Server.Broadcast(Msg("Despawned"));
                    });
                }
            });
        }

        void GiveItems(Inventory inv)
        {
            int num = 0;
            int count = Convert.ToInt32(Config["ItemsPerChest"]);
            while (num < count)
            {
                var ItemList = getRarity();
                int rand = UnityEngine.Random.Range(0, ItemList.Count);
                ItemGeneratorAsset generator = RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>((string)ItemList.ElementAt(rand).Key);
                GIM.GiveItem(generator, ItemList.ElementAt(rand).Value, inv);
                num++;
            }
        }

        Dictionary<string, int> getRarity()
        {
            int x = UnityEngine.Random.Range(0, 100);
            if (x >= 1 && x <= 3)
            {
                // Puts("RARE");
                return items.rare;
            }
            if (x >= 5 && x <= 40)
            {
                // Puts("UNCOMMON");
                return items.uncommon;
            }
            // Puts("COMMON");
            return items.common;
        }

        void Unload()
        {
            Puts("Cleaning up spawned objects...");
            foreach (var nwv in ChestList)
            {
                Singleton<HNetworkManager>.Instance.NetDestroy(HNetworkExtensions.HNetworkView(nwv));
            }
            Puts("Done");
        }

        void Destroy(HNetworkView nwv)
        {
            timer.Once(Convert.ToSingle(Config["SecondsTillDestroy"]), () =>
            {
                if (nwv != null)
                {
                    ChestList.Remove(nwv);
                    Singleton<HNetworkManager>.Instance.NetDestroy(HNetworkExtensions.HNetworkView(nwv));
                }
            });
        }
    }
}