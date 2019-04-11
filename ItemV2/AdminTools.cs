using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Admin Tools", "Kaidoz", "1.4.3")]
    [Description("Kick, ban, temp ban, mute, freeze and godmode with permissions")]
    public class AdminTools : HurtworldPlugin
    {
        /* Credits
            Noviets, the original author of this plugin */
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"nopermission","You dont have Permission to do this!"},
                {"playernotfound","That player does not exist, or is not online."},
                {"noreason","You must provide a reason to kick."},
                {"banfail","Incorrect Usage: /Ban (Player|IP|SteamID)"},
                {"tempbanfail","Incorrect Usage: /TempBan (Player|IP|SteamID) (Duration in minutes)"},
                {"godfail","Incorrect Usage: /Godmode (on|off)"},
                {"unbanfail","Invalid SteamID. Please try again: /unban SteamID"},
                {"kicked","<color=#ff0000>{Player}</color> has been <color=#ff0000>KICKED</color> for: <color=#ffa500>{Reason}</color>"},
                {"banned","<color=#ff0000>{Player}</color> has been <color=#ff0000>BANNED</color>."},
                {"tempbanned","<color=#ff0000>{Player}</color> has been <color=#ff0000>BANNED</color> for <color=orange>{Duration}</color> minutes"},
                {"tempcheckfail","Invalid Syntax - /CheckTempBan (IP|SteamID|PlayerName)"},
                {"istempbanned","{Name} is TempBanned for {Duration}"},
                {"isnttempbanned","{Name} is not TempBanned"},
                {"isnolongertempbanned","{Name} is no longer TempBanned"},
                {"tempremovefail","Invalid Syntax - /RemoveTempBan (IP|SteamID|PlayerName)"},
                {"unbanned","You have Unbanned SteamID: {ID}"},
                {"banmsg","You have been Banned."},
                {"healfail","Incorrect Usage. /Heal -or- /Heal (Player)"},
                {"tempbanmsg","You have been TempBanned for {Duration} minutes."},
                {"godkill","Killing in Godmode."},
                {"notgod","You are not in Godmode."},
                {"alreadygod","You are already in Godmode."},
                {"muted","<color=#ff0000>{Player}</color> has been <color=#ff0000>Muted</color> for {time} seconds.{reason}"},
                {"mutewarning","<color=#ff0000>Please be aware; your mute duration will increase on each attempt to speak</color>"},
                {"unmuted","<color=#ff0000>{Player}</color> is no longer <color=#ff0000>Muted</color>."},
                {"mutefail","Incorrect Usage: /mute (Player|IP|SteamID) (Duration)"},
                {"frozenmsg","You have Frozen {Player}."},
                {"unfrozenmsg","{Player} is no longer Frozen."},
                {"frozen","<color=#ff0000>You have been Frozen.</color>"},
                {"unfrozen","<color=#ff0000>You are no longer Frozen.</color>"},
                {"notvalidnumber","{arg} is not a valid number. "},
                {"infamyerror","Incorrect Usage! Use: /infamy (Player|all) (amount)"},
                {"infamyset"," {player}'s infamy set to {infamy}"},
            }, this);
        }

        public class TBA
        {
            public string ID;
            public string Name;
            public string IP;
            public DateTime TillExpire;

            public TBA(string iID, string iName, string iIP, DateTime TE)
            {
                ID = iID;
                Name = iName;
                IP = iIP;
                TillExpire = TE;
            }
        }

        protected override void LoadDefaultConfig()
        {
            if (Config["ShowConsoleMsg"] == null)
            {
                Config.Set("ShowConsoleMsg", false);
            }

            if (Config["KillingInGodBan"] == null)
            {
                Config.Set("KillingInGodBan", false);
            }

            if (Config["KillingInGodKick"] == null)
            {
                Config.Set("KillingInGodKick", false);
            }

            if (Config["LogKicks"] == null)
            {
                Config.Set("LogKicks", false);
            }

            if (Config["LogBans"] == null)
            {
                Config.Set("LogBans", false);
            }

            if (Config["LogTempBans"] == null)
            {
                Config.Set("LogTempBans", false);
            }

            if (Config["LogUnban"] == null)
            {
                Config.Set("LogUnban", false);
            }

            if (Config["LogGodMode"] == null)
            {
                Config.Set("LogGodMode", false);
            }

            if (Config["LogMute"] == null)
            {
                Config.Set("LogMute", false);
            }

            if (Config["LogNoClip"] == null)
            {
                Config.Set("LogNoClip", false);
            }

            if (Config["LogHeal"] == null)
            {
                Config.Set("LogHeal", false);
            }

            if (Config["LogInfamy"] == null)
            {
                Config.Set("LogInfamy", false);
            }

            if (Config["LogFreeze"] == null)
            {
                Config.Set("LogFreeze", false);
            }

            if (Config["LogKillingInGodMode"] == null)
            {
                Config.Set("LogKillingInGodMode", false);
            }

            SaveConfig();
        }

        private string GetNameOfObject(UnityEngine.GameObject obj)
        {
            GameManager ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }

        public static List<ulong> Godlist = new List<ulong>();
        private List<TBA> TempBans = new List<TBA>();
        private DateTime TimeLeft;
        private string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        private void Loaded()
        {
            try
            {
                TempBans = Interface.Oxide.DataFileSystem.ReadObject<List<TBA>>("AdminTools/TempBans");
            }
            catch
            {
                SaveTempBans();
            }
            try
            {
                globallog = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("AdminTools/GlobalLog");
            }
            catch { }

            permission.RegisterPermission("admintools.kick", this);
            permission.RegisterPermission("admintools.ban", this);
            permission.RegisterPermission("admintools.tempban", this);
            permission.RegisterPermission("admintools.godmode", this);
            permission.RegisterPermission("admintools.mute", this);
            permission.RegisterPermission("admintools.freeze", this);
            permission.RegisterPermission("admintools.infamy", this);
            permission.RegisterPermission("admintools.noclip", this);
            permission.RegisterPermission("admintools.all", this);

            LoadDefaultConfig();
        }

        private void OnServerInitialized()
        {
            HealAll();
        }

        private List<string> globallog = new List<string>();
        private void SaveGlobalLog() => Interface.Oxide.DataFileSystem.WriteObject("AdminTools/GlobalLog", globallog);
        private void SaveGodlist() => Interface.Oxide.DataFileSystem.WriteObject("AdminTools/Godlist", Godlist);
        private void SaveTempBans() => Interface.Oxide.DataFileSystem.WriteObject("AdminTools/TempBans", TempBans);

        private void OnPlayerDisconnected(PlayerSession session)
        {
            if (Godlist.Contains((ulong)session.SteamId))
            {
                Godlist.Remove((ulong)session.SteamId);
                SaveGodlist();
            }
        }

        private PlayerSession GetSession(String source)
        {
            Match IPCheck = Regex.Match(source, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");

            foreach (PlayerSession session in GameManager.Instance.GetSessions().Values)
            {
                if (session != null && session.IsLoaded)
                {
                    if (source.ToLower() == session.Identity.Name.ToLower())
                    {
                        return session;
                    }
                }
            }

            foreach (PlayerSession session in GameManager.Instance.GetSessions().Values)
            {
                if (session != null && session.IsLoaded)
                {
                    if (IPCheck.Success)
                    {
                        if (source == session.Player.ipAddress)
                        {
                            return session;
                        }
                    }
                    else if (source == session.SteamId.ToString())
                    {
                        return session;
                    }
                    else if (session.Identity.Name.ToLower().Contains(source.ToLower()))
                    {
                        return session;
                    }
                }
            }
            return null;
        }

        private void OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.godmode") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                string KillName = GetNameOfObject(source.EntitySource);
                if (KillName != "")
                {
                    string Killer = KillName.Replace("(P)", "");
                    if ((bool)Config["KillingInGodBan"] || (bool)Config["KillingInGodKick"])
                    {
                        PlayerSession person = GetSession(Killer);
                        if (person != null)
                        {
                            ulong ID = (ulong)person.SteamId;
                            if (Godlist.Contains(ID))
                            {
                                if ((bool)Config["LogKillingInGodMode"])
                                {
                                    globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") killed " + person.Identity.Name + " (" + person.SteamId + ") while in GodMode");
                                    SaveGlobalLog();
                                }
                                if ((bool)Config["KillingInGodKick"])
                                {
                                    GameManager.Instance.KickPlayer(ID.ToString(), Msg("godkill"));
                                }

                                if (!person.IsAdmin)
                                {
                                    if ((bool)Config["KillingInGodBan"])
                                    {
                                        ConsoleManager.Instance.ExecuteCommand("ban " + ID);
                                        GameManager.Instance.KickPlayer(ID.ToString(), Msg("godkill"));
                                    }
                                }
                            }
                        }
                    }
                }

                if (Godlist.Contains((ulong)session.SteamId))
                {
                    EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
                    timer.Once(0.1f, () =>
                    {
                        stats.GetFluidEffect(Singleton<RuntimeAssetManager>.Instance.RefList.EntityEffectDatabase.Health).SetValue(100f);
                    });
                }
            }
        }

        private void OnPlayerTakeDamage(PlayerSession session, EntityEffectSourceData source)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.godmode") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (Godlist.Contains((ulong)session.SteamId))
                {
                    source.LastEffectSource = null;
                }
            }
        }

        private void CanClientLogin(PlayerSession session)
        {
            TBA Joiner = CheckBanJoin(session.Player.ipAddress, session.Identity.Name, session.SteamId.ToString()) as TBA;

            if (Joiner != null)
            {
                DateTime CurrentTime = DateTime.UtcNow;
                if (Joiner.TillExpire < CurrentTime)
                {
                    TempBans.Remove(Joiner);
                    return;
                }

                string[] TidyTime = Joiner.TillExpire.Subtract(CurrentTime).ToString().Split('.');
                GameManager.Instance.KickPlayer(session.SteamId.ToString(), Msg("tempbanmsg", session.SteamId.ToString().Replace("{Duration} minutes.", TidyTime[0])));
            }
        }

        [ChatCommand("kick")]
        private void KickCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.kick") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length < 2)
                {
                    hurt.SendChatMessage(session, null, Msg("noreason", session.SteamId.ToString()));
                    return;
                }

                PlayerSession person = GetSession(args[0]);

                if (person == null)
                {
                    hurt.SendChatMessage(session, null, Msg("playernotfound", session.SteamId.ToString()));
                }
                else
                {
                    string ID = person.SteamId.ToString();
                    string reason = string.Join(" ", args, 1, args.Length - 1);

                    hurt.BroadcastChat(null, Msg("kicked", session.SteamId.ToString()).Replace("{Player}", person.Identity.Name).Replace("{Reason}", reason));
                    person.IPlayer.Kick(reason);
                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " kicked " + person.Identity.Name);
                    }

                    if ((bool)Config["LogKicks"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") KICKED " + person.Identity.Name + " (" + person.SteamId + ") for " + reason);
                        SaveGlobalLog();
                    }
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission"));
            }
        }

        [ChatCommand("ban")]
        private void BanCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.ban") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length != 1)
                {
                    hurt.SendChatMessage(session, null, Msg("banfail", session.SteamId.ToString()));
                    return;
                }
                if (args[0].Length == 17)
                {
                    string ID = args[0];
                    hurt.BroadcastChat(null, Msg("banned", session.SteamId.ToString()).Replace("{Player}", "SteamID: " + args[0]));
                    ConsoleManager.Instance.ExecuteCommand("ban " + ID);
                    GameManager.Instance.KickPlayer(ID, Msg("banmsg", session.SteamId.ToString()));
                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " banned SteamID: " + ID);
                    }

                    if ((bool)Config["LogBans"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") BANNED (" + ID + ")");
                        SaveGlobalLog();
                    }
                    return;
                }
                PlayerSession person = GetSession(args[0]);
                if (person == null)
                {
                    hurt.SendChatMessage(session, null, Msg("playernotfound", session.SteamId.ToString()));
                }
                else
                {
                    string ID = person.SteamId.ToString();
                    hurt.BroadcastChat(null, Msg("banned", session.SteamId.ToString()).Replace("{Player}", person.Identity.Name));
                    ConsoleManager.Instance.ExecuteCommand("ban " + ID);
                    //GameManager.Instance.KickPlayer(ID, Msg("banmsg", session.SteamId.ToString()));
                    person.IPlayer.Kick(Msg("banmsg", session.SteamId.ToString()));

                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " banned " + person.Identity.Name);
                    }

                    if ((bool)Config["LogBans"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") BANNED " + person.Identity.Name + " (" + person.SteamId + ")");
                        SaveGlobalLog();
                    }
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("tempban")]
        private void TempBanCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.tempban") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length != 2)
                {
                    hurt.SendChatMessage(session, null, Msg("tempbanfail", session.SteamId.ToString()));
                    return;
                }

                TBA PlayerBanned = CheckBan(args[0]) as TBA;

                if (PlayerBanned != null)
                {
                    TempBans.Remove(PlayerBanned);
                }

                DateTime CurrentTime = DateTime.UtcNow;
                PlayerSession person = GetSession(args[0]);

                if (person == null)
                {
                    hurt.SendChatMessage(session, null, Msg("playernotfound", session.SteamId.ToString()));
                    return;
                }

                double Duration;

                try
                {
                    Duration = Convert.ToDouble(args[1]);
                }
                catch
                {
                    hurt.SendChatMessage(session, null, Msg("tempbanfail", session.SteamId.ToString()));
                    return;
                }

                hurt.BroadcastChat(null, Msg("tempbanned", session.SteamId.ToString()).Replace("{Player}", person.Identity.Name).Replace("{Duration}", Duration.ToString()));
                TempBans.Add(new TBA(person.SteamId.ToString(), person.Identity.Name, person.Player.ipAddress, CurrentTime.AddMinutes(Duration)));
                SaveTempBans();
                person.IPlayer.Kick(Msg("tempbanmsg", session.SteamId.ToString()).Replace("{Duration}", Duration.ToString()));

                if ((bool)Config["ShowConsoleMsg"])
                {
                    Puts(session.Identity.Name + " TempBanned " + person.Identity.Name + " for " + Duration);
                }

                if ((bool)Config["LogTempBans"])
                {
                    globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") TempBANNED " + person.Identity.Name + " (" + person.SteamId + ") for " + Duration);
                    SaveGlobalLog();
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("removetempban")]
        private void RemoveTempBanCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.tempban") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length == 1)
                {
                    TBA PlayerBanned = CheckBan(args[0]) as TBA;

                    if (PlayerBanned != null)
                    {
                        hurt.SendChatMessage(session, null, Msg("isnolongertempbanned", session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
                        TempBans.Remove(PlayerBanned);

                        if ((bool)Config["ShowConsoleMsg"])
                        {
                            Puts(session.Identity.Name + " removed the TempBan on " + PlayerBanned.Name);
                        }
                    }
                    else
                    {
                        hurt.SendChatMessage(session, null, Msg("isnttempbanned", session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
                    }
                }
                else
                {
                    hurt.SendChatMessage(session, null, Msg("tempremovefail", session.SteamId.ToString()));
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("checktempban")]
        private void CheckTempBanCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.tempban") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length == 1)
                {
                    DateTime CurrentTime = DateTime.UtcNow;
                    TBA PlayerBanned = CheckBan(args[0]) as TBA;

                    if (PlayerBanned != null)
                    {
                        string[] TidyTime = PlayerBanned.TillExpire.Subtract(CurrentTime).ToString().Split('.');

                        if (PlayerBanned.TillExpire < CurrentTime)
                        {
                            hurt.SendChatMessage(session, null, Msg("isnolongertempbanned", session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
                            TempBans.Remove(PlayerBanned);
                            SaveTempBans();
                        }
                        else
                        {
                            hurt.SendChatMessage(session, null, Msg("istempbanned", session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name).Replace("{Duration}", TidyTime[0]));
                        }
                    }
                    else
                    {
                        hurt.SendChatMessage(session, null, Msg("isnttempbanned", session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
                    }
                }
                else
                {
                    hurt.SendChatMessage(session, null, Msg("tempcheckfail", session.SteamId.ToString()));
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("unban")]
        private void UnbanCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.ban") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length != 1)
                {
                    hurt.SendChatMessage(session, null, Msg("unbanfail", session.SteamId.ToString()));
                    return;
                }

                if (args[0].Length == 17)
                {
                    string ID = args[0];
                    ConsoleManager.Instance.ExecuteCommand("unban " + ID);
                    hurt.SendChatMessage(session, null, Msg("unbanned", session.SteamId.ToString()).Replace("{ID}", ID));

                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " unbanned: " + ID);
                    }

                    if ((bool)Config["LogUnBan"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") UNBANNED " + ID);
                        SaveGlobalLog();
                    }
                }
                else
                {
                    hurt.SendChatMessage(session, null, Msg("unbanfail", session.SteamId.ToString()));
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("mutez")]
        private void Command(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.mute") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                string reason = "";

                if (args.Length < 2)
                {
                    hurt.SendChatMessage(session, null, Msg("mutefail", session.SteamId.ToString()));
                    return;
                }

                PlayerSession person = GetSession(args[0]);
                float Duration = float.Parse(args[1]);

                if (args.Length >= 3)
                {
                    reason = " Reason: " + string.Join(" ", args, 2, args.Length - 2);
                }

                if (person == null)
                {
                    hurt.SendChatMessage(session, null, Msg("playernotfound", session.SteamId.ToString()));
                }
                else
                {
                    hurt.BroadcastChat(null, Msg("muted", person.SteamId.ToString()).Replace("{Player}", person.Identity.Name).Replace("{time}", args[1]).Replace("{reason}", reason));
                    hurt.SendChatMessage(person, null, Msg("mutewarning", session.SteamId.ToString()));
                    ChatManagerServer.Instance.Mute((ulong)person.SteamId, Duration);

                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " muted " + person.Identity.Name);
                    }

                    if ((bool)Config["LogMute"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") MUTED " + person.Identity.Name + " (" + person.SteamId + ") for " + args[1] + " seconds" + reason);
                        SaveGlobalLog();
                    }
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("unmutez")]
        private void UnmuteCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.mute") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length != 1)
                {
                    hurt.SendChatMessage(session, null, Msg("mutefail", session.SteamId.ToString()));
                    return;
                }

                PlayerSession person = GetSession(args[0]);

                if (person == null)
                {
                    hurt.SendChatMessage(session, null, Msg("playernotfound", session.SteamId.ToString()).Replace("{Player}", args[0]));
                }
                else
                {
                    ChatManagerServer ChatMgr = Singleton<ChatManagerServer>.Instance;
                    ulong ID = (ulong)person.SteamId;
                    hurt.BroadcastChat(null, Msg("unmuted", session.SteamId.ToString()).Replace("{Player}", person.Identity.Name));
                    ChatMgr.Unmute(ID);
                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " unmuted " + person.Identity.Name);
                    }
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("freeze")]
        private void FreezeCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.freeze") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                PlayerSession target = GetSession(args[0]);

                if (target != null)
                {
                    CharacterMotorSimple motor = target.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();

                    if (motor.CanMove)
                    {
                        motor.CanMove = false;
                        hurt.SendChatMessage(session, null, Msg("frozenmsg", session.SteamId.ToString()).Replace("{Player}", target.Identity.Name));
                        hurt.SendChatMessage(target, null, Msg("frozen", session.SteamId.ToString()));

                        if ((bool)Config["LogFreeze"])
                        {
                            globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") FROZE " + target.Identity.Name + " (" + target.SteamId + ")");
                            SaveGlobalLog();
                        }
                    }
                    else
                    {
                        motor.CanMove = true;
                        hurt.SendChatMessage(session, null, Msg("unfrozenmsg", session.SteamId.ToString()).Replace("{Player}", target.Identity.Name));
                        hurt.SendChatMessage(target, null, Msg("unfrozen", session.SteamId.ToString()));
                        if ((bool)Config["LogFreeze"])
                        {
                            globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") UNFROZE " + target.Identity.Name + " (" + target.SteamId + ")");
                            SaveGlobalLog();
                        }
                    }
                }
                else
                {
                    hurt.SendChatMessage(session, null, Msg("playernotfound", session.SteamId.ToString()).Replace("{Player}", args[0]));
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("godmode")]
        private void GodmodeCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.godmode") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length != 1)
                {
                    hurt.SendChatMessage(session, null, Msg("godfail", session.SteamId.ToString()));
                    return;
                }

                if (args[0] == "on")
                {
                    if (Godlist.Contains((ulong)session.SteamId))
                    {
                        hurt.SendChatMessage(session, null, Msg("alreadygod", session.SteamId.ToString()));
                        return;
                    }

                    Heal(session);
                    AlertManager.Instance.GenericTextNotificationServer("Godmode Enabled", session.Player);
                    Godlist.Add((ulong)session.SteamId);
                    SaveGodlist();

                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " turned GODMODE on");
                    }

                    if ((bool)Config["LogGodMode"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") turned GODMODE on");
                        SaveGlobalLog();
                    }
                }
                else if (args[0] == "off")
                {
                    if (!Godlist.Contains((ulong)session.SteamId))
                    {
                        hurt.SendChatMessage(session, null, Msg("notgod", session.SteamId.ToString()));
                        return;
                    }

                    Heal(session);
                    AlertManager.Instance.GenericTextNotificationServer("Godmode Disabled", session.Player);
                    Godlist.Remove((ulong)session.SteamId);
                    SaveGodlist();

                    if ((bool)Config["ShowConsoleMsg"])
                    {
                        Puts(session.Identity.Name + " turned GODMODE off");
                    }

                    if ((bool)Config["LogGodMode"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") turned GODMODE off");
                    }
                }
                else
                {
                    hurt.SendChatMessage(session, null, Msg("godfail", session.SteamId.ToString()));
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        [ChatCommand("noclip")]
        private void NoClipCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.noclip") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (session.WorldPlayerEntity.gameObject.layer != 12)
                {
                    session.WorldPlayerEntity.gameObject.layer = 12;
                    AlertManager.Instance.GenericTextNotificationServer("NoClip Enabled", session.Player);

                    if ((bool)Config["LogNoClip"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") turned NOCLIP on");
                        SaveGlobalLog();
                    }
                }
                else
                {
                    session.WorldPlayerEntity.gameObject.layer = 17;
                    AlertManager.Instance.GenericTextNotificationServer("NoClip Disabled", session.Player);

                    if ((bool)Config["LogNoClip"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") turned NOCLIP off");
                        SaveGlobalLog();
                    }
                }
            }
        }

        private void Heal(PlayerSession player)
        {
            EntityStats stats = player.WorldPlayerEntity.GetComponent<EntityStats>();
            Dictionary<EntityFluidEffectKey, IEntityFluidEffect> effects = stats.GetFluidEffects();

            foreach (KeyValuePair<EntityFluidEffectKey, IEntityFluidEffect> effect in effects)
            {
                if(!effect.Key.name.Contains("Inventory") && !effect.Key.name.Contains("Armor"))
                    effect.Value.Reset(true);
            }
        }

        private void HealAll()
        {
            foreach (PlayerSession session in GameManager.Instance.GetSessions().Values)
            {
                if (session != null && session.IsLoaded)
                {
                    Heal(session);
                }
            }
        }

        [ChatCommand("heal")]
        private void HealCommand(PlayerSession session, string command, string[] args)
        {
            if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.godmode") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all") || session.IsAdmin)
            {
                if (args.Length > 1)
                {
                    hurt.SendChatMessage(session, null, Msg("healfail", session.SteamId.ToString()));
                    hurt.SendChatMessage(session, null, args.Length.ToString());
                    return;
                }
                if (args.Length == 0)
                {
                    Heal(session);
                    hurt.SendChatMessage(session, null, "Healed");
                    if ((bool)Config["LogHeal"])
                    {
                        globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") HEALED");
                    }
                }
                if (args.Length == 1)
                {
                    PlayerSession target = GetSession(args[0]);
                    if (target != null)
                    {
                        Heal(target);
                        hurt.SendChatMessage(session, null, "Healed " + target.Identity.Name);
                        if ((bool)Config["LogHeal"])
                        {
                            globallog.Add("[" + DateTime.Now + "] " + session.Identity.Name + " (" + session.SteamId + ") HEALED " + target.Identity.Name + " (" + target.SteamId + ")");
                            SaveGlobalLog();
                        }
                    }
                    else
                    {
                        hurt.SendChatMessage(session, null, Msg("playernotfound", session.SteamId.ToString()));
                    }
                }
            }
            else
            {
                hurt.SendChatMessage(session, null, Msg("nopermission", session.SteamId.ToString()));
            }
        }

        private object CheckBan(string checkstring)
        {
            foreach (TBA TempBannedPlayer in TempBans)
            {
                if (TempBannedPlayer.Name == checkstring || TempBannedPlayer.IP == checkstring || TempBannedPlayer.ID == checkstring)
                {
                    return TempBannedPlayer;
                }
            }
            return null;
        }

        private object CheckBanJoin(string IP, string Name, string ID)
        {
            foreach (TBA TempBannedPlayer in TempBans)
            {
                if (TempBannedPlayer.Name == Name || TempBannedPlayer.IP == IP || TempBannedPlayer.ID == ID)
                {
                    return TempBannedPlayer;
                }
            }
            return null;
        }
    }
}
