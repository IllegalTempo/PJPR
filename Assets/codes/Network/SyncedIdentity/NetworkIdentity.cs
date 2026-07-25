using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using Assets.codes.Network.SyncedIdentity;

/// <summary>
/// Network Identity give a gameobject a unique identifier. Allow it to be found with NetworkSystem.Instance.FindNetworkIdentity[Identifier]
/// </summary>
public class NetworkIdentity : MonoBehaviour
{
    public string Identifier;
    public ulong Sovereignty = 0; //0 -> Server Authority
    [System.NonSerialized] public bool IsPooled = false;
    private readonly UniTaskCompletionSource _startTcs = new UniTaskCompletionSource();
    public UniTask StartTask => _startTcs.Task;
    public void ChangeSovereignty(ulong newowner)
    {
        Sovereignty = newowner;
    }
    public void Unregister()
    {
        if (NetworkSystem.Instance == null || string.IsNullOrWhiteSpace(Identifier))
        {
            return;
        }

        if (NetworkSystem.Instance.FindNetworkIdentity.TryGetValue(Identifier, out NetworkIdentity registeredIdentity) && registeredIdentity == this)
        {
            NetworkSystem.Instance.FindNetworkIdentity.Remove(Identifier);
        }
    }
    public void Reinitialize(string id)
    {
        Unregister();
        Identifier = id;
        initWithID(id);
    }
    private string GetChildIdentifier(GameObject child)
    {
        return Identifier + "_" + child.name;   
    }

    protected virtual void Start()
    {
        if (!IsPooled && !string.IsNullOrEmpty(Identifier))
        {
            Register();
        }
    }
    public void Register()
    {
        initWithID(Identifier);
        foreach (NetworkChildIdentity childid in GetComponentsInChildren<NetworkChildIdentity>())
        {
            string id = GetChildIdentifier(childid.gameObject);
            childid.Identifier = id;
            childid.initWithID(id);
        }

    }
    public void initWithID(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"<!>{name} has no NetworkObject Identifier.");
            return;
        }

        if (NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(id))
        {
            if (NetworkSystem.Instance.FindNetworkIdentity[id] == this)
            {
                _startTcs.TrySetResult();
                return;
            }

            Debug.LogError($"NetworkObject Identifier collision: {id} is already registered by {NetworkSystem.Instance.FindNetworkIdentity[id].name}. {name} will not be registered.");
            return;
        }

        NetworkSystem.Instance.FindNetworkIdentity.Add(id, this);
        _startTcs.TrySetResult();
    }


}
