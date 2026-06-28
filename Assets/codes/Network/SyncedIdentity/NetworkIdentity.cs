using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

/// <summary>
/// Network Identity give a gameobject a unique identifier. Allow it to be found with NetworkSystem.Instance.FindNetworkIdentity[Identifier]
/// </summary>
public class NetworkIdentity : MonoBehaviour
{
    public string Identifier;
    public ulong Sovereignty = 0; //0 -> Server Authority
    private readonly UniTaskCompletionSource _startTcs = new UniTaskCompletionSource();
    public UniTask StartTask => _startTcs.Task;
    public void ChangeSovereignty(ulong newowner)
    {
        Sovereignty = newowner;
    }

    protected virtual void Start()
    {
        if (string.IsNullOrWhiteSpace(Identifier))
        {
            Debug.LogWarning($"<!>{name} has no NetworkObject Identifier.");
            return;
        }

        if (NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(Identifier))
        {
            Debug.LogError($"NetworkObject Identifier collision: {Identifier} is already registered by {NetworkSystem.Instance.FindNetworkIdentity[Identifier].name}. {name} will not be registered.");
            return;
        }

        NetworkSystem.Instance.FindNetworkIdentity.Add(Identifier, this);
        _startTcs.TrySetResult();


    }

}
