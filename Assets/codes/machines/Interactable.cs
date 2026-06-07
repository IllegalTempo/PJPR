using UnityEngine;
using System.Collections;

public abstract class Interactable : Selectable,IUsable
{
    public abstract void OnInteract(PlayerMain who);
}
