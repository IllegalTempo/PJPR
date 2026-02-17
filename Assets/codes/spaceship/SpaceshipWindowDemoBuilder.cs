using UnityEngine;

public class SpaceshipWindowDemoBuilder : MonoBehaviour
{
    [Header("Auto Build")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool skipIfShipExists = true;
    [SerializeField] private bool createWindowOnly = true;
    [SerializeField] private string shipRootName = "Spaceship_Demo";
    [SerializeField] private string windowOnlyRootName = "Window_Demo";
    [SerializeField] private bool spawnInFrontOfLocalPlayer = true;
    [SerializeField] private float spawnDistance = 6f;

    [Header("Transforms")]
    [SerializeField] private Vector3 shipPosition = new Vector3(0f, 1f, 6f);
    [SerializeField] private Vector3 shipScale = new Vector3(6f, 2f, 10f);
    [SerializeField] private Vector3 windowLocalPosition = new Vector3(0f, 0.2f, 5.1f);
    [SerializeField] private Vector3 windowScale = new Vector3(2.2f, 1.3f, 0.2f);
    [SerializeField] private Vector3 windowOnlyLocalPosition = Vector3.zero;

    [Header("Layer")]
    [SerializeField] private int selectableLayer = -1;
    [SerializeField] private Color bodyColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color windowColor = new Color(0.35f, 0.8f, 1f, 1f);

    private void Start()
    {
        if (!buildOnStart)
        {
            return;
        }

        string rootToCheck = createWindowOnly ? windowOnlyRootName : shipRootName;
        if (skipIfShipExists && GameObject.Find(rootToCheck) != null)
        {
            Debug.Log($"{rootToCheck} already exists. Disable 'Skip If Ship Exists' or use Clear Demo Objects to rebuild.");
            return;
        }

        BuildDemoShip();
    }

    [ContextMenu("Build Demo Ship")]
    public void BuildDemoShip()
    {
        if (createWindowOnly)
        {
            BuildWindowOnly();
            return;
        }

        if (skipIfShipExists)
        {
            GameObject existingRoot = GameObject.Find(shipRootName);
            if (existingRoot != null)
            {
                Debug.Log($"{shipRootName} already exists. Disable 'Skip If Ship Exists' or use Clear Demo Objects to rebuild.");
                return;
            }
        }

        GameObject root = new GameObject(shipRootName);
        root.transform.position = ResolveSpawnPosition();

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = shipScale;
        ApplyColor(body, bodyColor);

        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "Window";
        window.transform.SetParent(root.transform, false);
        window.transform.localPosition = windowLocalPosition;
        window.transform.localScale = windowScale;
        ApplyColor(window, windowColor);

        if (selectableLayer >= 0)
        {
            window.layer = selectableLayer;
        }

        ShipWindowPart windowScript = window.AddComponent<ShipWindowPart>();
        windowScript.OpenWindow();
        windowScript.CloseWindow();

        Debug.Log($"Demo ship created at {root.transform.position}. Window path: {root.name}/Window");
    }

    [ContextMenu("Build Window Only")]
    public void BuildWindowOnly()
    {
        if (skipIfShipExists)
        {
            GameObject existingRoot = GameObject.Find(windowOnlyRootName);
            if (existingRoot != null)
            {
                Debug.Log($"{windowOnlyRootName} already exists. Disable 'Skip If Ship Exists' or use Clear Demo Objects to rebuild.");
                return;
            }
        }

        GameObject root = new GameObject(windowOnlyRootName);
        root.transform.position = ResolveSpawnPosition();

        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "Window";
        window.transform.SetParent(root.transform, false);
        window.transform.localPosition = windowOnlyLocalPosition;
        window.transform.localScale = windowScale;
        ApplyColor(window, windowColor);

        if (selectableLayer >= 0)
        {
            window.layer = selectableLayer;
        }

        ShipWindowPart windowScript = window.AddComponent<ShipWindowPart>();
        windowScript.OpenWindow();
        windowScript.CloseWindow();

        Debug.Log($"Window-only demo created at {window.transform.position}. Path: {root.name}/Window");
    }

    [ContextMenu("Clear Demo Objects")]
    public void ClearDemoObjects()
    {
        GameObject ship = GameObject.Find(shipRootName);
        if (ship != null)
        {
            Destroy(ship);
            Debug.Log($"Destroyed {shipRootName}");
        }

        GameObject windowOnly = GameObject.Find(windowOnlyRootName);
        if (windowOnly != null)
        {
            Destroy(windowOnly);
            Debug.Log($"Destroyed {windowOnlyRootName}");
        }
    }

    private Vector3 ResolveSpawnPosition()
    {
        if (!spawnInFrontOfLocalPlayer)
        {
            return shipPosition;
        }

        Transform refTransform = null;
        if (GameCore.instance != null && GameCore.instance.localPlayer != null && GameCore.instance.localPlayer.cam != null)
        {
            refTransform = GameCore.instance.localPlayer.cam.transform;
        }

        if (refTransform == null && Camera.main != null)
        {
            refTransform = Camera.main.transform;
        }

        if (refTransform == null)
        {
            return shipPosition;
        }

        Vector3 forward = refTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 pos = refTransform.position + forward * Mathf.Max(1f, spawnDistance);
        pos.y = Mathf.Max(1f, refTransform.position.y);
        return pos;
    }

    private static void ApplyColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = new Material(renderer.sharedMaterial);
        material.color = color;
        renderer.material = material;
    }
}
