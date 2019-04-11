using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("NoFriendlyFire", "Kaidoz", "1.0.0")]
    class NoFriendlyFire : HurtworldPlugin
    {

        private void OnPlayerTakeDamage(PlayerSession session, EntityEffectSourceData source)
        {
            string name = session.Identity.Name;
            string tmpName = GetNameOfObject(source.EntitySource);
            if (tmpName.EndsWith("(P)"))
            {
                var sesskiller = getSession(tmpName.Remove(tmpName.Length - 3));

                if (sesskiller == null)
                    return;

                if(sesskiller.Identity.Clan!=null)
                {
                    if(sesskiller.Identity.Clan.GetMemebers().Contains((ulong)session.SteamId))
                    {
                        Notice(sesskiller, "<color=lime>Это твой союзник!</color>");
                        source.LastEffectSource = null;
                    }
                }
            }
        }

        void Notice(PlayerSession session, string message) => Singleton<AlertManager>.Instance.GenericTextNotificationServer(message, session.Player);

        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
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
