using UnityEngine;
using System.Collections;

namespace Assets.codes.Network.Messages
{
	public interface IServerHandle
	{
		public void ServerHandle(NetworkPlayer p);	
    }
}