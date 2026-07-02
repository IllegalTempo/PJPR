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

    }
}