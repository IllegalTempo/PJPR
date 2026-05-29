using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BoxColliderGeneratorWindow : EditorWindow
{
    private const string GeneratedContainerName = "__GeneratedBoxColliders";

    private bool _includeChildren = true;
    private bool _includeInactive;
    private bool _replaceGenerated = true;
    private bool _createVisibleBoxes;
    private bool _preferFewerBoxes = true;
    private RotationMode _rotationMode = RotationMode.AutoFit;
    private int _resolution = 18;
    private int _rotationSamples = 12;

    private enum RotationMode
    {
        TargetAxes,
        MeshLocal,
        AutoFit
    }

    [MenuItem("Tools/Colliders/Box Collider Generator")]
    public static void ShowWindow()
    {
        BoxColliderGeneratorWindow window = GetWindow<BoxColliderGeneratorWindow>("Box Collider Generator");
        window.minSize = new Vector2(420, 260);
    }

    [MenuItem("Tools/Colliders/Generate Box Colliders For Selected Mesh")]
    public static void GenerateSelectedFromMenu()
    {
        GenerateForSelection(true, false, true, false, RotationMode.AutoFit, 18, 12, true);
    }

    [MenuItem("Tools/Colliders/Generate Box Colliders For Selected Mesh", true)]
    private static bool ValidateGenerateSelectedFromMenu()
    {
        return Selection.gameObjects.Length > 0;
    }

    private void OnGUI()
    {
        GUILayout.Label("Box Collider Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scans selected meshes into a voxel grid, merges occupied cells into boxes, and creates generated child BoxColliders.",
            MessageType.Info);

        _resolution = EditorGUILayout.IntSlider("Resolution", _resolution, 4, 48);
        _rotationMode = (RotationMode)EditorGUILayout.EnumPopup("Rotation Mode", _rotationMode);

        using (new EditorGUI.DisabledScope(_rotationMode != RotationMode.AutoFit))
        {
            _rotationSamples = EditorGUILayout.IntSlider("Rotation Samples", _rotationSamples, 4, 36);
            _preferFewerBoxes = EditorGUILayout.ToggleLeft("Prefer fewer boxes", _preferFewerBoxes);
        }

        _includeChildren = EditorGUILayout.ToggleLeft("Include child meshes", _includeChildren);
        _includeInactive = EditorGUILayout.ToggleLeft("Include inactive child objects", _includeInactive);
        _replaceGenerated = EditorGUILayout.ToggleLeft("Replace previous generated boxes", _replaceGenerated);
        _createVisibleBoxes = EditorGUILayout.ToggleLeft("Create visible box meshes", _createVisibleBoxes);

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Generate For Selection", GUILayout.Height(30)))
            {
                GenerateForSelection(
                    _includeChildren,
                    _includeInactive,
                    _replaceGenerated,
                    _createVisibleBoxes,
                    _rotationMode,
                    _resolution,
                    _rotationSamples,
                    _preferFewerBoxes);
            }
        }

        if (GUILayout.Button("Clear Generated Boxes From Selection", GUILayout.Height(24)))
        {
            ClearGeneratedBoxesFromSelection();
        }
    }

    private static void GenerateForSelection(
        bool includeChildren,
        bool includeInactive,
        bool replaceGenerated,
        bool createVisibleBoxes,
        RotationMode rotationMode,
        int resolution,
        int rotationSamples,
        bool preferFewerBoxes)
    {
        int changedCount = 0;
        int colliderCount = 0;

        try
        {
            foreach (GameObject selectedObject in Selection.gameObjects)
            {
                int generatedCount;
                if (GenerateForObject(
                    selectedObject,
                    includeChildren,
                    includeInactive,
                    replaceGenerated,
                    createVisibleBoxes,
                    rotationMode,
                    resolution,
                    rotationSamples,
                    preferFewerBoxes,
                    out generatedCount))
                {
                    changedCount++;
                    colliderCount += generatedCount;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (changedCount == 0)
        {
            Debug.LogWarning("Box Collider Generator: No usable meshes found on the selected GameObject(s).");
            return;
        }

        Debug.Log("Box Collider Generator: Updated " + changedCount + " GameObject(s), generated " + colliderCount + " BoxCollider(s).");
    }

    private static bool GenerateForObject(
        GameObject target,
        bool includeChildren,
        bool includeInactive,
        bool replaceGenerated,
        bool createVisibleBoxes,
        RotationMode rotationMode,
        int resolution,
        int rotationSamples,
        bool preferFewerBoxes,
        out int generatedCount)
    {
        generatedCount = 0;

        List<SourceMeshData> sourceMeshes = CollectMeshes(target, includeChildren, includeInactive);
        if (sourceMeshes.Count == 0)
        {
            return false;
        }

        Transform existingContainer = target.transform.Find(GeneratedContainerName);
        if (existingContainer != null && replaceGenerated)
        {
            Undo.DestroyObjectImmediate(existingContainer.gameObject);
        }

        GameObject container = new GameObject(GeneratedContainerName);
        Undo.RegisterCreatedObjectUndo(container, "Create Generated Box Colliders");
        container.transform.SetParent(target.transform, false);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        if (rotationMode == RotationMode.TargetAxes)
        {
            ScanResult result = GenerateForFrame(sourceMeshes, ScanFrame.Identity, resolution, target.name);
            CreateBoxes(container.transform, result, createVisibleBoxes, ref generatedCount);
        }
        else if (rotationMode == RotationMode.MeshLocal)
        {
            foreach (SourceMeshData sourceMesh in sourceMeshes)
            {
                ScanFrame frame = new ScanFrame(sourceMesh.RotationInTarget);
                ScanResult result = GenerateForFrame(sourceMesh, frame, resolution, target.name + "/" + sourceMesh.Name);
                CreateBoxes(container.transform, result, createVisibleBoxes, ref generatedCount);
            }
        }
        else
        {
            foreach (SourceMeshData sourceMesh in sourceMeshes)
            {
                ScanResult result = GenerateBestFit(sourceMesh, resolution, rotationSamples, preferFewerBoxes, target.name);
                CreateBoxes(container.transform, result, createVisibleBoxes, ref generatedCount);
            }
        }

        if (generatedCount == 0)
        {
            Undo.DestroyObjectImmediate(container);
            return false;
        }

        EditorUtility.SetDirty(target);
        return true;
    }

    private static List<SourceMeshData> CollectMeshes(GameObject target, bool includeChildren, bool includeInactive)
    {
        List<SourceMeshData> meshes = new List<SourceMeshData>();

        if (includeChildren)
        {
            MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>(includeInactive);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                AddMesh(target.transform, meshFilter.transform, meshFilter.sharedMesh, meshes);
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                AddMesh(target.transform, skinnedMeshRenderer.transform, skinnedMeshRenderer.sharedMesh, meshes);
            }
        }
        else
        {
            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                AddMesh(target.transform, meshFilter.transform, meshFilter.sharedMesh, meshes);
            }

            SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                AddMesh(target.transform, skinnedMeshRenderer.transform, skinnedMeshRenderer.sharedMesh, meshes);
            }
        }

        return meshes;
    }

    private static void AddMesh(Transform targetTransform, Transform meshTransform, Mesh mesh, List<SourceMeshData> meshes)
    {
        if (mesh == null || mesh.vertexCount == 0 || mesh.triangles == null || mesh.triangles.Length == 0)
        {
            return;
        }

        Matrix4x4 meshToTarget = targetTransform.worldToLocalMatrix * meshTransform.localToWorldMatrix;
        Vector3[] sourceVertices = mesh.vertices;
        Vector3[] targetVertices = new Vector3[sourceVertices.Length];

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            targetVertices[i] = meshToTarget.MultiplyPoint3x4(sourceVertices[i]);
        }

        Quaternion rotationInTarget = Quaternion.Inverse(targetTransform.rotation) * meshTransform.rotation;
        meshes.Add(new SourceMeshData(meshTransform.name, targetVertices, mesh.triangles, rotationInTarget));
    }

    private static ScanResult GenerateBestFit(SourceMeshData sourceMesh, int resolution, int rotationSamples, bool preferFewerBoxes, string targetName)
    {
        List<ScanFrame> candidates = GenerateCandidateFrames(sourceMesh, rotationSamples);
        ScanResult bestResult = ScanResult.Empty;
        ScanScore bestScore = ScanScore.Worst;

        for (int i = 0; i < candidates.Count; i++)
        {
            string progressName = targetName + "/" + sourceMesh.Name + " candidate " + (i + 1) + "/" + candidates.Count;
            ScanResult result = GenerateForFrame(sourceMesh, candidates[i], resolution, progressName);
            ScanScore score = new ScanScore(result.Boxes.Count, result.TotalBoxVolume, result.BoundsVolume);

            if (score.IsBetterThan(bestScore, preferFewerBoxes))
            {
                bestScore = score;
                bestResult = result;
            }
        }

        return bestResult;
    }

    private static List<ScanFrame> GenerateCandidateFrames(SourceMeshData sourceMesh, int rotationSamples)
    {
        List<Quaternion> rotations = new List<Quaternion>();
        AddUniqueRotation(rotations, Quaternion.identity);
        AddUniqueRotation(rotations, sourceMesh.RotationInTarget);

        PcaFrame pcaFrame = CalculatePcaFrame(sourceMesh.Vertices);
        AddUniqueRotation(rotations, pcaFrame.Rotation);

        int sampleCount = Mathf.Max(4, rotationSamples);
        for (int i = 0; i < sampleCount; i++)
        {
            float angle = 180f * i / sampleCount;
            AddUniqueRotation(rotations, Quaternion.AngleAxis(angle, pcaFrame.AxisX) * pcaFrame.Rotation);
            AddUniqueRotation(rotations, Quaternion.AngleAxis(angle, pcaFrame.AxisY) * pcaFrame.Rotation);
            AddUniqueRotation(rotations, Quaternion.AngleAxis(angle, pcaFrame.AxisZ) * pcaFrame.Rotation);
        }

        List<ScanFrame> frames = new List<ScanFrame>();
        foreach (Quaternion rotation in rotations)
        {
            frames.Add(new ScanFrame(rotation));
        }

        return frames;
    }

    private static void AddUniqueRotation(List<Quaternion> rotations, Quaternion rotation)
    {
        rotation = NormalizeQuaternion(rotation);

        foreach (Quaternion existing in rotations)
        {
            if (Mathf.Abs(Quaternion.Dot(existing, rotation)) > 0.9995f)
            {
                return;
            }
        }

        rotations.Add(rotation);
    }

    private static ScanResult GenerateForFrame(SourceMeshData sourceMesh, ScanFrame frame, int resolution, string targetName)
    {
        List<SourceMeshData> meshes = new List<SourceMeshData> { sourceMesh };
        return GenerateForFrame(meshes, frame, resolution, targetName);
    }

    private static ScanResult GenerateForFrame(List<SourceMeshData> sourceMeshes, ScanFrame frame, int resolution, string targetName)
    {
        List<ScanMeshData> scanMeshes = new List<ScanMeshData>();

        foreach (SourceMeshData sourceMesh in sourceMeshes)
        {
            scanMeshes.Add(CreateScanMesh(sourceMesh, frame));
        }

        Bounds scanBounds = CalculateBounds(scanMeshes);
        if (scanBounds.size == Vector3.zero)
        {
            return ScanResult.Empty;
        }

        bool[,,] occupiedCells = ScanOccupiedCells(scanMeshes, scanBounds, resolution, targetName);
        List<BoxBounds> boxes = MergeOccupiedCells(occupiedCells, scanBounds);
        return new ScanResult(frame, boxes, CalculateTotalBoxVolume(boxes), GetVolume(scanBounds));
    }

    private static ScanMeshData CreateScanMesh(SourceMeshData sourceMesh, ScanFrame frame)
    {
        Vector3[] scanVertices = new Vector3[sourceMesh.Vertices.Length];

        for (int i = 0; i < sourceMesh.Vertices.Length; i++)
        {
            scanVertices[i] = frame.TargetToScan.MultiplyPoint3x4(sourceMesh.Vertices[i]);
        }

        return new ScanMeshData(scanVertices, sourceMesh.Triangles);
    }

    private static Bounds CalculateBounds(List<ScanMeshData> meshes)
    {
        Bounds bounds = new Bounds(meshes[0].Vertices[0], Vector3.zero);

        foreach (ScanMeshData mesh in meshes)
        {
            foreach (Vector3 vertex in mesh.Vertices)
            {
                bounds.Encapsulate(vertex);
            }
        }

        Vector3 padding = bounds.size * 0.001f;
        padding.x = Mathf.Max(padding.x, 0.0001f);
        padding.y = Mathf.Max(padding.y, 0.0001f);
        padding.z = Mathf.Max(padding.z, 0.0001f);
        bounds.Expand(padding);

        return bounds;
    }

    private static bool[,,] ScanOccupiedCells(List<ScanMeshData> meshes, Bounds bounds, int resolution, string targetName)
    {
        Vector3Int cellCounts = GetCellCounts(bounds.size, resolution);
        bool[,,] occupied = new bool[cellCounts.x, cellCounts.y, cellCounts.z];
        Vector3 cellSize = new Vector3(
            bounds.size.x / cellCounts.x,
            bounds.size.y / cellCounts.y,
            bounds.size.z / cellCounts.z);

        MarkSurfaceCells(meshes, bounds, cellCounts, cellSize, occupied);

        int totalCells = cellCounts.x * cellCounts.y * cellCounts.z;
        int checkedCells = 0;

        for (int z = 0; z < cellCounts.z; z++)
        {
            for (int y = 0; y < cellCounts.y; y++)
            {
                for (int x = 0; x < cellCounts.x; x++)
                {
                    if (!occupied[x, y, z])
                    {
                        Vector3 point = GetCellCenter(bounds, cellSize, x, y, z);
                        occupied[x, y, z] = IsPointInsideMeshes(point, meshes);
                    }

                    checkedCells++;
                }
            }

            EditorUtility.DisplayProgressBar(
                "Generating Box Colliders",
                "Scanning " + targetName + " at " + resolution + " resolution...",
                checkedCells / (float)totalCells);
        }

        return occupied;
    }

    private static Vector3Int GetCellCounts(Vector3 size, int resolution)
    {
        float longestAxis = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        return new Vector3Int(
            Mathf.Max(1, Mathf.CeilToInt(size.x / longestAxis * resolution)),
            Mathf.Max(1, Mathf.CeilToInt(size.y / longestAxis * resolution)),
            Mathf.Max(1, Mathf.CeilToInt(size.z / longestAxis * resolution)));
    }

    private static void MarkSurfaceCells(
        List<ScanMeshData> meshes,
        Bounds bounds,
        Vector3Int cellCounts,
        Vector3 cellSize,
        bool[,,] occupied)
    {
        float surfaceDistance = cellSize.magnitude * 0.5f;
        float surfaceDistanceSqr = surfaceDistance * surfaceDistance;

        foreach (ScanMeshData mesh in meshes)
        {
            int[] triangles = mesh.Triangles;
            Vector3[] vertices = mesh.Vertices;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];

                Bounds triangleBounds = new Bounds(a, Vector3.zero);
                triangleBounds.Encapsulate(b);
                triangleBounds.Encapsulate(c);
                triangleBounds.Expand(cellSize);

                Vector3Int min = WorldToCell(bounds, cellSize, cellCounts, triangleBounds.min);
                Vector3Int max = WorldToCell(bounds, cellSize, cellCounts, triangleBounds.max);

                for (int z = min.z; z <= max.z; z++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int x = min.x; x <= max.x; x++)
                        {
                            Vector3 point = GetCellCenter(bounds, cellSize, x, y, z);
                            if (SqrDistancePointTriangle(point, a, b, c) <= surfaceDistanceSqr)
                            {
                                occupied[x, y, z] = true;
                            }
                        }
                    }
                }
            }
        }
    }

    private static Vector3Int WorldToCell(Bounds bounds, Vector3 cellSize, Vector3Int cellCounts, Vector3 point)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((point.x - bounds.min.x) / cellSize.x), 0, cellCounts.x - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((point.y - bounds.min.y) / cellSize.y), 0, cellCounts.y - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt((point.z - bounds.min.z) / cellSize.z), 0, cellCounts.z - 1);

        return new Vector3Int(x, y, z);
    }

    private static Vector3 GetCellCenter(Bounds bounds, Vector3 cellSize, int x, int y, int z)
    {
        return new Vector3(
            bounds.min.x + (x + 0.5f) * cellSize.x,
            bounds.min.y + (y + 0.5f) * cellSize.y,
            bounds.min.z + (z + 0.5f) * cellSize.z);
    }

    private static bool IsPointInsideMeshes(Vector3 point, List<ScanMeshData> meshes)
    {
        int intersections = 0;
        Vector3 rayDirection = Vector3.right;

        foreach (ScanMeshData mesh in meshes)
        {
            Vector3[] vertices = mesh.Vertices;
            int[] triangles = mesh.Triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (RayIntersectsTriangle(point, rayDirection, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]))
                {
                    intersections++;
                }
            }
        }

        return (intersections & 1) == 1;
    }

    private static bool RayIntersectsTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c)
    {
        const float epsilon = 0.000001f;

        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;
        Vector3 h = Vector3.Cross(direction, edge2);
        float determinant = Vector3.Dot(edge1, h);

        if (determinant > -epsilon && determinant < epsilon)
        {
            return false;
        }

        float inverseDeterminant = 1f / determinant;
        Vector3 s = origin - a;
        float u = inverseDeterminant * Vector3.Dot(s, h);
        if (u < 0f || u > 1f)
        {
            return false;
        }

        Vector3 q = Vector3.Cross(s, edge1);
        float v = inverseDeterminant * Vector3.Dot(direction, q);
        if (v < 0f || u + v > 1f)
        {
            return false;
        }

        float distance = inverseDeterminant * Vector3.Dot(edge2, q);
        return distance > epsilon;
    }

    private static List<BoxBounds> MergeOccupiedCells(bool[,,] occupied, Bounds bounds)
    {
        int countX = occupied.GetLength(0);
        int countY = occupied.GetLength(1);
        int countZ = occupied.GetLength(2);
        bool[,,] used = new bool[countX, countY, countZ];
        Vector3 cellSize = new Vector3(bounds.size.x / countX, bounds.size.y / countY, bounds.size.z / countZ);
        List<BoxBounds> boxes = new List<BoxBounds>();

        for (int z = 0; z < countZ; z++)
        {
            for (int y = 0; y < countY; y++)
            {
                for (int x = 0; x < countX; x++)
                {
                    if (!occupied[x, y, z] || used[x, y, z])
                    {
                        continue;
                    }

                    Vector3Int start = new Vector3Int(x, y, z);
                    Vector3Int size = FindLargestFreeBlock(start, occupied, used);
                    MarkBlockUsed(start, size, used);
                    boxes.Add(CreateBoxBounds(bounds, cellSize, start, size));
                }
            }
        }

        return boxes;
    }

    private static Vector3Int FindLargestFreeBlock(Vector3Int start, bool[,,] occupied, bool[,,] used)
    {
        int countX = occupied.GetLength(0);
        int countY = occupied.GetLength(1);
        int countZ = occupied.GetLength(2);

        int sizeX = 1;
        while (start.x + sizeX < countX && IsBlockAvailable(start, new Vector3Int(sizeX + 1, 1, 1), occupied, used))
        {
            sizeX++;
        }

        int sizeY = 1;
        while (start.y + sizeY < countY && IsBlockAvailable(start, new Vector3Int(sizeX, sizeY + 1, 1), occupied, used))
        {
            sizeY++;
        }

        int sizeZ = 1;
        while (start.z + sizeZ < countZ && IsBlockAvailable(start, new Vector3Int(sizeX, sizeY, sizeZ + 1), occupied, used))
        {
            sizeZ++;
        }

        return new Vector3Int(sizeX, sizeY, sizeZ);
    }

    private static bool IsBlockAvailable(Vector3Int start, Vector3Int size, bool[,,] occupied, bool[,,] used)
    {
        for (int z = start.z; z < start.z + size.z; z++)
        {
            for (int y = start.y; y < start.y + size.y; y++)
            {
                for (int x = start.x; x < start.x + size.x; x++)
                {
                    if (!occupied[x, y, z] || used[x, y, z])
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static void MarkBlockUsed(Vector3Int start, Vector3Int size, bool[,,] used)
    {
        for (int z = start.z; z < start.z + size.z; z++)
        {
            for (int y = start.y; y < start.y + size.y; y++)
            {
                for (int x = start.x; x < start.x + size.x; x++)
                {
                    used[x, y, z] = true;
                }
            }
        }
    }

    private static BoxBounds CreateBoxBounds(Bounds bounds, Vector3 cellSize, Vector3Int start, Vector3Int size)
    {
        Vector3 min = new Vector3(
            bounds.min.x + start.x * cellSize.x,
            bounds.min.y + start.y * cellSize.y,
            bounds.min.z + start.z * cellSize.z);

        Vector3 boxSize = new Vector3(size.x * cellSize.x, size.y * cellSize.y, size.z * cellSize.z);
        return new BoxBounds(min + boxSize * 0.5f, boxSize);
    }

    private static void CreateBoxes(Transform parent, ScanResult result, bool createVisibleBox, ref int generatedCount)
    {
        foreach (BoxBounds bounds in result.Boxes)
        {
            CreateBox(parent, result.Frame, bounds, createVisibleBox, generatedCount);
            generatedCount++;
        }
    }

    private static void CreateBox(Transform parent, ScanFrame frame, BoxBounds bounds, bool createVisibleBox, int index)
    {
        GameObject boxObject;

        if (createVisibleBox)
        {
            boxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(boxObject.GetComponent<BoxCollider>());
        }
        else
        {
            boxObject = new GameObject();
        }

        boxObject.name = "BoxCollider_" + index.ToString("000");
        Undo.RegisterCreatedObjectUndo(boxObject, "Create Generated Box Collider");
        boxObject.transform.SetParent(parent, false);
        boxObject.transform.localPosition = frame.ScanToTarget.MultiplyPoint3x4(bounds.Center);
        boxObject.transform.localRotation = frame.RotationInTarget;
        boxObject.transform.localScale = createVisibleBox ? Vector3.Scale(frame.ScaleInTarget, bounds.Size) : frame.ScaleInTarget;

        BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(boxObject);
        boxCollider.center = Vector3.zero;
        boxCollider.size = createVisibleBox ? Vector3.one : bounds.Size;
    }

    private static void ClearGeneratedBoxesFromSelection()
    {
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            Transform existingContainer = selectedObject.transform.Find(GeneratedContainerName);
            if (existingContainer != null)
            {
                Undo.DestroyObjectImmediate(existingContainer.gameObject);
                EditorUtility.SetDirty(selectedObject);
            }
        }
    }

    private static PcaFrame CalculatePcaFrame(Vector3[] vertices)
    {
        Vector3 mean = Vector3.zero;
        foreach (Vector3 vertex in vertices)
        {
            mean += vertex;
        }
        mean /= vertices.Length;

        Matrix3x3 covariance = Matrix3x3.Zero;
        foreach (Vector3 vertex in vertices)
        {
            Vector3 offset = vertex - mean;
            covariance.M00 += offset.x * offset.x;
            covariance.M01 += offset.x * offset.y;
            covariance.M02 += offset.x * offset.z;
            covariance.M11 += offset.y * offset.y;
            covariance.M12 += offset.y * offset.z;
            covariance.M22 += offset.z * offset.z;
        }

        float inverseCount = 1f / vertices.Length;
        covariance.M00 *= inverseCount;
        covariance.M01 *= inverseCount;
        covariance.M02 *= inverseCount;
        covariance.M11 *= inverseCount;
        covariance.M12 *= inverseCount;
        covariance.M22 *= inverseCount;
        covariance.M10 = covariance.M01;
        covariance.M20 = covariance.M02;
        covariance.M21 = covariance.M12;

        Vector3 axisX = PowerIteration(covariance, Vector3.right);
        float eigenValueX = Vector3.Dot(axisX, covariance.Multiply(axisX));
        Matrix3x3 deflated = covariance - Matrix3x3.OuterProduct(axisX, axisX) * eigenValueX;

        Vector3 axisY = PowerIteration(deflated, GetLeastAlignedAxis(axisX));
        axisY = (axisY - axisX * Vector3.Dot(axisY, axisX)).normalized;
        if (axisY == Vector3.zero)
        {
            axisY = GetLeastAlignedAxis(axisX);
        }

        Vector3 axisZ = Vector3.Cross(axisX, axisY).normalized;
        axisY = Vector3.Cross(axisZ, axisX).normalized;

        Quaternion rotation = Quaternion.LookRotation(axisZ, axisY);
        return new PcaFrame(NormalizeQuaternion(rotation), axisX, axisY, axisZ);
    }

    private static Vector3 PowerIteration(Matrix3x3 matrix, Vector3 start)
    {
        Vector3 vector = start.normalized;
        if (vector == Vector3.zero)
        {
            vector = Vector3.right;
        }

        for (int i = 0; i < 16; i++)
        {
            Vector3 next = matrix.Multiply(vector);
            if (next.sqrMagnitude < 0.0000001f)
            {
                return vector;
            }

            vector = next.normalized;
        }

        return vector;
    }

    private static Vector3 GetLeastAlignedAxis(Vector3 axis)
    {
        Vector3 absAxis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
        if (absAxis.x <= absAxis.y && absAxis.x <= absAxis.z)
        {
            return Vector3.right;
        }

        if (absAxis.y <= absAxis.x && absAxis.y <= absAxis.z)
        {
            return Vector3.up;
        }

        return Vector3.forward;
    }

    private static Quaternion NormalizeQuaternion(Quaternion quaternion)
    {
        float magnitude = Mathf.Sqrt(
            quaternion.x * quaternion.x +
            quaternion.y * quaternion.y +
            quaternion.z * quaternion.z +
            quaternion.w * quaternion.w);

        if (magnitude < 0.000001f)
        {
            return Quaternion.identity;
        }

        float inverseMagnitude = 1f / magnitude;
        return new Quaternion(
            quaternion.x * inverseMagnitude,
            quaternion.y * inverseMagnitude,
            quaternion.z * inverseMagnitude,
            quaternion.w * inverseMagnitude);
    }

    private static float CalculateTotalBoxVolume(List<BoxBounds> boxes)
    {
        float totalVolume = 0f;
        foreach (BoxBounds box in boxes)
        {
            totalVolume += GetVolume(box.Size);
        }

        return totalVolume;
    }

    private static float GetVolume(Bounds bounds)
    {
        return GetVolume(bounds.size);
    }

    private static float GetVolume(Vector3 size)
    {
        return Mathf.Abs(size.x * size.y * size.z);
    }

    private static float SqrDistancePointTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = point - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f)
        {
            return (point - a).sqrMagnitude;
        }

        Vector3 bp = point - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3)
        {
            return (point - b).sqrMagnitude;
        }

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            Vector3 projection = a + v * ab;
            return (point - projection).sqrMagnitude;
        }

        Vector3 cp = point - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6)
        {
            return (point - c).sqrMagnitude;
        }

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            Vector3 projection = a + w * ac;
            return (point - projection).sqrMagnitude;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            Vector3 projection = b + w * (c - b);
            return (point - projection).sqrMagnitude;
        }

        Vector3 normal = Vector3.Cross(ab, ac).normalized;
        float distance = Vector3.Dot(point - a, normal);
        return distance * distance;
    }

    private struct SourceMeshData
    {
        public readonly string Name;
        public readonly Vector3[] Vertices;
        public readonly int[] Triangles;
        public readonly Quaternion RotationInTarget;

        public SourceMeshData(string name, Vector3[] vertices, int[] triangles, Quaternion rotationInTarget)
        {
            Name = name;
            Vertices = vertices;
            Triangles = triangles;
            RotationInTarget = NormalizeQuaternion(rotationInTarget);
        }
    }

    private struct ScanMeshData
    {
        public readonly Vector3[] Vertices;
        public readonly int[] Triangles;

        public ScanMeshData(Vector3[] vertices, int[] triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }
    }

    private struct ScanFrame
    {
        public static readonly ScanFrame Identity = new ScanFrame(Quaternion.identity);

        public readonly Quaternion RotationInTarget;
        public readonly Matrix4x4 TargetToScan;
        public readonly Matrix4x4 ScanToTarget;
        public readonly Vector3 ScaleInTarget;

        public ScanFrame(Quaternion rotationInTarget)
            : this(rotationInTarget, Vector3.one)
        {
        }

        public ScanFrame(Quaternion rotationInTarget, Vector3 scaleInTarget)
        {
            RotationInTarget = NormalizeQuaternion(rotationInTarget);
            ScaleInTarget = scaleInTarget;
            ScanToTarget = Matrix4x4.TRS(Vector3.zero, RotationInTarget, ScaleInTarget);
            TargetToScan = ScanToTarget.inverse;
        }
    }

    private struct ScanResult
    {
        public static readonly ScanResult Empty = new ScanResult(ScanFrame.Identity, new List<BoxBounds>(), 0f, 0f);

        public readonly ScanFrame Frame;
        public readonly List<BoxBounds> Boxes;
        public readonly float TotalBoxVolume;
        public readonly float BoundsVolume;

        public ScanResult(ScanFrame frame, List<BoxBounds> boxes, float totalBoxVolume, float boundsVolume)
        {
            Frame = frame;
            Boxes = boxes;
            TotalBoxVolume = totalBoxVolume;
            BoundsVolume = boundsVolume;
        }
    }

    private struct ScanScore
    {
        public static readonly ScanScore Worst = new ScanScore(int.MaxValue, float.MaxValue, float.MaxValue);

        public readonly int BoxCount;
        public readonly float TotalBoxVolume;
        public readonly float BoundsVolume;

        public ScanScore(int boxCount, float totalBoxVolume, float boundsVolume)
        {
            BoxCount = boxCount;
            TotalBoxVolume = totalBoxVolume;
            BoundsVolume = boundsVolume;
        }

        public bool IsBetterThan(ScanScore other, bool preferFewerBoxes)
        {
            if (preferFewerBoxes)
            {
                if (BoxCount != other.BoxCount)
                {
                    return BoxCount < other.BoxCount;
                }

                if (!Mathf.Approximately(TotalBoxVolume, other.TotalBoxVolume))
                {
                    return TotalBoxVolume < other.TotalBoxVolume;
                }

                return BoundsVolume < other.BoundsVolume;
            }

            if (!Mathf.Approximately(TotalBoxVolume, other.TotalBoxVolume))
            {
                return TotalBoxVolume < other.TotalBoxVolume;
            }

            if (BoxCount != other.BoxCount)
            {
                return BoxCount < other.BoxCount;
            }

            return BoundsVolume < other.BoundsVolume;
        }
    }

    private struct BoxBounds
    {
        public readonly Vector3 Center;
        public readonly Vector3 Size;

        public BoxBounds(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = size;
        }
    }

    private struct PcaFrame
    {
        public readonly Quaternion Rotation;
        public readonly Vector3 AxisX;
        public readonly Vector3 AxisY;
        public readonly Vector3 AxisZ;

        public PcaFrame(Quaternion rotation, Vector3 axisX, Vector3 axisY, Vector3 axisZ)
        {
            Rotation = rotation;
            AxisX = axisX;
            AxisY = axisY;
            AxisZ = axisZ;
        }
    }

    private struct Matrix3x3
    {
        public static readonly Matrix3x3 Zero = new Matrix3x3();

        public float M00;
        public float M01;
        public float M02;
        public float M10;
        public float M11;
        public float M12;
        public float M20;
        public float M21;
        public float M22;

        public Vector3 Multiply(Vector3 vector)
        {
            return new Vector3(
                M00 * vector.x + M01 * vector.y + M02 * vector.z,
                M10 * vector.x + M11 * vector.y + M12 * vector.z,
                M20 * vector.x + M21 * vector.y + M22 * vector.z);
        }

        public static Matrix3x3 OuterProduct(Vector3 a, Vector3 b)
        {
            return new Matrix3x3
            {
                M00 = a.x * b.x,
                M01 = a.x * b.y,
                M02 = a.x * b.z,
                M10 = a.y * b.x,
                M11 = a.y * b.y,
                M12 = a.y * b.z,
                M20 = a.z * b.x,
                M21 = a.z * b.y,
                M22 = a.z * b.z
            };
        }

        public static Matrix3x3 operator -(Matrix3x3 a, Matrix3x3 b)
        {
            return new Matrix3x3
            {
                M00 = a.M00 - b.M00,
                M01 = a.M01 - b.M01,
                M02 = a.M02 - b.M02,
                M10 = a.M10 - b.M10,
                M11 = a.M11 - b.M11,
                M12 = a.M12 - b.M12,
                M20 = a.M20 - b.M20,
                M21 = a.M21 - b.M21,
                M22 = a.M22 - b.M22
            };
        }

        public static Matrix3x3 operator *(Matrix3x3 matrix, float value)
        {
            return new Matrix3x3
            {
                M00 = matrix.M00 * value,
                M01 = matrix.M01 * value,
                M02 = matrix.M02 * value,
                M10 = matrix.M10 * value,
                M11 = matrix.M11 * value,
                M12 = matrix.M12 * value,
                M20 = matrix.M20 * value,
                M21 = matrix.M21 * value,
                M22 = matrix.M22 * value
            };
        }
    }
}
