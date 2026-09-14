using UnityEngine;

[RequireComponent(typeof(Selectable))]
public class SpaceshipPart : Destroyable
{
    [Header("Identity")]
    [SerializeField] private string partName = "Hull";

    [Header("Collision Damage")]
    [SerializeField] private bool enableCollisionDamage = true;

    private Selectable selectableComponent;
    [Header("Broken Visibility")]
    [SerializeField, Range(0f, 1f)] private float brokenHiddenOpacity = 0f;



    private void OnCollisionEnter(Collision collision)
    {
        if (!enableCollisionDamage || IsBroken )
        {
            return;
        }
        float damage = GetDamageFromCollision(collision.gameObject);
        if (damage > 0f)
        {
            OnDamage(damage, collision.gameObject.tag);
        }
    }

    private float GetDamageFromCollision(GameObject other)
    {
        return 5;
    }

}
