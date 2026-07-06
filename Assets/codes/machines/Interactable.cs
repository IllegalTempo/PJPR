using UnityEngine;
using System.Collections;

public abstract class Interactable : Selectable,IUsable //Interactable is not intrinsicly synced. 
{
    public abstract void OnInteract_press(PlayerMain who);
    public virtual void OnInteract_release(PlayerMain who) { }
}
