using System.Collections.Generic;
using Assets.codes.Network.SyncedIdentity;
using UnityEngine;

namespace Assets.codes.Network
{
    public class NetworkPrefabPool
    {
        public readonly PrefabDefinition prefabDefinition;
        private readonly int PoolSize;
        private Queue<NetworkGameObject> Pool;
        private readonly HashSet<NetworkGameObject> pooledObjects = new HashSet<NetworkGameObject>();
        private readonly Transform poolRoot;

        public NetworkPrefabPool(PrefabDefinition prefabDefinition, int poolSize)
        {
            this.prefabDefinition = prefabDefinition;
            PoolSize = Mathf.Max(0, poolSize);
            Pool = new Queue<NetworkGameObject>(PoolSize);
            poolRoot = new GameObject($"{prefabDefinition.prefabID}_NetworkPrefabPool").transform;
            Prewarm();
        }

        public NetworkGameObject InstantiatePoolNetworkPrefab(string uid,Vector3 pos, Quaternion rot, Transform parent = null)
        {
            if (Pool.Count > 0)
            {
                NetworkGameObject nobj = Pool.Dequeue();
                pooledObjects.Remove(nobj);
                nobj.gameObject.SetActive(true);
                nobj.transform.position = pos;
                nobj.transform.rotation = rot;
                nobj.transform.SetParent(parent);
                
                return nobj;
            } else
            {
                return null;
            }
        }

        private void Prewarm()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                NetworkGameObject nobj = CreatePoolObject();
                if (nobj != null)
                {
                    Return(nobj);
                }
            }
        }

        private NetworkGameObject CreatePoolObject()
        {
            GameObject obj = Object.Instantiate(prefabDefinition.itemPrefab, poolRoot);
            NetworkGameObject nobj = obj.GetComponent<NetworkGameObject>();
            if (nobj == null)
            {
                Debug.LogError($"The prefab {prefabDefinition.prefabID} does not have a NetworkGameObject component attached.");
                Object.Destroy(obj);
                return null;
            }

            return nobj;
        }

        public void Return(NetworkGameObject nobj)
        {
            if (nobj == null)
            {
                return;
            }

            if (pooledObjects.Contains(nobj))
            {
                return;
            }

            if (nobj.Identity != null)
            {
                nobj.Identity.Unregister();
            }

            nobj.transform.SetParent(poolRoot);
            nobj.gameObject.SetActive(false);
            Pool.Enqueue(nobj);
            pooledObjects.Add(nobj);
        }

        public void Return(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            Return(obj.GetComponent<NetworkGameObject>());
        }

        public void Cleanup()
        {
            foreach (NetworkGameObject nobj in pooledObjects)
            {
                if (nobj != null)
                {
                    Object.Destroy(nobj.gameObject);
                }
            }

            pooledObjects.Clear();
            Pool.Clear();

            if (poolRoot != null)
            {
                Object.Destroy(poolRoot.gameObject);
            }
        }
    }
}
