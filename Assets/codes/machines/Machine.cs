using Assets.codes.Network.Messages;
using System.Collections;
using UnityEngine;
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
    public override void OnInteract_press(PlayerMain who)
    {
        base.OnInteract_press(who);
        NMS_Both_MachineInteract msg = new NMS_Both_MachineInteract(identity.Identifier);
        msg.SendMessageAsServerOrClient();
    }
}
