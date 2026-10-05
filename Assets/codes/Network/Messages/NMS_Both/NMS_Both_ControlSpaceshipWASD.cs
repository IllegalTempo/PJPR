using System;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_ControlSpaceshipWASD : NMS_BOTH_SERVERACTION
    {
        private Vector2 inputVector;
        public NMS_Both_ControlSpaceshipWASD(Vector2 inputVector) : base((int)packets.BothPackets.ControlSpaceshipWASD)
        {
            this.inputVector = inputVector;
        }


        public static NMS_Both_ControlSpaceshipWASD Read(Packet packet)
        {
            return new NMS_Both_ControlSpaceshipWASD(packet.ReadVector2());
        }

        public override void Write(Packet packet)
        {
            packet.Write(inputVector);
        }

        protected override void applyaction()
        {
            // TODO: Apply shared client/server behavior.
        }

        protected override void serverAction()
        {
            MainSpaceship.Instance.ServerMoveMainSpaceship(inputVector);
        }
    }
}
