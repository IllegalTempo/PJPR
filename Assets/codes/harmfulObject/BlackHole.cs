using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//testing
public class BlackHole : HarmfulObject
{
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private Vector3 movementDirection;
    
    [Header("Gravity Well")]
    [SerializeField] private float attractionRadius = 40f;
    [SerializeField] private float attractionForce = 20f;
    [SerializeField] private AnimationCurve forceByDistance = AnimationCurve.Linear(0, 2, 1, 0.1f);
    [SerializeField] private LayerMask attractedLayers = Physics.AllLayers;

    [Header("Event Horizon")]
    [SerializeField] private float eventHorizonRadius = 6f;
    [SerializeField] private float damageRateInsideHorizon = 30f; 
    [SerializeField] private float vanishScaleSpeed = 3f; 
    [SerializeField] private float vanishDestroyDelay = 1.5f;

    [Header("Visual Spin")]
    [SerializeField] private Transform diskTransform; 
    [SerializeField] private float diskSpinSpeed = 60f;

    [Header("Effects")]
    [SerializeField] private GameObject swallowEffect;

    private readonly HashSet<GameObject> swallowing = new HashSet<GameObject>();
    private bool IsServerAuthority => NetworkSystem.Instance == null || NetworkSystem.Instance.IsServer;

    private void Awake()
    {
        SetHarmfulObjectType(HarmfulObjectType.BlackHole);
    }

    public void InitializeMovement(Vector3 direction, float speed)
    {
        movementDirection = direction;
        movementSpeed = speed;
    }

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null) netObj.Sync_Transform = true;
    }

    private void FixedUpdate()
    {
        // Only server drives physics
        if (!IsServerAuthority) return;

        // Move the black hole
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && movementDirection.magnitude > 0)
        {
            rb.MovePosition(rb.position + movementDirection.normalized * movementSpeed * Time.fixedDeltaTime);
        }

        ApplyGravity();
    }

    private void Update()
    {
        if (diskTransform != null)
        {
            diskTransform.Rotate(Vector3.up, diskSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void ApplyGravity()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, attractionRadius, attractedLayers);

        foreach (Collider col in nearby)
        {
            if (col.gameObject == gameObject) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb == null || rb.isKinematic) continue;

            Vector3 toBlackHole = transform.position - col.transform.position;
            float distance = toBlackHole.magnitude;

            if (distance < 0.01f) continue;

            float t = 1f - Mathf.Clamp01(distance / attractionRadius);
            float forceMagnitude = attractionForce * forceByDistance.Evaluate(t);
            rb.AddForce(toBlackHole.normalized * forceMagnitude, ForceMode.Acceleration);
            if (distance <= eventHorizonRadius && !swallowing.Contains(col.gameObject))
            {
                swallowing.Add(col.gameObject);
                StartCoroutine(SwallowObject(col.gameObject));
            }
        }
    }

    private IEnumerator SwallowObject(GameObject obj)
    {
        if (obj == null) yield break;

        TryDamageSpaceship(obj);

        if (swallowEffect != null)
        {
            GameObject fx = Instantiate(swallowEffect, obj.transform.position, Quaternion.identity);
            Destroy(fx, vanishDestroyDelay + 1f);
        }

        float elapsed = 0f;
        Vector3 originalScale = obj.transform.localScale;

        Collider[] cols = obj.GetComponentsInChildren<Collider>();
        foreach (Collider c in cols) c.enabled = false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        while (elapsed < vanishDestroyDelay && obj != null)
        {
            float t = elapsed / vanishDestroyDelay;
            if (obj != null)
                obj.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (obj != null)
        {
            swallowing.Remove(obj);
            Destroy(obj);
        }
    }

    private void TryDamageSpaceship(GameObject obj)
    {
        //Spaceship spaceship = obj.GetComponent<Spaceship>() ?? obj.GetComponentInParent<Spaceship>();
        //if (spaceship != null)
        //{
        //    foreach (SpaceshipPart part in spaceship.Parts)
        //    {
        //        if (part != null)
        //            StartCoroutine(DamageOverTime(part, damageRateInsideHorizon, vanishDestroyDelay));
        //    }

        //    SpaceshipPart[] allParts = spaceship.GetComponentsInChildren<SpaceshipPart>();
        //    foreach (SpaceshipPart part in allParts)
        //    {
        //        if (part != null && !spaceship.Parts.Contains(part))
        //            StartCoroutine(DamageOverTime(part, damageRateInsideHorizon * 0.5f, vanishDestroyDelay));
        //    }
        //    return;
        //}

        //SpaceshipPart singlePart = obj.GetComponent<SpaceshipPart>() ?? obj.GetComponentInParent<SpaceshipPart>();
        //if (singlePart != null)
        //    StartCoroutine(DamageOverTime(singlePart, damageRateInsideHorizon, vanishDestroyDelay));
    }

    private IEnumerator DamageOverTime(SpaceshipPart part, float damagePerSecond, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && part != null)
        {
            part.OnDamage(damagePerSecond * Time.deltaTime, "BlackHole");
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, eventHorizonRadius);
    }
}
