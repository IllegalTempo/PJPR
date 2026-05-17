using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    public class NMS_Client_RequestMissions : NMS, IServerHandle
    {
        private readonly string terminalNetworkObjectId;

        public NMS_Client_RequestMissions(string terminalId) : base((int)packets.ClientPackets.RequestMissions)
        {
            this.terminalNetworkObjectId = terminalId;
        }

        public static NMS_Client_RequestMissions Read(Packet packet)
        {
            return new NMS_Client_RequestMissions(packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(terminalNetworkObjectId);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            // Server receives request for missions from client
            // TODO: Validate request, generate missions, send back to all players
            Debug.Log($"Server received mission request from terminal: {terminalNetworkObjectId}");
        }
    }
}
