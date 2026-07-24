using System.Collections.Generic;
using Assets.codes.Network.SyncedIdentity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.codes.Network
{
    /// <summary>
    /// Per-lobby network state. Create a new instance whenever the player creates or enters a lobby.
    /// </summary>
    public class NetworkInstance
    {
        public readonly Dictionary<string, NetworkIdentity> FindNetworkIdentity = new Dictionary<string, NetworkIdentity>();
        public readonly Dictionary<ulong, NetworkPlayerObject> PlayerList = new Dictionary<ulong, NetworkPlayerObject>();
        public readonly Dictionary<string, NetworkPrefabPool> PrefabPools = new Dictionary<string, NetworkPrefabPool>();
        public List<Slot> Slots = new List<Slot>();

        public NetworkPrefabPool GetPool(PrefabDefinition prefabDefinition)
        {
            if (!PrefabPools.ContainsKey(prefabDefinition.prefabID))
            {
                PrefabPools[prefabDefinition.prefabID] = new NetworkPrefabPool(prefabDefinition, prefabDefinition.poolSize);
            }

            return PrefabPools[prefabDefinition.prefabID];
        }

        public void CleanupScene()
        {
            foreach (NetworkPlayerObject player in PlayerList.Values)
            {
                if (player != null)
                {
                    Object.Destroy(player.gameObject);
                }
            }

            foreach (NetworkIdentity identity in FindNetworkIdentity.Values)
            {
                if (identity != null && identity is NetworkPrefabIdentity)
                {
                    Object.Destroy(identity.gameObject);
                }
            }

            foreach (NetworkPrefabPool pool in PrefabPools.Values)
            {
                pool.Cleanup();
            }

            PlayerList.Clear();
            FindNetworkIdentity.Clear();
            PrefabPools.Clear();
            Slots.Clear();
        }
    }
}
