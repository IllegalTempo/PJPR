using UnityEngine;
using System.Collections;
/// <summary>
/// Machine is defined as an interactable that is synced. 
/// ServerActionOnInteract() only runs on server, typically do Object spawning etc.
/// ShareActionOnInteract() runs on both, often use to do visuals 
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public abstract class Machine : Interactable //Machine should be synced
{
    protected NetworkIdentity identity;
    public abstract void ServerActionOnInteract();
    public abstract void ShareActionOnInteract();
    protected virtual void Start()
    {
        identity = GetComponent<NetworkIdentity>();
        if(identity == null)
        {
            Debug.LogError("GameObject " + gameObject.name + " don't have a identity");
        }

    }
}
