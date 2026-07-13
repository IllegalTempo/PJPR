
using UnityEngine;
using UnityEngine.Events;


public class Lever: Interactable
{
    [SerializeField]
    private UnityEvent onSwitch_On;

    [SerializeField]
    private UnityEvent onSwitch_Off;
    private bool isOn = false;
    public override void OnInteract_press(PlayerMain who)
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
