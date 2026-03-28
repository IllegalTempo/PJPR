using UnityEngine;

public class WashingMachineButton : button
{
    [SerializeField]
    private SplashController splashController;


    [SerializeField]
    private bool triggerSplashOnClick = true;

    private float offset = 0.05f;

    public override void OnInteract(PlayerMain who)
    {

        base.OnInteract(who);

        if (triggerSplashOnClick && splashController != null)
        {
            splashController.Splash();
        }
    }

    public void click()
    {
        transform.position = transform.position + transform.right * offset;
    }
    public void release()
    {
        transform.position = transform.position - transform.right * offset;
    }


}
