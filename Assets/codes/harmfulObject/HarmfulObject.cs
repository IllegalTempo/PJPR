using UnityEngine;

public enum HarmfulObjectType // enum w
{
    Other,
    Meteorite,
    MeteoriteFragment
}

public abstract class HarmfulObject : MonoBehaviour
{
    [SerializeField] private HarmfulObjectType harmfulObjectType = HarmfulObjectType.Other;
    [SerializeField] private float damageToShipPart = 10f;

    public HarmfulObjectType Type => harmfulObjectType;
    public float DamageToShipPart => damageToShipPart;

    protected void SetHarmfulObjectType(HarmfulObjectType type)
    {
        harmfulObjectType = type;
    }
}