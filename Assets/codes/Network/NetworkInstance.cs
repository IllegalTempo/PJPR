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
        private readonly Dictionary<ulong, NetworkPlayerObject> _players = new Dictionary<ulong, NetworkPlayerObject>();
        public readonly Dictionary<string, NetworkPrefabPool> PrefabPools = new Dictionary<string, NetworkPrefabPool>();
        public List<Slot> Slots = new List<Slot>();
        public IEnumerable<NetworkPlayerObject> Players => _players.Values;
        public int PlayerCount => _players.Count;

        public NetworkPlayerObject GetPlayer(ulong steamId)
        {
            _players.TryGetValue(steamId, out NetworkPlayerObject player);
            return player;
        }

        public bool TryGetPlayer(ulong steamId, out NetworkPlayerObject player)
        {
            return _players.TryGetValue(steamId, out player);
        }

        public void SetPlayer(ulong steamId, NetworkPlayerObject player)
        {
            _players[steamId] = player;
        }

        public bool RemovePlayer(ulong steamId)
        {
            return _players.Remove(steamId);
        }

        public void ClearPlayers()
        {
            _players.Clear();
        }

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
            foreach (NetworkPlayerObject player in _players.Values)
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

            _players.Clear();
            FindNetworkIdentity.Clear();
            PrefabPools.Clear();
            Slots.Clear();
        }
    }
}
