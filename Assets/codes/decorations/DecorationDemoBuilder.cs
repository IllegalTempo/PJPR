using UnityEngine;

public class DecorationDemoBuilder : MonoBehaviour 
//This is temp code to build decorations for test without actually spaceship created, AIAIAI
{
    [SerializeField] private bool buildOnStart;
    [SerializeField] private bool skipIfExists = true;
    [SerializeField] private string rootName = "Decorations_Demo";
    [SerializeField] private int selectableLayer = -1;

    private void Start()
    {
        if (buildOnStart)
        {
            BuildDecorations();
        }
    }

    [ContextMenu("Build Decorations")]
    public void BuildDecorations()
    {
        if (skipIfExists && GameObject.Find(rootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(rootName);
        root.transform.position = ResolveSpawnPosition();

        BuildWindow(root.transform);
        BuildLight(root.transform);
    }

    [ContextMenu("Clear Decorations")]
    public void ClearDecorations()
    {
        GameObject root = GameObject.Find(rootName);
        if (root != null)
        {
            Destroy(root);
        }
    }

    private void BuildWindow(Transform root)
    {
        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "Decoration_Window";
        window.transform.SetParent(root, false);
        window.transform.localPosition = new Vector3(-1.4f, 0.5f, 0f);
        window.transform.localScale = new Vector3(1.8f, 1.2f, 0.15f);

        int layerToUse = GetSelectableLayerToUse();
        if (layerToUse >= 0)
        {
            window.layer = layerToUse;
        }

        window.AddComponent<SpaceshipPart>();
        window.AddComponent<ShipWindowPart>();
    }

    private void BuildLight(Transform root)
    {
        GameObject lightRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightRoot.name = "Decoration_Light";
        lightRoot.transform.SetParent(root, false);
        lightRoot.transform.localPosition = new Vector3(1.4f, 1.4f, 0f);
        lightRoot.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

        int layerToUse = GetSelectableLayerToUse();
        if (layerToUse >= 0)
        {
            lightRoot.layer = layerToUse;
        }

        Light sceneLight = lightRoot.AddComponent<Light>();
        sceneLight.type = LightType.Point;
        sceneLight.range = 8f;
        sceneLight.intensity = 2f;

        lightRoot.AddComponent<DecorationLight>();
    }

    private int GetSelectableLayerToUse()
    {
        if (selectableLayer >= 0)
        {
            return selectableLayer;
        }

        if (GameCore.instance == null || GameCore.instance.Masks == null)
        {
            return -1;
        }

        int mask = GameCore.instance.Masks.SelectableItems.value;
        if (mask == 0)
        {
            return -1;
        }

        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask & (1 << layer)) != 0)
            {
                return layer;
            }
        }

        return -1;
    }

    private Vector3 ResolveSpawnPosition()
    {
        if (GameCore.instance != null && GameCore.instance.localPlayer != null && GameCore.instance.localPlayer.cam != null)
        {
            Transform cam = GameCore.instance.localPlayer.cam.transform;
            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 pos = cam.position + (forward * 5f);
            pos.y = Mathf.Max(1f, cam.position.y - 0.7f);
            return pos;
        }

        return new Vector3(0f, 1f, 5f);
    }
}