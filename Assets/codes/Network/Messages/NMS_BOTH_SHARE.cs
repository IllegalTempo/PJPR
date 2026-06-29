using UnityEngine;
using System.Collections;
using System;

namespace Assets.codes.Network.Messages
{
    public abstract class NMS_BOTH_SHARE : NMS,IClientHandle,IServerHandle
    {
        protected NMS_BOTH_SHARE(int pID) : base(pID)
        {
        }
        public void SendMessageAsServerOrClient()
        {
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline)
            {
                applyaction();
                return;
            }
            if (NetworkSystem.Instance.IsServer)
            {
                NetworkRouter.Instance.DistributeMessageToReady(this);
                applyaction();
            }
            else
            {
                NetworkRouter.Instance.SendMessageToServer(this);
            }
        }
        protected abstract void applyaction();
        public virtual void ClientHandle()
        {
            Debug.Log("[Handle] Received message from server " + "Packet: " + this.GetType().Name);

            applyaction();
        }
        public virtual void ServerHandle(NetworkPlayer p)
        {
            Debug.Log("[Handle] Received message from " + p.SteamName + "Packet: " + this.GetType().Name);
            applyaction();
            //NetworkRouter.Instance.DistributeMessageToReady(this, p.steamId); //todo is this right?
            NetworkRouter.Instance.DistributeMessageToReady(this);
        }
    }
}