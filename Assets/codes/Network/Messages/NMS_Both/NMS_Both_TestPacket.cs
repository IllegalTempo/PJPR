using Assets.codes.Network.Messages;
using Steamworks;
using System;
using System.Threading.Tasks;
using UnityEngine;


namespace Assets.codes.Network.Packets.BothMessages
{
    public class NMS_Both_TestPacket : NMS,IClientHandle,IServerHandle
    {
        string TestString;
        long TestTime;
        ulong clientSteamID;
        public NMS_Both_TestPacket(string testString, long testTime, ulong clientSteamID) : base((int)packets.BothPackets.Test)
        {
            TestString = testString;
            TestTime = testTime;
            this.clientSteamID = clientSteamID;
        }
        public static NMS_Both_TestPacket Read(Packet packet)
        {
            string testString = packet.ReadstringUNICODE();
            long testTime = packet.Readlong();
            ulong clientSteamID = packet.Readulong();

            return new NMS_Both_TestPacket(testString, testTime, clientSteamID);
        }
        public override void Write(Packet p)
        {
            p.WriteUNICODE(TestString);
            p.Write(TestTime);
            p.Write(clientSteamID);
        }
        public void ServerHandle(NetworkPlayer p) //This is what the server do
        {
            if (TestString == NetworkRouter.TestRandomUnicode)
            {
                
                Debug.Log($"Test Passed ✅, delay:{(DateTime.UtcNow.Ticks - TestTime) / 10000}ms");
                //trigger listeners
                NetworkSystem.Instance.NetworkListener.RaisePlayerJoinSuccessful(p);

            }
            else
            {
                Debug.Log($"Check Code Mismatched ❌, Message: {TestString}");
            }
        }

        public async void ClientHandle()
        {
            if (clientSteamID != SteamClient.SteamId)
            {
                Debug.Log("SteamID mismatched, handshake failed");
                NetworkSystem.Instance.Client.Close();

            }
            //NetworkSystem.instance.client.NetworkID = NetworkID;

            if (TestString == NetworkRouter.TestRandomUnicode)
            {
                Debug.Log($"Test Passed ✅, delay:{(DateTime.UtcNow.Ticks - TestTime) / 10000}ms");

            }
            else
            {
                Debug.Log($"Check Code Mismatched ❌, Message: {TestString}");
                NetworkSystem.Instance.Client.Close();

            }
            await Task.Delay(5);
            NetworkRouter.Instance.SendMessageToServer(new NMS_Both_TestPacket(TestString, DateTime.UtcNow.Ticks, SteamClient.SteamId));
        }

    }
}
