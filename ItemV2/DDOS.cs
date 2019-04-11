using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("DDOS", "Kaidoz", "1.0.0")]
    public class DDOS : HurtworldPlugin
    {
        object OnConnect(uLink.NetworkPlayer player)
        {
            //Puts(player.externalPort== 1194)
            if (player.externalPort == 1194)
                return true;

            return null;
        }
        /* uLink_OnPlayerConnected
            
		if(Oxide.Core.Interface.Call("OnConnect", player)!=null)
		{
			uLink.Network.CloseConnection(player, false, 0);
			return;
		}


         * */
    }
}
