using UnityEngine;

public class WashingMachineSetup : MonoBehaviour
{
    public void SetupWashingMachine()
    {
        SplashController splashController = GetComponent<SplashController>();
        if (splashController == null)
        {
            Debug.LogError("SplashController not found! Add it to this GameObject first.");
            return;
        }

        Debug.Log("Washing Machine Setup Complete!");
    }

    // Example method to call from a button
    public void OnWaterButtonPressed()
    {
        GetComponent<SplashController>().Splash(LiquidType.Water);
    }

    public void OnHClButtonPressed()
    {
        GetComponent<SplashController>().Splash(LiquidType.HCl);
    }

    public void OnNaOHButtonPressed()
    {
        GetComponent<SplashController>().Splash(LiquidType.NaOH);
    }

    public void OnSoapButtonPressed()
    {
        GetComponent<SplashController>().Splash(LiquidType.Soap);
    }

    public void OnBleachButtonPressed()
    {
        GetComponent<SplashController>().Splash(LiquidType.Bleach);
    }
}
