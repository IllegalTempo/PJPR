using UnityEngine;


//optionally carries a MeteoriteRing child.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class Planet : HarmfulObject
{
    [Header("Planet Movement Settings")]
    [SerializeField] private float movementSpeed = 0f; // Disabled by default
    [SerializeField] private Vector3 movementDirection;
    [Header("Planet Properties")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Collision Damage")]
    [SerializeField] private float collisionDamageToEachPart = 40f;
    [SerializeField] private float connectorDamageMultiplier = 0.5f; // relative to part damage
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float impactEffectDuration = 3f;

    [Header("Meteorite Ring (optional)")]
    [SerializeField] private MeteoriteRing meteoriteRing;
    [SerializeField] private bool spawnRingOnStart = false;
    [SerializeField] private GameObject meteoriteRingPrefab;

    private Rigidbody rb;
    private bool IsServerAuthority => NetworkSystem.Instance == null || NetworkSystem.Instance.IsServer;

    private void Awake()
    {
        SetHarmfulObjectType(HarmfulObjectType.Planet);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // use isKinematic true if Planet physics shouldn't be affected by other objects
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // We only broadcast transform if the planet moves!
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null) netObj.Sync_Transform = movementSpeed > 0;

        if (spawnRingOnStart && meteoriteRingPrefab != null && meteoriteRing == null)
        {
            GameObject ringGO = Instantiate(meteoriteRingPrefab, transform.position, Quaternion.identity, transform);
            meteoriteRing = ringGO.GetComponent<MeteoriteRing>();
        }
    }

    private void Update()
    {
        transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime, Space.World);
    }
    
    private void FixedUpdate()
    {
        if (IsServerAuthority && rb != null && movementDirection.magnitude > 0 && movementSpeed > 0)
        {
            rb.MovePosition(rb.position + movementDirection.normalized * movementSpeed * Time.fixedDeltaTime);
        }
    }

    public void InitializeMovement(Vector3 direction, float speed)
    {
        movementDirection = direction;
        movementSpeed = speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only the server applies damage; clients just see the visual collision
        if (!IsServerAuthority) return;

        GameObject hit = collision.gameObject;
        Spaceship spaceship = hit.GetComponent<Spaceship>();
        if (spaceship == null)
            spaceship = hit.GetComponentInParent<Spaceship>();

        if (spaceship != null)
        {
            DamageSpaceship(spaceship, collision.contacts[0].point);
            return;
        }
        SpaceshipPart part = hit.GetComponent<SpaceshipPart>();
        if (part == null)
            part = hit.GetComponentInParent<SpaceshipPart>();

        if (part != null)
        {
            part.OnDamage(collisionDamageToEachPart, "Planet");
            SpawnImpactEffect(collision.contacts[0].point);
        }
    }

    private void DamageSpaceship(Spaceship spaceship, Vector3 contactPoint)
    {
        foreach (SpaceshipPart part in spaceship.Parts)
        {
            if (part != null)
                part.OnDamage(collisionDamageToEachPart, "Planet");
        }
        SpaceshipPart[] allParts = spaceship.GetComponentsInChildren<SpaceshipPart>();
        foreach (SpaceshipPart part in allParts)
        {
            if (part != null && !spaceship.Parts.Contains(part))
                part.OnDamage(collisionDamageToEachPart * 0.5f, "Planet");
        }
        Connector[] connectors = spaceship.GetComponentsInChildren<Connector>();
        foreach (Connector connector in connectors)
        {
            foreach (SpaceshipPart part in spaceship.Parts)
            {
                if (part != null)
                    part.OnDamage(collisionDamageToEachPart * connectorDamageMultiplier, "Planet (connector shock)");
            }
        }

        SpawnImpactEffect(contactPoint);
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, position, Quaternion.identity);
            Destroy(effect, impactEffectDuration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x * 0.5f);
    }
}
