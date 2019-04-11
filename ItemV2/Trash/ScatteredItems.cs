using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;

/**
 * Commands:
 * /fps
 * /antilags
 * /bugplace
 */
namespace Oxide.Plugins
{
    [Info("ScatteredItems", "devs", "1.2.0")]
    [Description("Provides automatic repair server lags")]

    class ScatteredItems : CovalencePlugin
    {
        #region [Variables]

        string squareID, playerSquareID;
        int works = 0;
        bool busy, dumpBusy = false;
        bool debug = true;
        int destroyOver;
        int fpsCritical;
        int updateIntervalSeconds;
        int radiusXZ;
        int radiusY;

        const string permRun = "run";
        const string permDiagnostic = "diagnostic";

        Dictionary<string, List<GameObject>> squareObjList;
        Dictionary<string, int> squareCleaned;
        Dictionary<int, Dictionary<string, int>> dumpPositions;

        #endregion


        #region [Hooks]

        void Init()
        {
            LoadDefaultConfig();
            //LoadDefaultMessages();
            permission.RegisterPermission($"{PermissionPrefix}.{permRun}", this);
            permission.RegisterPermission($"{PermissionPrefix}.{permDiagnostic}", this);
        }

        protected override void LoadDefaultConfig()
        {
            Config["Destroy Over"] = destroyOver = GetConfig("Destroy Over", 100);
            Config["Critical FPS"] = fpsCritical = GetConfig("Critical FPS", 33);
            Config["Check Interval"] = updateIntervalSeconds = GetConfig("Check Interval", 10);
            Config["Horizontal Space"] = radiusXZ = GetConfig("Horizontal Space", 3);
            Config["Vertical Space"] = radiusY = GetConfig("Vertical Space", 1);

            SaveConfig();
        }

        void Loaded()
        {
            timer.Repeat(Convert.ToSingle(updateIntervalSeconds), 0, () => { DoClean(); });
        }

        #endregion


        #region [Command]

        [Command("fps")]
        void FpsCommand(IPlayer player)
        {
            if (!player.IsAdmin && !HasPerm(player.Id, "diagnostic"))
            {
                return;
            }
            SendChatMessage(player, "FPS: " + Mathf.RoundToInt(1f / Time.smoothDeltaTime));
        }

        [Command("antilags")]
        void AntiLagsCommand(IPlayer player)
        {
            if (!player.IsAdmin && !HasPerm(player.Id, "run"))
            {
                SendChatMessage(player, "Not have permission");
                return;
            }

            Puts("Command 'antilags' used by: " + player.Name);

            FpsCommand(player);

            DoClean(player);
        }

        [Command("bugplace")]
        void DumpCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !HasPerm(player.Id, "diagnostic"))
            {
                SendChatMessage(player, "Not have permission");
                return;
            }

