using UnityEngine;
using System.Collections;

namespace Assets.codes.Network.Messages
{
	public abstract class NMS
	{
		protected int packetID;
		public int PacketID { 
			get { return packetID; }
        }

        public NMS(int pID)
		{
			packetID = pID;
		}	
		public abstract void Write(Packet p);
    }
}