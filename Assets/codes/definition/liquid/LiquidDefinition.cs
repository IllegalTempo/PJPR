using UnityEngine;

[CreateAssetMenu(fileName = "New Liquid", menuName = "Game/Liquid")]
public class LiquidDefinition : ScriptableObject
{
    public float SplashForce;
    public float SplashDuration;
    public AudioClip SplashSound;
}