            switch (args.Length)
            {
                case 0:

                    timer.Once(0, () =>
                    {
                        if (dumpBusy)
                        {
                            SendChatMessage(player, "Busy");
                        }
                        dumpBusy = true;
                        Puts("Start 'bugplace' by " + player.Name);

                        if (debug)
                            playerSquareID = GetSquareID(player.Position());
                        Dictionary<string, int> dumpSquareCount = new Dictionary<string, int>();

                        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
                        {
                            if (!CanBeDangerous(obj))
                            {
                                continue;
                            }

                            squareID = GetSquareID(obj.transform.position);
                            if (debug && squareID == playerSquareID)
                            {
                                Puts(obj.name.ToString());
                                SendChatMessage(player, obj.name.ToString());
                            }

                            if (!dumpSquareCount.ContainsKey(squareID))
                            {
                                dumpSquareCount.Add(squareID, 0);
                            }
                            dumpSquareCount[squareID]++;
                        }
                        int i = 1;
                        string[] posArray;
                        string pos;
                        dumpPositions = new Dictionary<int, Dictionary<string, int>>();
                        foreach (KeyValuePair<string, int> entry in dumpSquareCount.OrderByDescending(pair => pair.Value).Take(30))
                        {
                            posArray = entry.Key.Split(',');
                            pos = posArray[0] + "5, " + posArray[1] + "9, " + posArray[2] + "5";
                            SendChatMessage(player, i + ". Pos: " + pos + "   (" + entry.Value + ")");
                            Puts("[pos] " + pos + " (" + entry.Value + ")");
                            dumpPositions.Add(i, new Dictionary<string, int> { { "x", Convert.ToInt16(posArray[0]) * 10 + 5 }, { "y", Convert.ToInt16(posArray[1]) * 10 + 9 }, { "z", Convert.ToInt16(posArray[2]) * 10 + 5 }, { "count", entry.Value } });
                            i++;
                        }
                        SendChatMessage(player, "Scan completed. To move use the command: /bugplace <id>");
                        dumpSquareCount = null;
                        dumpBusy = false;
                    });

                    break;

                case 1:

                    if (dumpPositions.Count > 0)
                    {
                        int x, y, z, count;
                        //dumpPositions.FindIndex
                        int key = Convert.ToInt16(args[0]);
                        if (dumpPositions.ContainsKey(key))
                        {
                            dumpPositions[key].TryGetValue("x", out x);
                            dumpPositions[key].TryGetValue("y", out y);
                            dumpPositions[key].TryGetValue("z", out z);
                            dumpPositions[key].TryGetValue("count", out count);
                            player.Teleport(x, (y + 10), z);
                            SendChatMessage(player, "TP (" + key + "): " + x + ", " + (y + 10) + ", " + z + " (" + count + ")");
                        }
                    }

                    break;
            }
        }

        #endregion

        #region [Helpers]

        void DoClean(IPlayer player = null)
        {
            if (player == null)
            {
                if (busy == true)
                {
                    Puts("Busy");
                    return;
                }
                if (works <= 0)
                {
                    if (fpsCritical < Mathf.RoundToInt(1f / Time.smoothDeltaTime))
                    {
                        return;
                    }
                }
            }

            timer.Once(0, () =>
            {
                busy = true;
                Puts("Start (FPS:" + Mathf.RoundToInt(1f / Time.smoothDeltaTime).ToString() + ")");

                int destroyed = 0;
                int destroyedAll = 0;
                int squareCleanedCount = 0;
                int subSquareCleanedCount = 0;
                int x, y, z;
                string squareID;
                Dictionary<string, Dictionary<string, int>> cordList = new Dictionary<string, Dictionary<string, int>>();

                squareObjList = new Dictionary<string, List<GameObject>>();

                foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (CanBeDangerous(obj))
                    {
                        x = (int)Math.Round(obj.transform.position.x / 10);
                        y = (int)Math.Round(obj.transform.position.y / 10);
                        z = (int)Math.Round(obj.transform.position.z / 10);
                        squareID = GetSquareID(obj.transform.position);

                        if (!cordList.ContainsKey(squareID))
                        {
                            cordList.Add(squareID, new Dictionary<string, int> { { "x", x }, { "y", y }, { "z", z } });
                        }

                        if (!squareObjList.ContainsKey(squareID))
                        {
                            squareObjList.Add(squareID, new List<GameObject>());
                        }
                        squareObjList[squareID].Add(obj);
                    }
                }

                squareCleaned = new Dictionary<string, int>();

                //Puts("Clean Center Points");
                Dictionary<string, Dictionary<string, int>> dangerousPointList = new Dictionary<string, Dictionary<string, int>>();

                foreach (KeyValuePair<string, List<GameObject>> entry in squareObjList)
                {
                    if (entry.Value.Count() > destroyOver)
                    {
                        destroyed = CleanSquere(entry.Key);

                        if (destroyed > 0)
                        {
                            squareCleanedCount++;

                            if (!dangerousPointList.ContainsKey(entry.Key) && cordList.ContainsKey(entry.Key))
                            {
                                dangerousPointList.Add(entry.Key, cordList[entry.Key]);
                            }
                        }
                        destroyedAll = destroyedAll + destroyed;
                    }
                }
                works--;

                if (squareCleanedCount > 0)
                {
                    if (dangerousPointList.Count > 0)
                    {
                        //Puts("Clean Near Center Points");
                        foreach (KeyValuePair<string, Dictionary<string, int>> dangerusPoint in dangerousPointList)
                        {
                            dangerusPoint.Value.TryGetValue("x", out x);
                            dangerusPoint.Value.TryGetValue("y", out y);
                            dangerusPoint.Value.TryGetValue("z", out z);
                            int xStart = x - radiusXZ;
                            int xEnd = x + radiusXZ;
                            int yStart = y - radiusY;
                            int yEnd = y + radiusY;
                            int zStart = z - radiusXZ;
                            int zEnd = z + radiusXZ;
                            int xTemp, yTemp, zTemp;
                            for (xTemp = xStart; xTemp <= xEnd; xTemp++)
                            {
                                for (yTemp = yStart; yTemp <= yEnd; yTemp++)
                                {
                                    for (zTemp = zStart; zTemp <= zEnd; zTemp++)
                                    {
                                        destroyed = CleanSquere(GetSquareID(xTemp, yTemp, zTemp));

                                        if (destroyed > 0)
                                        {
                                            subSquareCleanedCount++;
                                        }
                                        destroyedAll = destroyedAll + destroyed;
                                    }
                                }
                            }
                        }
                    }
                    //Puts("Enabled safe mode");
                    works = 6;
                    //updateIntervalSeconds = 10;
                    //timer.Interval = Convert.ToInt32(updateIntervalSeconds);
                }
                else
                {
                    if (works <= 0)
                    {
                        works = 0;
                        //updateIntervalSeconds = 20;
                    }
                }
                busy = false;

                Puts("End");

                if (player != null)
                {
                    SendChatMessage(player, "Cleaned " + (squareCleanedCount + subSquareCleanedCount) + "(" + squareCleanedCount + ")/" + squareCleaned.Count + " in " + destroyedAll + " points");
                }
                squareObjList = null;
                squareCleaned = null;
            });
        }

        string GetSquareID(int x, int y, int z)
        {
            return x.ToString() + "," + y.ToString() + "," + z.ToString();
        }

        string GetSquareID(Vector3 position)
        {
            return Math.Round(position.x / 10).ToString() + "," + Math.Round(position.y / 10).ToString() + "," + Math.Round(position.z / 10).ToString();
        }

        string GetSquareID(GenericPosition position)
        {
            return Math.Round(position.X / 10).ToString() + "," + Math.Round(position.Y / 10).ToString() + "," + Math.Round(position.Z / 10).ToString();
        }

        bool CanBeDangerous(GameObject obj)
        {
            if (obj.name.Contains("WorldItem(Clone)") || obj.name.Contains("WorldItemOre(Clone)"))
            {
                return true;
            }
            return false;
        }

        int CleanSquere(string squerePosition)
        {
            if (squareCleaned.ContainsKey(squerePosition) || !squareObjList.ContainsKey(squerePosition))
            {
                return 0;
            }
            int destroyed = 0;
            List<GameObject> listObj = new List<GameObject>();
            squareObjList.TryGetValue(squerePosition, out listObj);
            foreach (GameObject obj in listObj)
            {
                if (!obj.IsNullOrDestroyed())
                {
                        Singleton<HNetworkManager>.Instance.NetDestroy(obj.HNetworkView());
                        destroyed++;
                }
            }

            string[] posArray = squerePosition.Split(',');
            string pos = posArray[0] + "5, " + posArray[1] + "9, " + posArray[2] + "5";
            Puts("Square cleaned: '" + pos + "' destroyed " + destroyed + " obj.");
            squareCleaned.Add(squerePosition, 1);
            return destroyed;
        }

        //bool HasPerm(object uid, params string[] permArray)
        bool HasPerm(object uid, string perm)
        {
            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        void SendChatMessage(IPlayer player, string msg) => player.Reply("<color=#C4FF00>" + msg + "</color>");

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
