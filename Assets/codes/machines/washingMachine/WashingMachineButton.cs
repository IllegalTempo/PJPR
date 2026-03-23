using UnityEngine;

public class WashingMachineButton : button
{
    [SerializeField]
    private SplashController splashController;

    [SerializeField]
    private LiquidType liquidType = LiquidType.Water;

    [SerializeField]
    private bool triggerSplashOnClick = true;

    public override void OnInteract(PlayerMain who)
    {

        base.OnInteract(who);

        if (triggerSplashOnClick && splashController != null)
        {
            splashController.Splash(liquidType);
        }
    }

    public void SetLiquidType(LiquidType newLiquidType)
    {
        liquidType = newLiquidType;
    }

    public void SetSplashController(SplashController controller)
    {
        splashController = controller;
    }

    public LiquidType GetLiquidType()
    {
        return liquidType;
    }
}
