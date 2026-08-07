using Assets.codes.Network.Messages;
using System.Collections;
using UnityEngine;
/// <summary>
/// Machine is defined as an interactable that is synced. 
/// ServerActionOnInteract() only runs on server, typically do Object spawning etc.
/// ShareActionOnInteract() runs on both, often use to do visuals 
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public abstract class SyncedMachine : Interactable //Machine should be synced
{
    
    protected NetworkIdentity identity;
    public PlayerMain pressedByPlayer;
    public bool IsPressed => pressedByPlayer != null;
    protected virtual void ServerActionOnInteract_press(PlayerMain who) { }
    protected virtual void ShareActionOnInteract_press(PlayerMain who) {pressedByPlayer = who;}

    protected virtual void ServerActionOnInteract_release() {}
    protected virtual void ShareActionOnInteract_release() {pressedByPlayer = null;}
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
        SendInteractMessage((int)InteractionType.Press, who);
    }
    public override void OnInteract_release(PlayerMain who)
    {
        base.OnInteract_release(who);
        SendInteractMessage((int)InteractionType.Release, who);
    }
    private void SendInteractMessage(int interacttype, PlayerMain who)
    {
        NMS_Both_MachineInteract msg = new NMS_Both_MachineInteract(identity.Identifier, interacttype, who.networkinfo.steamID);
        msg.SendMessageAsServerOrClient();
    }
    public void OnNetworkApplyAction(int interacttype, PlayerMain who)
    {
        switch (interacttype)
        {
            case 0:
                ShareActionOnInteract_press(who);
                break;
            case 1:
                ShareActionOnInteract_release();
                break;
           
            
        }

    }
    public void OnNetworkApplyActionServer(int interacttype, PlayerMain who)
    {
        switch (interacttype)
        {
           
            case 0:
                ServerActionOnInteract_press(who);
                break;
            case 1:
                ServerActionOnInteract_release();
                break;
        }
    }

}
