using UnityEngine;
using UnityEngine.Events;

public class Button: Interactable
{
    [SerializeField]
    private UnityEvent onClicked;
    [SerializeField]
    private UnityEvent onReleased;

    [SerializeField]
    private float releaseTime = 1f; //1 second by default
    public override void OnInteract(PlayerMain who)
    {
        onClicked.Invoke();

        Invoke(nameof(OnRelease), releaseTime);
    }

    private void OnRelease()
    {
        onReleased.Invoke();
    }
    protected override void Update()
    {
        base.Update();

    }
}
