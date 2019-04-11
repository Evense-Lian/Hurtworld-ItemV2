using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("AdminJoin", "Force", "1.0.0")]
    [Description("Уведомляет Админ вы или Игрок.")]

    class AdminJoin : CovalencePlugin
    {
        [JsonProperty("Включить выключить сообщения")]
        private bool ShowAdmin = true;

        [JsonProperty("Время показа после захода в игру")]
        private float TimeConnect = 10f;

        [JsonProperty("Цвет сообщения Админа")]
        public string Color_Admin = "#000000";

        [JsonProperty("Цвет сообщения Игрока")]
        public string Color_Player = "#FF00FF";


        private void Loaded()
        {
            PrintWarning("Плагин успешно загружен");
        }

        [Command("Admin")]
        void AdminCommand(IPlayer player, string command, string[] args)
        {

            if (player.IsAdmin)
            {
                player.Reply("Привет, ты сейчас Админ, будь уверен.");
            }

            else

            {
                player.Reply("У вас нету прав на данную команду.");
            }

        }

        void OnUserConnected(IPlayer player)
        {
            if (!ShowAdmin)
                return;

            string message = string.Empty;
            message = player.IsAdmin
                ? $"<color={Color_Admin}>Вы зашли как Администратор.</color>"
                : $"<color={Color_Player}>Вы зашли как простой игрок.</color>";

            timer.Once(TimeConnect, () =>
            player.Reply(message));
        }

    }
}