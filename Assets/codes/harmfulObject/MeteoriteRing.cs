using System.Collections.Generic;
using UnityEngine;

//Attach as a child of a Planet or use standalone
public class MeteoriteRing : MonoBehaviour
{
    [Header("Ring Layout")]
    [SerializeField] private float orbitRadius = 20f;
    [SerializeField] private int meteoriteCount = 12;
    [SerializeField] private float ringThickness = 3f;  
    [SerializeField] private float ringHeight = 2f;      
    [SerializeField] private Vector2 meteoriteScaleRange = new Vector2(0.5f, 1.5f);

    [Header("Orbit Motion")]
    [SerializeField] private float orbitSpeed = 15f;
    [SerializeField] private bool randomizeOrbitOffset = true;

    [Header("Meteorite Prefabs")]
    [SerializeField] private GameObject[] meteoritePrefabs;

    [Header("Optional Extra Ring")]
    [SerializeField] private bool addSecondRing = true;
    [SerializeField] private float secondRingRadius = 30f;
    [SerializeField] private int secondRingCount = 8;
    [SerializeField] private float secondRingOrbitSpeed = -10f; 

    private readonly List<Transform> ringPivots = new List<Transform>();

    private void Start()
    {
        if (meteoritePrefabs == null || meteoritePrefabs.Length == 0) return;

        SpawnRing(orbitRadius, meteoriteCount, orbitSpeed, 0f);

        if (addSecondRing)
        {
            SpawnRing(secondRingRadius, secondRingCount, secondRingOrbitSpeed, 30f);
        }
    }

    private void SpawnRing(float radius, int count, float speed, float tiltDeg)
    {
        if (count <= 0) return;
        GameObject pivotGO = new GameObject($"RingPivot_r{radius:F0}");
        pivotGO.transform.SetParent(transform);
        pivotGO.transform.localPosition = Vector3.zero;
        pivotGO.transform.localEulerAngles = new Vector3(tiltDeg, 0f, 0f);

        RingRotator rotator = pivotGO.AddComponent<RingRotator>();
        rotator.speed = speed;

        ringPivots.Add(pivotGO.transform);

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            if (randomizeOrbitOffset) angle += Random.Range(-10f, 10f);

            float r = radius + Random.Range(-ringThickness, ringThickness);
            float radians = angle * Mathf.Deg2Rad;
            Vector3 localPos = new Vector3(Mathf.Cos(radians) * r, Random.Range(-ringHeight, ringHeight), Mathf.Sin(radians) * r);

            GameObject prefab = meteoritePrefabs[Random.Range(0, meteoritePrefabs.Length)];
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, pivotGO.transform);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Random.rotation;

            float scale = Random.Range(meteoriteScaleRange.x, meteoriteScaleRange.y);
            instance.transform.localScale = Vector3.one * scale;
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Random.insideUnitSphere * 2f;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        DrawCircleGizmo(transform.position, orbitRadius);
        if (addSecondRing)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.4f);
            DrawCircleGizmo(transform.position, secondRingRadius);
        }
    }

    private static void DrawCircleGizmo(Vector3 center, float radius)
    {
        int seg = 32;
        for (int i = 0; i < seg; i++)
        {
            float a1 = (i / (float)seg) * Mathf.PI * 2f;
            float a2 = ((i + 1) / (float)seg) * Mathf.PI * 2f;
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * radius;
            Gizmos.DrawLine(p1, p2);
        }
    }
}

public class RingRotator : MonoBehaviour
{
    public float speed = 15f;

    private void Update()
    {
        transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
    }
}
