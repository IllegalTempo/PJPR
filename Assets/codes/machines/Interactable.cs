using UnityEngine;
using System.Collections;

public abstract class Interactable : Selectable,IUsable //Interactable is not intrinsicly synced. 
{
    public abstract void OnInteract(PlayerMain who);
}
