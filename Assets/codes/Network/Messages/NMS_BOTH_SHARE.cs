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
            applyaction();
        }
        public virtual void ServerHandle(NetworkPlayer p)
        {
            applyaction();
            NetworkRouter.Instance.DistributeMessageToReady(this, p.steamId);
        }
    }
}