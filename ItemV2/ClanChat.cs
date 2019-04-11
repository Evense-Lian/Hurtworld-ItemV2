using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("ClanChat", "Kaidoz", "1.0.0")]
    [Description("")]
    class ClanChat : HurtworldPlugin
    {
        string prefix = "<color=orange>[Clan]</color>";

        [ChatCommand("c")]
        private void ClanCommand(PlayerSession session, string cmd, string[] args)
        {
            if(session.Identity.Clan==null)
            {
                SendChat(session, "У вас нет клана!");
                return;
            }
            string msg = string.Join(" ", args);
            foreach(var id in session.Identity.Clan.GetMemebers())
            {
                PlayerSession clanM = Player.FindById(id.ToString());
                if(clanM!=null)
                    SendChat(clanM, session.Identity.Name + ": " + msg);
            }
        }

        void SendChat(PlayerSession session, string msg) => hurt.SendChatMessage(session, null, prefix + " " + msg);
    }
}
