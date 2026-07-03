using System.Diagnostics;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_NO_Destroy : NMS, IClientHandle
    {
        public string id;
        public NMS_Server_NO_Destroy(string id) : base((int)packets.ServerPackets.NO_Destroy)
        {
            this.id = id;
        }

        public static NMS_Server_NO_Destroy Read(Packet packet)
        {
            // TODO: Read message fields from packet.
            return new NMS_Server_NO_Destroy(packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.Write(id);
        }

        public void ClientHandle()
        {
            GameCore.Instance.DestroyNetworkIdentity(id);
        }
    }
}
