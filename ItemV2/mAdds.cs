using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("mAdds", "Mroczny и pasha_94", "1.0.6")]
    [Description("Удобные команды для админов")]

    class mAdds : HurtworldPlugin
    {
        /*  Support Plugin
         *  Поддержка плагина
         *  https://oxide-russia.ru/plugins/669/
         * */

        void LoadDefaultMessages()
        {
            // RU
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"msg_Prefix", "<color=lime>[mAdds]</color> "},
                {"msg_noperm", "У Вас нет прав для использования этой команды"},
                {"msg_pl", "Язык сервера изменен на польский"},
                {"msg_ru", "Язык сервера изменен на русский"},
                {"msg_eng", "Язык сервера изменен на английский"},
                {"msg_save", "Карта сохранена"},
                {"msg_day", "Установлен ДЕНЬ"},
                {"msg_night", "Установлена НОЧЬ"},
                {"msg_cc", "Очистка чата прошла успешно"},
                {"msg_eacon", "EAC включен"},
                {"msg_eacoff", "EAC отключен"},
                {"msg_shutdownhelp", "/shutdown <сек>"},
                {"msg_adds.shutdown", "adds.shutdown <cек>"},
                {"msg_adds.say", "adds.say <сообщение>"},
                {"msg_say", "/say <сообщение>"},
                {"msg_woff", "Погода остановлена"},
                {"msg_eachelp", "/eac ON|OFF|Check."},
                {"msg_rai", "Все животные были удалены"}
            }, this, "ru");

            // PL
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"msg_Prefix", "<color=lime>[mAdds]</color> "},
                {"msg_noperm", "Nie masz uprawnien do tej komendy"},
                {"msg_pl", "Zmieniłeś swój język na PL"},
                {"msg_save", "Zapisano stan serwera"},
                {"msg_day", "Ustawiles Dzien"},
                {"msg_night", "Ustawiles Noc"},
                {"msg_cc", "Czat został wyczyszczony!"},
                {"msg_eacon", "Pomyślnie włączyłeś EAC"},
                {"msg_eacoff", "Pomyślnie wyłączyłeś EAC"},
                {"msg_shutdownhelp", "Poprawne użycie: /shutdown <czas>"},
                {"msg_adds.shutdown", "Poprawne uzycie: adds.shutdown <czas>"},
                {"msg_adds.say", "adds.say <wiadomosc>"},
                {"msg_say", "Poprawne użycie: /say"},
                {"msg_woff", "Wyłączono warunki pogodowe!"},
                {"msg_eachelp", "Poprawne użycie: /eac ON|OFF|Check"},
                {"msg_rai", "Wszystkie zwierzęta zostały usunięte"}
            }, this, "pl");
        }

        string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);

        protected override void LoadDefaultConfig()
        {
            Config["TimeManager", "DayLength"] = 600;
            Config["TimeManager", "NightLength"] = 60;

            Config["WelcomeAlert", "Enabled"] = true;
            Config["WelcomeAlert", "Message"] = "\n\nДобро пожаловать на сервер!\n\n";
            //Config["WelcomeAlert", "Message"] = "\n\nWitamy na serwerze!\n\n";

            Config["Messages", "Welcome"] = new List<string>
            {
                "<color=#5B5B5B>##</color> Твой магазин.ru <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> Привет, <color=#EFE3A6>{Name}</color>",
                "<color=#5B5B5B>»</color> Хорошей игры! ",
                "<color=#5B5B5B>############</color>"
            };
            /*{
                "<color=#5B5B5B>##</color> TwójSerwer.pl <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> Witaj, <color=#EFE3A6>{Name}</color>",
                "<color=#5B5B5B>»</color> Życzymy miłej gry! ",
                "<color=#5B5B5B>############</color>"
            };*/

            Config["Messages", "HelpCmd"] = new List<string>
            {
                "<color=#5B5B5B>##</color> Помощь <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/kits</color> - Киты.",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/roll</color> - Ролы",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/ping</color> - Твой пинг",
                "<color=#5B5B5B>##########</color>",
            };
            /*{
                "<color=#5B5B5B>##</color> Pomoc <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/kits</color> - Zestawy.",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/roll</color> - Losowania",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/ping</color> - Sprawdź swój ping",
                "<color=#5B5B5B>##########</color>",
            };*/

            Config["Messages", "RulesCmd"] = new List<string>
            {
                "<color=#5B5B5B>##</color> Правила <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> Не играть с читами на сервере!",
                "<color=#5B5B5B>»</color> Нельзя создавать базы с помощью канги!",
                "<color=#5B5B5B>»</color> Не спамить в чате!",
                "<color=#5B5B5B>»</color> Слидить за ртом!",
                "<color=#5B5B5B>»</color> Не доставать админа!",
                "<color=#5B5B5B>###########</color>"
            };
            /*{
                "<color=#5B5B5B>##</color> Zasady <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> Zakaz czitowania na serwerze!",
                "<color=#5B5B5B>»</color> Zakaz budowania baz na kangę!",
                "<color=#5B5B5B>»</color> Zakaz spamowania na czacie!",
                "<color=#5B5B5B>»</color> Zachowaj kulturę na czacie!",
                "<color=#5B5B5B>»</color> Admin to nie <color=#EFE3A6>taksówka</color>!",
                "<color=#5B5B5B>###########</color>"
            };*/

            Config["Messages", "VipCmd"] = new List<string>
            {
                "<color=#5B5B5B>##</color> VIP <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/kit vip</color> - Кин для випа.",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/roll vip</color> - Рол для випа.",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/sethome</color> - Поставить точку дома! ",
                "<color=#5B5B5B>########</color>"
            };
            /*            {
                "<color=#5B5B5B>##</color> VIP <color=#5B5B5B>##</color>",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/kit vip</color> - Specjalny zestaw dla VIPa.",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/roll vip</color> - Specjalne Losowanie dla VIPa.",
                "<color=#5B5B5B>»</color> <color=#1e88bc>/sethome</color> - Możliwość użycia 10 domów! ",
                "<color=#5B5B5B>########</color>"
            };*/

            SaveConfig();
        }

        List<string> perms = new List<string>()
        {
            "madds.position",
            "madds.weatheroff",
            "madds.clearchat",
            "madds.shutdown",
            "madds.day",
            "madds.night",
            "madds.save",
            "madds.say",
            "madds.eac",
            "madds.rai"
        };

        void Loaded()
        {
            foreach(string perm in perms)
            {
                if(!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            }
        }

        void Init()
        {
            TimeManager.Instance.DayLength = Convert.ToSingle(Config["TimeManager", "DayLength"]);
            TimeManager.Instance.NightLength = Convert.ToSingle(Config["TimeManager", "NightLength"]);
        }

        void OnServerInitialized()
        {
            lang.SetServerLanguage("pl"); // Ставим "ru" и сервер будет брать сообщения с lang/ru
            LoadDefaultMessages();
        }

        [ChatCommand("pomoc")]
        void HelpCommand(PlayerSession p, string command, string[] args)
        {
            for (var i = 0; i < (Config["Messages", "HelpCmd"] as List<object>).Count; i++)
            {
                hurt.SendChatMessage(p, null, $"{(Config["Messages", "HelpCmd"] as List<object>)[i]}");
            }
        }

        [ChatCommand("zasady")]
        void RulesCommand(PlayerSession p, string command, string[] args)
        {
            for (var i = 0; i < (Config["Messages", "RulesCmd"] as List<object>).Count; i++)
            {
                hurt.SendChatMessage(p, null, $"{(Config["Messages", "RulesCmd"] as List<object>)[i]}");
            }
        }

        [ChatCommand("vip")]
        void VipCommand(PlayerSession p, string command, string[] args)
        {
            for (var i = 0; i < (Config["Messages", "VipCmd"] as List<object>).Count; i++)
            {
                hurt.SendChatMessage(p, null, $"{(Config["Messages", "VipCmd"] as List<object>)[i]}");
            }
        }

        [ChatCommand("pl")]
        void PLCommand(PlayerSession p, string command, string[] args)
        {
            hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_pl", p.SteamId.ToString()));
            lang.SetLanguage("pl", p.SteamId.ToString());
        }

        [ChatCommand("en")]
        void ENCommand(PlayerSession p, string command, string[] args)
        {
            hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_eng", p.SteamId.ToString()));
            lang.SetLanguage("en", p.SteamId.ToString());
        }

        [ChatCommand("ru")]
        void RUCommand(PlayerSession p, string command, string[] args)
        {
            hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_eng", p.SteamId.ToString()));
            lang.SetLanguage("en", p.SteamId.ToString());
        }

        [ChatCommand("save")]
        void SaveCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.save") || p.IsAdmin)
            {
                SaveServer();
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_save", p.SteamId.ToString()));
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("day")]
        void DayCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.day") || p.IsAdmin)
            {
                Server.Command("settime 0.1");
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_day", p.SteamId.ToString()));
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("night")]
        void NightCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.night") || p.IsAdmin)
            {
                Server.Command("settime 1");
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_night", p.SteamId.ToString()));
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("cc")]
        void ClearChatCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.clearchat") || p.IsAdmin)
            {
                for (int i = 0; i < 100; i++)
                {
                    hurt.BroadcastChat(null, "");
                }
                hurt.BroadcastChat(Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_cc", p.SteamId.ToString()));
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("eac")]
        void EACCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.eac") || p.IsAdmin)
            {
                if (args.Length == 0)
                {
                    hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_eachelp", p.SteamId.ToString()));
                }
                else if (args[0] == "on")
                {
                    hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_eacon", p.SteamId.ToString()));
                    GameManager.Instance.ServerConfig.EnforceEac = true;
                }
                else if (args[0] == "off")
                {
                    hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_eacoff", p.SteamId.ToString()));
                    GameManager.Instance.ServerConfig.EnforceEac = false;
                }
                else if (args[0] == "check" || args[0] == "c")
                {
                    hurt.SendChatMessage(p, null, "Status EAC " + GameManager.Instance.ServerConfig.EnforceEac.ToString()
                    .Replace("False", "OFF")
                    .Replace("True", "ON"));
                }
                else
                {
                    hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_eachelp", p.SteamId.ToString()));
                }
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("shutdown")]
        void ShutdownCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.shutdown") || p.IsAdmin)
            {
                if (args.Length == 0)
                {
                    hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_shutdownhelp", p.SteamId.ToString()));
                }
                else if (args.Length == 1)
                {
                    hurt.BroadcastChat(null, "shutdown " + args[0] + "sec!");
                    SaveServer();
                    timer.Once(Convert.ToSingle(args[0]), () =>
                    {
                        Puts("Shutting down server..");
                        GameManager.Instance.Quit();
                    });
                }
                else
                {
                    hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_shutdownhelp", p.SteamId.ToString()));
                }
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ConsoleCommand("adds.shutdown")]
        void ConsoleShutdownCommand(string commandString, string[] args)
        {
            if (args.Length == 0)
            {
                Puts(Msg("msg_adds.shutdown"));
            }
            else if (args.Length == 1)
            {
                hurt.BroadcastChat(null, "Shutdown " + args[0] + " sec");
                SaveServer();
                timer.Once(Convert.ToSingle(args[0]), () =>
                {
                    Puts("Shutting down server!");
                    GameManager.Instance.Quit();
                });
            }
        }

        [ConsoleCommand("adds.say")]
        void ConsoleSayCommand(string commandString, string[] args)
        {
            if (args.Length == 0)
            {
                Puts(Msg("msg_adds.say"));
            }
            if (args.Length > 0)
            {
                hurt.BroadcastChat(null, "Console " + string.Join(" ", args));
            }
        }

        [ChatCommand("say")]
        void SayCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.say") || p.IsAdmin)
            {
                if (args.Length == 0)
                {
                    hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_say", p.SteamId.ToString()));
                }
                if (args.Length > 0)
                {
                    hurt.BroadcastChat(null, "");
                    hurt.BroadcastChat(null, "<size=20><color=#5B5B5B>»</color> " + string.Join(" ", args) + "</size>");
                }
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("ping")]
        void PingCommand(PlayerSession p, string command)
        {
            hurt.SendChatMessage(p, null, "Ping: " + p.Player.averagePing);
        }

        [ChatCommand("online")]
        void OnlineCommand(PlayerSession p, string command)
        {
            hurt.SendChatMessage(p, null, "Online: " + GameManager.Instance.GetPlayerCount() + "/" + GameManager.Instance.ServerConfig.MaxPlayers);
        }

        [ChatCommand("woff")]
        void WeatherOFFCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.weatheroff") || p.IsAdmin)
            {
                Server.Command("destroyall RainstormServer");
                Server.Command("destroyall SandstormServer");
                Server.Command("destroyall BlizzardServer");
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_woff", p.SteamId.ToString()));
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("pos")]
        void PosCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.position") || p.IsAdmin)
            {
                float x = (float)Math.Round((decimal)p.WorldPlayerEntity.transform.position.x, 1);
                float y = (float)Math.Round((decimal)p.WorldPlayerEntity.transform.position.y, 1);
                float z = (float)Math.Round((decimal)p.WorldPlayerEntity.transform.position.z, 1);
                hurt.SendChatMessage(p, null, "POS: x: " + x + " y: " + y + " z: " + z);
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        [ChatCommand("rai")]
        void RaiCommand(PlayerSession p, string command, string[] args)
        {
            if (permission.UserHasPermission(p.SteamId.ToString(), "madds.rai") || p.IsAdmin)
            {
                Server.Command("destroyall gavku");
                Server.Command("destroyall yeti");
                Server.Command("destroyall bor");
                Server.Command("destroyall tokar");
                Server.Command("destroyall shigi");
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_rai", p.SteamId.ToString()));
            }
            else
            {
                hurt.SendChatMessage(p, null, Msg("msg_Prefix", p.SteamId.ToString()) + Msg("msg_noperm", p.SteamId.ToString()));
            }
        }

        void OnPlayerConnected(PlayerSession p)
        {
            if (p != null && p.IsLoaded)
            {
                if (Convert.ToBoolean(Config["WelcomeAlert", "Enabled"]))
                {
                    SendAlert(p, $"{Config["WelcomeAlert", "Message"]}");
                }
                for (var i = 0; i < (Config["Messages", "Welcome"] as List<object>).Count; i++)
                {
                    hurt.SendChatMessage(p, null, $"{(Config["Messages", "Welcome"] as List<object>)[i]}"
                        .Replace("{Name}", p.Identity.Name));
                }
            }
        }

        void SaveServer() => GameManager.Instance.StartCoroutine(Singleton<GameSerializer>.Instance.SaveServer("autosave_" + GameManager.Instance.ServerConfig.Map));
        void SendAlert(PlayerSession p, string msg) => AlertManager.Instance.GenericTextNotificationServer(msg, p.Player);
    }
}
