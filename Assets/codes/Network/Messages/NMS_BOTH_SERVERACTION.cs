using UnityEngine;
using System.Collections;

namespace Assets.codes.Network.Messages
{
    public abstract class NMS_BOTH_SERVERACTION : NMS_BOTH_SHARE
    {
        protected NMS_BOTH_SERVERACTION(int pID) : base(pID)
        { 
        }
        /// <summary>
        /// serverAction() are actions that will only run on server-side and offline. Often use for server authoritised action
        /// </summary>
        protected abstract void serverAction();
        public override void ServerHandle(NetworkPlayer p)
        {
            serverAction();
            base.ServerHandle(p);
        }
        public override void SendMessageAsServerOrClient()
        {
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline)
            {
                applyaction();
                serverAction();
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
    }
}