using Oxide.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Advanced Broadcast Death", "Kaidoz | vk.com/kaidoz", "1.1.3")]
    [Description("Продвинутая система сообщений о смерти в чате.")]

    class AdvancedBroadcastDeath : HurtworldPlugin
    {
        void Loaded()
        {
            LoadConfig();
        }

        void OnServerInitialized()
        {
            GameManager.Instance.ServerConfig.ChatDeathMessagesEnabled = false;
        }

        private struct C
        {
            // Игрок
            public static bool msgpb = true;
            public static bool msgplayer = true;
            public static int mspgradius = 200;
            public static string msgp = "<size=12><color=#FF6347>[Смерть]</color> {name} убит {Killer} ({weapon} {ds}м.)</size>";
            public static List<object> listw = new List<object>
            {
                "Items/Assault Rifle Auto;М4 AUTO",
                "Items/Assault Rifle Semi;M4 SEMI",
                "Items/Gold Axe;Gold Axe",
                "Items/Flint Hatchet;Flint Hatchet",
                "Items/Titranium Pickaxe;Titranium Pickaxe",
                "Items/Mondinium Pickaxe;Mondinium Pickaxe",
                "Items/Ultranium Pickaxe;Ultranium Pickaxe",
                "Items/Stone Pickaxe;Stone Pickaxe",
                "Items/Bow of Punishment;Bow of Punishment",
                "Items/Shotgun;Shotgun",
                "Items/Basic Spear;Basic Spear",
                "Items/Wood Bow;Wood Bow",
                "Items/Hunting Bow;Hunting Bow",
                "Items/Ice Spear;Ice Spear",
                "Items/Molten Spear;Molten Spear",
                "Items/Sharp Spear;Sharp Spear",
                "Items/Bolt Action Rifle;Bolt Action Rifle"
            };

            // Существо           
            public static bool msgcreatb = true;
            public static bool msgcreat = true;
            public static int msgcreatgradius = 200;
            public static string msgcreata = "<size=12><color=#FF6347>[Смерть]</color> {name} погиб от существа {Killer}</size>";
            public static List<object> listcreatures = new List<object>
            {
                "Creatures/Tokar;Токар"
            };

            // Фактор
            public static bool msgentb = true;
            public static bool msgent = true;
            public static int msgentgradius = 200;
            public static string msgenta = "<size=12><color=#FF6347>[Смерть]</color> {name} погиб от {Killer}</size>";
            public static List<object> listent = new List<object>
            {
                "EntityStats/Sources/Starvation;Голод"
            };

            // Ловушки    
            public static bool msgmachb = true;
            public static bool msgmach = true;
            public static int msgmachgradius = 200;
            public static string msgmacha = "<size=12><color=#FF6347>[Смерть]</color> {name} погиб от {Killer}</size>";
            public static List<object> listmach = new List<object>
            {
                "Machines/Landmine;Мина"
            };
        }

        protected override void LoadDefaultConfig() => PrintWarning("Creating config file for BroadcastDeath...");

        private new void LoadConfig()
        {
            GetConfig(ref C.msgpb, "BroadcastDeath", "Player", "Enabled");
            GetConfig(ref C.msgp, "BroadcastDeath", "Player", "Message");
            GetConfig(ref C.msgplayer, "BroadcastDeath", "Player", "Message around", "Enabled");
            GetConfig(ref C.mspgradius, "BroadcastDeath", "Player", "Message around", "Radius");
            GetConfig(ref C.listw, "BroadcastDeath", "Player", "List Items");

            GetConfig(ref C.msgcreatb, "BroadcastDeath", "Creatures", "Enabled");
            GetConfig(ref C.msgcreata, "BroadcastDeath", "Creatures", "Message");
            GetConfig(ref C.msgcreat, "BroadcastDeath", "Creatures", "Message around", "Enabled");
            GetConfig(ref C.msgcreatgradius, "BroadcastDeath", "Creatures", "Message around", "Radius");
            GetConfig(ref C.listcreatures, "BroadcastDeath", "Creatures", "List Creatures");

            GetConfig(ref C.msgentb, "BroadcastDeath", "EntityStats", "Enabled");
            GetConfig(ref C.msgenta, "BroadcastDeath", "EntityStats", "Message");
            GetConfig(ref C.msgent, "BroadcastDeath", "EntityStats", "Message around", "Enabled");
            GetConfig(ref C.msgentgradius, "BroadcastDeath", "EntityStats", "Message around", "Radius");
            GetConfig(ref C.listent, "BroadcastDeath", "EntityStats", "List EntityStats");

            GetConfig(ref C.msgmachb, "BroadcastDeath", "Machines", "Enabled");
            GetConfig(ref C.msgmacha, "BroadcastDeath", "Machines", "Message");
            GetConfig(ref C.msgmach, "BroadcastDeath", "Machines", "Message around", "Enabled");
            GetConfig(ref C.msgmachgradius, "BroadcastDeath", "Machines", "Message around", "Radius");
            GetConfig(ref C.listmach, "BroadcastDeath", "Machines", "List Machines");
            SaveConfig();
        }

        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }

        private object OnPlayerDeath(PlayerSession session, EntityEffectSourceData dataSource)
        {
            int err = 0;
            try
            {
                string name = string.Empty;
                string KillerName = string.Empty;
                try
                {
                    if (string.IsNullOrEmpty(GetNameOfObject(dataSource.EntitySource)) && string.IsNullOrEmpty(dataSource.SourceDescriptionKey))
                        return null;

                    name = session.Identity.Name;
                    KillerName = GetNameOfObject(dataSource.EntitySource);
                }
                catch (Exception ex)
                {
                    Puts(ex.ToString());
                }
                Char delimiter = ';';
                Char delimiter1 = '/';
                string[] noplayer = null;
                string bnf = "";
                if (Interface.Oxide.CallHook("OnDeathMessageADV", session) != null)
                    return null;
                err++;
                if (!KillerName.EndsWith("(P)"))
                {
                    noplayer = dataSource.SourceDescriptionKey.Split(delimiter1);
                    KillerName = noplayer[0];
                }
                else
                {
                    if (getSession(KillerName.Remove(KillerName.Length - 3)) != null)
                    {
                        KillerName = KillerName.Remove(KillerName.Length - 3);

                        if (dataSource.SourceDescriptionKey != null)
                        {
                            bnf = KillerName;
                            noplayer = dataSource.SourceDescriptionKey.Split(delimiter1);
                            KillerName = noplayer[0];
                        }
                    }
                    else
                    {
                        noplayer = KillerName.Split(delimiter1);
                        KillerName = noplayer[0];
                    }
                }
                err++;
                if (getSession(KillerName) != null && C.msgpb)
                {
                    err++;
                    Vector3 sd = session.WorldPlayerEntity.transform.position;
                    var ehs = getSession(KillerName).WorldPlayerEntity.GetComponent<EquippedHandlerServer>();
                    Vector3 rd = getSession(KillerName).WorldPlayerEntity.transform.position;
                    string gr = Convert.ToString(Math.Round(Vector3.Distance(rd, sd)));
                    var item = ehs.GetEquippedItem();
                    string ibk = item.Generator.GetNameKey();
                    int b = 0;

                    foreach (string a in C.listw)
                    {
                        String[] c = a.Split(delimiter);

                        if (ibk == c[0])
                        {
                            ibk = c[1];
                            b = 1;
                        }
                    }
                    if (b == 0)
                    {
                        String[] cx = ibk.Split(delimiter1);

                        if (cx.Count() == 2)
                        {
                            C.listw.Add(ibk + ";" + cx[1]);
                            KillerName = cx[1];
                        }
                        else
                        {
                            C.listw.Add(ibk + ";" + cx[0]);
                            KillerName = cx[0];
                        }
                        SaveConfig();
                    }
                    if (C.msgplayer)
                    {
                        sendaround(session, C.mspgradius, C.msgp.Replace("{name}", name).Replace("{Killer}", KillerName).Replace("{weapon}", ibk).Replace("{ds}", gr));
                    }
                    else
                    {
                        Server.Broadcast(C.msgp.Replace("{name}", name).Replace("{Killer}", KillerName).Replace("{weapon}", ibk).Replace("{ds}", gr));
                    }
                }
                else if (KillerName == "Creatures" && C.msgcreatb)
                {
                    err++;
                    int b = 0;
                    KillerName = GetNameOfObject(dataSource.EntitySource);
                    foreach (string a in C.listcreatures)
                    {
                        String[] c = a.Split(delimiter);

                        if (KillerName == c[0])
                        {
                            KillerName = c[1];
                            b = 1;
                        }
                    }
                    if (b == 0)
                    {
                        String[] cx = KillerName.Split(delimiter1);
                        C.listcreatures.Add(KillerName + ";" + cx[1]);
                        SaveConfig();

                        KillerName = cx[1];
                    }
                    if (C.msgcreat)
                    {
                        sendaround(session, C.msgcreatgradius, C.msgcreata.Replace("{name}", name).Replace("{Killer}", KillerName));
                    }
                    else
                    {
                        Server.Broadcast(C.msgcreata.Replace("{name}", name).Replace("{Killer}", KillerName));
                    }
                    return null;
                }
                else if (KillerName == "Machines" && C.msgmachb)
                {
                    err++;
                    int b = 0;
                    KillerName = GetNameOfObject(dataSource.EntitySource);
                    foreach (string a in C.listmach)
                    {
                        String[] c = a.Split(delimiter);

                        if (KillerName == c[0])
                        {
                            KillerName = c[1];
                            b = 1;
                        }
                    }
                    if (b == 0)
                    {
                        string[] cx = KillerName.Split(delimiter1);
                        C.listmach.Add(KillerName + ";" + cx[1]);
                        SaveConfig();
                        KillerName = cx[1];
                    }
                    if (C.msgmach)
                    {
                        sendaround(session, C.msgmachgradius, C.msgmacha.Replace("{name}", name).Replace("{Killer}", KillerName));
                    }
                    else
                    {
                        Server.Broadcast(C.msgmacha.Replace("{name}", name).Replace("{Killer}", KillerName));
                    }
                    return null;
                }
                else if (KillerName == "EntityStats" && C.msgentb)
                {
                    err++;
                    int b = 0;
                    KillerName = dataSource.SourceDescriptionKey;
                    foreach (string a in C.listent)
                    {
                        String[] c = a.Split(delimiter);

                        if (KillerName == c[0])
                        {
                            KillerName = c[1];
                            b = 1;
                        }
                    }
                    if (b == 0)
                    {
                        String[] cx = KillerName.Split(delimiter1);

                        if (bnf == "")
                            C.listent.Add(KillerName + ";" + cx[2]);
                        else
                            C.listent.Add(KillerName + ";" + cx[2] + " {player}");

                        SaveConfig();
                        KillerName = cx[2];
                    }

                    if (C.msgent)
                    {
                        sendaround(session, C.msgentgradius, C.msgenta.Replace("{name}", name).Replace("{Killer}", KillerName).Replace("{player}", bnf));
                    }
                    else
                    {
                        Server.Broadcast(C.msgenta.Replace("{name}", name).Replace("{Killer}", KillerName).Replace("{player}", bnf));
                    }
                }
            }
            catch {
                Puts(err.ToString());
            }
            return null;
        }

        void sendaround(PlayerSession session, int aroundf, string msg)
        {
            Vector3 sd = session.WorldPlayerEntity.transform.position;
            var sessions = GameManager.Instance?.GetSessions()?.Values.Where(IsValidSession).ToList();
            var playersNear =
              sessions.Where(
                  s =>
                      !s.Identity.Name.ToString().Equals(session.Identity.Name.ToString()) &&
                      Vector3.Distance(s.WorldPlayerEntity.transform.position,
                          sd) <= aroundf)
                  .ToList();

            foreach (PlayerSession sesa in playersNear)
            {
                hurt.SendChatMessage(sesa, null, msg);
            }
            hurt.SendChatMessage(session, null, msg);
        }

        public bool IsValidSession(PlayerSession session)
        {
            return session?.SteamId != null && session.IsLoaded && session.Identity.Name != null && session.Identity != null &&
            session.WorldPlayerEntity?.transform?.position != null;
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