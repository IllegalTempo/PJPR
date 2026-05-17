using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SpawnMissionProjections : NMS, IClientHandle
    {
        private readonly int missionCount;
        private readonly string[] missionNames;
        private readonly string[] missionDescriptions;
        private readonly int[] rewardCredits;
        private readonly float[] difficulties;
        private readonly int[] durations;

        public NMS_Server_SpawnMissionProjections(Mission[] missions) : base((int)packets.ServerPackets.SpawnMissionProjections)
        {
            missionCount = missions.Length;
            missionNames = new string[missionCount];
            missionDescriptions = new string[missionCount];
            rewardCredits = new int[missionCount];
            difficulties = new float[missionCount];
            durations = new int[missionCount];

            for (int i = 0; i < missionCount; i++)
            {
                missionNames[i] = missions[i].missionName;
                missionDescriptions[i] = missions[i].missionDescription;
                rewardCredits[i] = missions[i].rewardCredits;
                difficulties[i] = missions[i].difficulty;
                durations[i] = missions[i].estimatedDuration;
            }
        }

        public static NMS_Server_SpawnMissionProjections Read(Packet packet)
        {
            int count = packet.Readint();
            Mission[] missions = new Mission[count];

            for (int i = 0; i < count; i++)
            {
                string name = packet.ReadstringUNICODE();
                string description = packet.ReadstringUNICODE();
                int reward = packet.Readint();
                float difficulty = packet.Readfloat();
                int duration = packet.Readint();

                missions[i] = new Mission(name, description, reward, difficulty * 10f, duration);
            }

            return new NMS_Server_SpawnMissionProjections(missions);
        }

        public override void Write(Packet packet)
        {
            packet.Write(missionCount);

            for (int i = 0; i < missionCount; i++)
            {
                packet.WriteUNICODE(missionNames[i]);
                packet.WriteUNICODE(missionDescriptions[i]);
                packet.Write(rewardCredits[i]);
                packet.Write(difficulties[i]);
                packet.Write(durations[i]);
            }
        }

        public void ClientHandle()
        {
            if (MissionProjectionDisplay.Instance != null)
            {
                Mission[] missions = new Mission[missionCount];
                for (int i = 0; i < missionCount; i++)
                {
                    missions[i] = new Mission(
                        missionNames[i],
                        missionDescriptions[i],
                        rewardCredits[i],
                        difficulties[i] * 10f,
                        durations[i]
                    );
                }
                MissionProjectionDisplay.Instance.ShowMissions(missions);
            }
        }
    }
}
