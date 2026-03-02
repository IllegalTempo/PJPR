using Assets.codes.items;
using UnityEngine;
using UnityEngine.Events;


public class lever : Selectable,IUsable
{
    [SerializeField]
    private UnityEvent onSwitch_On;

    [SerializeField]
    private UnityEvent onSwitch_Off;
    private bool isOn = false;
    public void OnInteract(PlayerMain who)
    {
        if (isOn)
        {
            isOn = false;
            onSwitch_Off.Invoke();
        }
        else
        {
            isOn = true;
            onSwitch_On.Invoke();
        }


    }

}
