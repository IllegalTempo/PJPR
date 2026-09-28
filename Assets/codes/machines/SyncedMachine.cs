using Assets.codes.Network.Messages;
using System.Collections;
using UnityEngine;
/// <summary>
/// SyncedMachine is defined as an interactable that is synced. 
/// ServerActionOnInteract() only runs on server, typically do Object spawning etc.
/// ShareActionOnInteract() runs on both, often use to do visuals 
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public abstract class SyncedMachine : Interactable
{
    public enum InteractionType
    {
        Press,
        Release,
        SecondaryPress,
        SecondaryRelease,
    }
    protected NetworkIdentity identity;
    public PlayerMain pressedByPlayer;
    public bool IsPressed => pressedByPlayer != null;
    
    protected virtual void ServerActionOnInteract_press(PlayerMain who) { }
    protected virtual void ServerActionOnInteract_release() { }
    protected virtual void ServerActionOnSecondaryInteract_press(PlayerMain who) { }
    protected virtual void ServerActionOnSecondaryInteract_release(PlayerMain who) { }


    protected virtual void ShareActionOnInteract_release() {pressedByPlayer = null;}
    protected virtual void ShareActionOnInteract_press(PlayerMain who) { pressedByPlayer = who; }
    protected virtual void ShareActionOnSecondaryInteract_press(PlayerMain who) { pressedByPlayer = who; }

    protected virtual void ShareActionOnSecondaryInteract_release(PlayerMain who) { pressedByPlayer = null; }
    protected bool InteractHolding = false;
    protected bool SecondInteractHolding = false;
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
        InteractHolding = true;
        SendInteractMessage((int)InteractionType.Press, who);
    }
    public override void OnInteract_release(PlayerMain who)
    {
        base.OnInteract_release(who);
        InteractHolding = false;
        SendInteractMessage((int)InteractionType.Release, who);
    }
    public override void OnSecondaryInteract_press(PlayerMain who)
    {
        base.OnSecondaryInteract_press(who);
        SecondInteractHolding = true;
        SendInteractMessage((int)InteractionType.SecondaryPress, who);
    }
    public override void OnSecondaryInteract_release(PlayerMain who)
    {
        base.OnSecondaryInteract_release(who);
        SecondInteractHolding = false;
        SendInteractMessage((int)InteractionType.SecondaryRelease, who);
    }
    private void SendInteractMessage(int interacttype, PlayerMain who)
    {
        NMS_Both_MachineInteract msg = new NMS_Both_MachineInteract(identity.Identifier, interacttype, who.networkinfo.steamID, who.GetHeadRotation());
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
            case 2:
                ShareActionOnSecondaryInteract_press(who);
                break;
           case 3:
                ShareActionOnSecondaryInteract_release(who);
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
            case 2:
                ServerActionOnSecondaryInteract_press(who);
                break;
            case 3:
                ServerActionOnSecondaryInteract_release(who);
                break;
        }
    }

}
