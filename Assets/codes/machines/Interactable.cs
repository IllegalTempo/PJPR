using Assets.codes.Network.Messages;
using System.Collections;
using System.Security.Principal;
using UnityEngine;

public abstract class Interactable : Selectable,IUsable //Interactable is not intrinsicly synced. 
{
    public virtual void OnInteract_press(PlayerMain who)
    {
        if (who == null) return;

        
    }
    public virtual void OnInteract_release(PlayerMain who) { }
}
