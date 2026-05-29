using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RotateSelectionToFaceTool
{
    private const string MenuPath = "Tools/Scene Tools/Rotate Selection To Face Under Pointer";
    private const string ShortcutName = "Scene View/Rotate Selection To Face Under Pointer";
    private const string UndoName = "Rotate Selection To Face";
    private const float MaxRayDistance = 10000f;
    private const float RayEpsilon = 0.000001f;
    private static readonly Quaternion ZOffset = Quaternion.Euler(0f, 0f, -90f);

    private static SceneView _lastSceneView;
    private static Vector2 _lastMousePosition;
    private static bool _hasMousePosition;

    static RotateSelectionToFaceTool()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem(MenuPath)]
    public static void RotateSelectionFromMenu()
    {
        SceneView sceneView = _lastSceneView != null ? _lastSceneView : SceneView.lastActiveSceneView;
        if (sceneView == null || !_hasMousePosition)
        {
            Debug.LogWarning("Rotate Selection To Face: Move the pointer over the Scene view first.");
            return;
        }

        RotateSelection(sceneView, _lastMousePosition);
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateRotateSelectionFromMenu()
    {
        return Selection.transforms.Length > 0;
    }

    [Shortcut(ShortcutName, typeof(SceneView), KeyCode.F, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
    private static void RotateSelectionShortcut(ShortcutArguments arguments)
    {
        SceneView sceneView = arguments.context as SceneView;
        if (sceneView == null)
        {
            sceneView = _lastSceneView != null ? _lastSceneView : SceneView.lastActiveSceneView;
        }

        if (sceneView == null || !_hasMousePosition)
        {
            Debug.LogWarning("Rotate Selection To Face: Move the pointer over the Scene view first.");
            return;
        }

        RotateSelection(sceneView, _lastMousePosition);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        if (currentEvent.isMouse || currentEvent.type == EventType.Repaint || currentEvent.type == EventType.Layout)
        {
            _lastSceneView = sceneView;
            _lastMousePosition = currentEvent.mousePosition;
            _hasMousePosition = true;
        }
    }

    private static void RotateSelection(SceneView sceneView, Vector2 mousePosition)
    {
        Transform[] selectedTransforms = Selection.transforms;
        if (selectedTransforms.Length == 0)
        {
            Debug.LogWarning("Rotate Selection To Face: Select at least one GameObject first.");
            return;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        SurfaceHit hit;
        if (!TryFindClosestMeshFace(ray, out hit))
        {
            Debug.LogWarning("Rotate Selection To Face: No visible mesh face found under the pointer.");
            return;
        }

        Undo.RecordObjects(selectedTransforms, UndoName);

        foreach (Transform selectedTransform in selectedTransforms)
        {
            selectedTransform.rotation = GetRotationAlignedToNormal(selectedTransform, hit.Normal);
            EditorUtility.SetDirty(selectedTransform);
        }

        sceneView.Repaint();
    }

    private static Quaternion GetRotationAlignedToNormal(Transform target, Vector3 normal)
    {
        Transform parent = target.parent;
        Vector3 localNormal = parent != null ? parent.InverseTransformDirection(normal) : normal;
        Vector3 flattenedLocalNormal = new Vector3(localNormal.x, 0f, localNormal.z);

        if (flattenedLocalNormal.sqrMagnitude > RayEpsilon)
        {
            float yaw = Mathf.Atan2(flattenedLocalNormal.x, flattenedLocalNormal.z) * Mathf.Rad2Deg;
            Quaternion localRotation = Quaternion.Euler(0f, yaw, 0f) * ZOffset;
            return parent != null ? parent.rotation * localRotation : localRotation;
        }

        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal);
        if (up.sqrMagnitude < RayEpsilon)
        {
            up = Vector3.ProjectOnPlane(target.up, normal);
        }

        if (up.sqrMagnitude < RayEpsilon)
        {
            up = Vector3.ProjectOnPlane(target.right, normal);
        }

        if (up.sqrMagnitude < RayEpsilon)
        {
            up = Vector3.Cross(normal, Vector3.right);
        }

        if (up.sqrMagnitude < RayEpsilon)
        {
            up = Vector3.Cross(normal, Vector3.forward);
        }

        return Quaternion.LookRotation(normal, up.normalized) * ZOffset;
    }

    private static bool TryFindClosestMeshFace(Ray ray, out SurfaceHit closestHit)
    {
        closestHit = new SurfaceHit();
        bool hasHit = false;
        float closestDistance = MaxRayDistance;
        MeshFilter[] meshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (mesh == null || meshRenderer == null || !meshRenderer.enabled || !meshFilter.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!meshRenderer.bounds.IntersectRay(ray))
            {
                continue;
            }

            SurfaceHit meshHit;
            if (TryRaycastMesh(ray, meshFilter.transform.localToWorldMatrix, mesh, closestDistance, out meshHit))
            {
                closestDistance = meshHit.Distance;
                closestHit = meshHit;
                hasHit = true;
            }
        }

        return hasHit;
    }

    private static bool TryRaycastMesh(Ray ray, Matrix4x4 localToWorld, Mesh mesh, float maxDistance, out SurfaceHit closestHit)
    {
        closestHit = new SurfaceHit();
        bool hasHit = false;
        float closestDistance = maxDistance;

        Vector3[] vertices;
        int[] triangles;
        try
        {
            vertices = mesh.vertices;
            triangles = mesh.triangles;
        }
        catch (UnityException)
        {
            return false;
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = localToWorld.MultiplyPoint3x4(vertices[triangles[i]]);
            Vector3 b = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 1]]);
            Vector3 c = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 2]]);

            float distance;
            if (!RayIntersectsTriangle(ray, a, b, c, out distance) || distance >= closestDistance)
            {
                continue;
            }

            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (normal.sqrMagnitude < RayEpsilon)
            {
                continue;
            }

            if (Vector3.Dot(normal, ray.direction) > 0f)
            {
                normal = -normal;
            }

            closestDistance = distance;
            closestHit = new SurfaceHit(ray.GetPoint(distance), normal, distance);
            hasHit = true;
        }

        return hasHit;
    }

    private static bool RayIntersectsTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance)
    {
        distance = 0f;

        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;
        Vector3 h = Vector3.Cross(ray.direction, edge2);
        float determinant = Vector3.Dot(edge1, h);

        if (determinant > -RayEpsilon && determinant < RayEpsilon)
        {
            return false;
        }

        float inverseDeterminant = 1f / determinant;
        Vector3 s = ray.origin - a;
        float u = inverseDeterminant * Vector3.Dot(s, h);
        if (u < 0f || u > 1f)
        {
            return false;
        }

        Vector3 q = Vector3.Cross(s, edge1);
        float v = inverseDeterminant * Vector3.Dot(ray.direction, q);
        if (v < 0f || u + v > 1f)
        {
            return false;
        }

        distance = inverseDeterminant * Vector3.Dot(edge2, q);
        return distance > RayEpsilon && distance < MaxRayDistance;
    }

    private struct SurfaceHit
    {
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly float Distance;

        public SurfaceHit(Vector3 point, Vector3 normal, float distance)
        {
            Point = point;
            Normal = normal;
            Distance = distance;
        }
    }
}
