using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("CleanPlus", "Kaidoz | vk.com/kaidoz", "1.1.2")]
    [Description("Продвинутая система очистки карты от объектов.")]

    class CleanPlus : HurtworldPlugin
    {
        OwnershipStakeServer stake;

        void Loaded()
        {
            permission.RegisterPermission("cleanplus.use", this);
            permission.RegisterPermission("cleanplus.admin", this);
            LoadConfig();
            timerstart();
        }

        void timerstart()
        {
            if (C.objr)
            {
                timer.Repeat(60f, 0, () =>
                {
                    try
                    {


                        if (C.time.Count() != 0)
                        {
                            foreach (string tim in C.time)
                            {
                                if (tim == DateTime.Now.ToString("HH:mm")) ;
                                if (C.broadcastr)
                                    Server.Broadcast(C.broadcastmsg.Replace("{0}", Convert.ToString(cleanserver())));
                                else
                                    cleanserver();
                            }
                        }
                    }
                    catch { }
                });
            }

            if (C.objta)
            {
                timer.Repeat(C.objt * 60f, 0, () =>
                {
                    if (C.broadcasta)
                    {
                        int a = cleanserver();
                        Puts($"Очищено {a}");
                        Server.Broadcast(C.broadcastmsg.Replace("{0}", Convert.ToString(a)));
                    }
                    else
                    {
                        int a = cleanserver();
                        Puts($"Очищено {a}");
                    }
                });
            }
        }

        private struct C
        {
            public static int objs = 100;
            public static int objt = 120;
            public static bool objta = true;
            public static bool objr = false;
            public static bool broadcasta = true;
            public static bool broadcastr = true;
            public static string broadcastmsg = "<color=green><b>[CleanPlus]</b></color> Было очищено {0} объектов на сервере";

            public static List<object> time = new List<object>
            {
                "12:00",
                "16:00",
                "18:00",
                "21:00"
            };
        }

        private new void LoadConfig()
        {
            GetConfig(ref C.objs, "CleanPlus", "Настройка", "Очищать объектов за раз");
            GetConfig(ref C.broadcastmsg, "CleanPlus", "Настройка", "Сообщение");
            GetConfig(ref C.objt, "CleanPlus", "Проверка карты", "Первый вариант", "Каждые(в минутах)");
            GetConfig(ref C.broadcasta, "CleanPlus", "Проверка карты", "Первый вариант", "Отображать сообщение");
            GetConfig(ref C.objta, "CleanPlus", "Проверка карты", "Первый вариант", "Включено(false-выключено)");
            GetConfig(ref C.objr, "CleanPlus", "Проверка карты", "Второй вариант", "Включено(false-выключено)");
            GetConfig(ref C.broadcastr, "CleanPlus", "Проверка карты", "Второй вариант", "Отображать сообщение");
            GetConfig(ref C.time, "CleanPlus", "Проверка карты", "Второй вариант", "Расписание");
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => PrintWarning("Creating config file for CleanPlus...");

        [ChatCommand("clean")]
        void cleanplayer(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(session.SteamId.ToString(), "cleanplus.use") || !session.IsAdmin)
            {
                hurt.SendChatMessage(session, null, "У вас нет прав");
                return;
            }

            int a = 0;
            int s = 0;
            int z = 0;

            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name.Contains("Constructed") || obj.name.Contains("StructureManager") || obj.name.Contains("Dynamic") || obj.name.Contains("LadderCollider(Clone)"))
                {
                    long cell2 = ConstructionUtilities.GetOwnershipCell(obj.transform.position);
                    long cell1 = ConstructionUtilities.GetOwnershipCell(session.WorldPlayerEntity.transform.position);

                    if (cell2 == cell1)
                    {
                        z += 1;

                        if (obj.HNetworkView() != null)
                        {
                            ConstructionManager.Instance.OwnershipCells.TryGetValue(ConstructionUtilities.GetOwnershipCell(obj.transform.position), out stake);
                            if (stake == null)
                            {
                                Singleton<HNetworkManager>.Instance.NetDestroy(obj.HNetworkView());
                                s = 1;
                            }
                            else
                                a = 1;
                        }
                    }
                }
            }
            if (a == 1)
            {
                hurt.SendChatMessage(session, null, $"<color=green><b>[CleanPlus]</b></color> На территории есть тотем");
                return;
            }

            if (s == 1)
            {
                hurt.SendChatMessage(session, null, $"<color=green><b>[CleanPlus]</b></color> Очищено {z} объектов");
                return;
            }
            hurt.SendChatMessage(session, null, $"<color=green><b>[CleanPlus]</b></color> Территория пустая");
        }

        [ChatCommand("cleanworld")]
        void cleanadmin(PlayerSession session)
        {
            if (!permission.UserHasPermission(session.SteamId.ToString(), "cleanplus.admin") || !session.IsAdmin)
            {
                hurt.SendChatMessage(session, null, "У вас нет прав");
                return;
            }
            hurt.SendChatMessage(session, null, $"<color=green><b>[CleanPlus]</b></color> Очищено объектов на сервере: {cleanserver()}");
        }

        object OnServerCommand(string cmd, string[] args)
        {
            if (cmd == "cleanworld")
            {
                int a = cleanserver();
                Puts($" Очищено объектов: {a}");
                Server.Broadcast(C.broadcastmsg.Replace("{0}", Convert.ToString(a)));
                return true;
            }

            return null;
        }
        PlayerSession pol;

        int cleanserver()
        {
            int a = 0;
            int s = 0;
            int z = 0;

            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name.Contains("Constructed") || obj.name.Contains("StructureManager") || obj.name.Contains("Dynamic") || obj.name.Contains("LadderCollider(Clone)"))
                //if ((obj.name.Contains("Constructed") || obj.name.Contains("StructureManager") || obj.name.Contains("Dynamic") || obj.name.Contains("LadderCollider(Clone)")) && !obj.name.Contains("ShackDynamicConstructed(Clone)"))
                {
                    long cell2 = ConstructionUtilities.GetOwnershipCell(obj.transform.position);

                    ConstructionManager.Instance.OwnershipCells.TryGetValue(ConstructionUtilities.GetOwnershipCell(obj.transform.position), out stake);
                    if (stake == null)
                    {
                        //z += 1;
                        if (noclean(cell2) == 0)
                        {

                            if (obj.HNetworkView()!=null)
                            {
                                Singleton<HNetworkManager>.Instance.NetDestroy(obj.HNetworkView());
                                z += 1;
                                s = 1;
                                if (C.objs == z)
                                {
                                    Puts(GameManager.Instance.GetSession(obj.HNetworkView().owner).Identity.Name);
                                    //Puts($" Очищено {z} объектов");
                                    return z;
                                }
                            }
                        }
                    }
                }
            }

            //if (s == 1)
                //Puts($" Очищено {z} объектов");

            return z;
        }

        private HashSet<string> ArroundCell(int cell)
        {
            string d = Convert.ToString(cell);
            var gridMax2 = ConstructionUtilities.OWNERSHIP_GRID_MAX * 2;
            return new HashSet<string>
                {
                    $"{cell}",
                    $"{cell + 1}",
                    $"{cell - 1}",
                    $"{cell + gridMax2}",
                    $"{cell + gridMax2 + 1}",
                    $"{cell + gridMax2 - 1}",
                    $"{cell - gridMax2}",
                    $"{cell - gridMax2 + 1}",
                    $"{cell - gridMax2 - 1}"
                };
        }

        ////////////////
        //// НУЖНОЕ ////
        ////////////////

        bool IsValidSession(PlayerSession session)
        {
            return session != null && session?.SteamId != null && session.IsLoaded && session.Identity.Name != null && session.Identity != null &&
                   session.WorldPlayerEntity?.transform?.position != null;
        }

        int noclean(long c)
        {
            string a = Convert.ToString(c);
            int z = 0;

            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var pl = pair.Value;

                int cell1 = ConstructionUtilities.GetOwnershipCell(pl.WorldPlayerEntity.transform.position);

                if (ArroundCell(cell1).Contains(a))
                    z++;
            }
            return z;
        }

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
    }
}