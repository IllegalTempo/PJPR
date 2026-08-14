using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Selectable))]
public class Interactable:MonoBehaviour
{

    public virtual void OnInteract_press(PlayerMain who)
    {
        if (who == null) return;
    }
    public virtual void OnInteract_release(PlayerMain who)
    {
        if (who == null) return;

    }
}