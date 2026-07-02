using System.Collections.Generic;
using System.IO;
using Assets.codes.Network.SyncedIdentity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NetworkItemPrefabCreatorWindow : EditorWindow
{
    private const string PrefabRoot = "Assets/Resources/Prefabs";

    private string _group = "";
    private string _prefabName = "NewNetworkItem";
    private string _prefabId = "NewNetworkItem";
    private string _itemDescription = "";
    private int _maxStackSize = 64;
    private bool _useSceneViewPosition = true;
    private Vector3 _spawnPosition = Vector3.zero;
    private string _newGroup = "";
    private string[] _groupOptions = new[] { "" };
    private string[] _groupLabels = new[] { "(None)" };
    private int _selectedGroupIndex;

    [MenuItem("Tools/Network Objects/New Item Prefab")]
    public static void ShowWindow()
    {
        NetworkItemPrefabCreatorWindow window = GetWindow<NetworkItemPrefabCreatorWindow>("New Item Prefab");
        window.minSize = new Vector2(440, 260);
        window.SuggestPrefabId();
    }

    private void OnEnable()
    {
        RefreshGroupOptions();
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Network Item Prefab", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates a prefab instance in the open scene, adds NetworkPrefabIdentity, Item, and NetworkGameObject, then creates an ItemDefinition with the canonical prefab ID.", MessageType.Info);

        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        DrawGroupField();
        _prefabName = EditorGUILayout.TextField("Prefab Name", _prefabName);
        if (EditorGUI.EndChangeCheck())
        {
            SuggestPrefabId();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _prefabId = EditorGUILayout.TextField("Prefab ID", _prefabId);
            if (GUILayout.Button("Suggest", GUILayout.Width(80)))
            {
                SuggestPrefabId();
            }
        }

        _itemDescription = EditorGUILayout.TextField("Item Description", _itemDescription);
        _maxStackSize = EditorGUILayout.IntField("Max Stack Size", _maxStackSize);

        _useSceneViewPosition = EditorGUILayout.ToggleLeft("Place at Scene view pivot", _useSceneViewPosition);
        using (new EditorGUI.DisabledScope(_useSceneViewPosition))
        {
            _spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", _spawnPosition);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Target Folder", GetTargetFolderPath());
        EditorGUILayout.LabelField("Prefab Path", GetPrefabPath());
        EditorGUILayout.LabelField("Item Definition Path", GetItemDefinitionPath());

        ValidationResult validation = ValidateInput();
        if (!validation.IsValid)
        {
            EditorGUILayout.HelpBox(validation.Message, MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!validation.IsValid))
        {
            if (GUILayout.Button("Create Prefab In Scene", GUILayout.Height(32)))
            {
                CreatePrefab();
            }
        }
    }

    private void DrawGroupField()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            int selectedIndex = Mathf.Clamp(_selectedGroupIndex, 0, _groupOptions.Length - 1);
            selectedIndex = EditorGUILayout.Popup("Group", selectedIndex, _groupLabels);
            if (selectedIndex != _selectedGroupIndex)
            {
                _selectedGroupIndex = selectedIndex;
                _group = _groupOptions[_selectedGroupIndex];
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                RefreshGroupOptions();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _newGroup = EditorGUILayout.TextField("New Group", _newGroup);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newGroup)))
            {
                if (GUILayout.Button("Add", GUILayout.Width(70)))
                {
                    AddNewGroup();
                }
            }
        }
    }

    private void RefreshGroupOptions()
    {
        EnsureFolder(PrefabRoot);

        List<string> groups = new List<string> { "" };
        AddGroupsRecursive(PrefabRoot, groups);

        string currentGroup = SanitizeGroupPath(_group);
        if (!string.IsNullOrWhiteSpace(currentGroup) && !groups.Contains(currentGroup))
        {
            groups.Add(currentGroup);
        }

        groups.Sort((left, right) =>
        {
            if (string.IsNullOrWhiteSpace(left))
            {
                return -1;
            }

            if (string.IsNullOrWhiteSpace(right))
            {
                return 1;
            }

            return string.CompareOrdinal(left, right);
        });

        _groupOptions = groups.ToArray();
        _groupLabels = new string[_groupOptions.Length];
        for (int i = 0; i < _groupOptions.Length; i++)
        {
            _groupLabels[i] = string.IsNullOrWhiteSpace(_groupOptions[i]) ? "(None)" : _groupOptions[i];
        }

        _selectedGroupIndex = FindGroupIndex(currentGroup);
        _group = _groupOptions[_selectedGroupIndex];
        Repaint();
    }

    private void AddNewGroup()
    {
        string groupPath = SanitizeGroupPath(_newGroup);
        if (string.IsNullOrWhiteSpace(groupPath))
        {
            return;
        }

        EnsureFolder(PrefabRoot + "/" + groupPath);
        AssetDatabase.Refresh();
        _group = groupPath;
        _newGroup = "";
        RefreshGroupOptions();
        SuggestPrefabId();
    }

    private int FindGroupIndex(string group)
    {
        for (int i = 0; i < _groupOptions.Length; i++)
        {
            if (_groupOptions[i] == group)
            {
                return i;
            }
        }

        return 0;
    }

    private static void AddGroupsRecursive(string folderPath, List<string> groups)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
        foreach (string subFolder in subFolders)
        {
            string groupPath = subFolder.Substring(PrefabRoot.Length).TrimStart('/');
            if (!IsGeneratedItemFolder(subFolder))
            {
                groups.Add(groupPath);
            }

            AddGroupsRecursive(subFolder, groups);
        }
    }

    private static bool IsGeneratedItemFolder(string folderPath)
    {
        string folderName = Path.GetFileName(folderPath);
        string prefabPath = folderPath + "/" + folderName + ".prefab";
        string itemDefinitionPath = folderPath + "/" + folderName + ".asset";
        return File.Exists(prefabPath) || File.Exists(itemDefinitionPath);
    }

    private void CreatePrefab()
    {
        EnsureFolder(GetTargetFolderPath());

        string prefabPath = GetPrefabPath();
        GameObject instance = new GameObject(_prefabName.Trim());
        Undo.RegisterCreatedObjectUndo(instance, "Create Network Item Prefab");
        instance.transform.position = GetScenePosition();

        NetworkPrefabIdentity identity = AddOrGetComponent<NetworkPrefabIdentity>(instance);
        identity.PrefabID = _prefabId.Trim();

        NetworkGameObject networkGameObject = AddOrGetComponent<NetworkGameObject>(instance);
        networkGameObject.Identity = identity;

        Item item = AddOrGetComponent<Item>(instance);
        AddOrGetComponent<StaticOutline>(instance);
        AddOrGetComponent<BoxCollider>(instance);

        SerializedObject itemObject = new SerializedObject(item);
        SerializedProperty netObjProperty = itemObject.FindProperty("netObj");
        if (netObjProperty != null)
        {
            netObjProperty.objectReferenceValue = networkGameObject;
            itemObject.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.UserAction);
        if (prefabAsset == null)
        {
            Undo.DestroyObjectImmediate(instance);
            EditorUtility.DisplayDialog("Create Network Item Prefab", "Unity could not save the prefab asset.", "OK");
            return;
        }

        ItemDefinition itemDefinition = CreateItemDefinition(prefabAsset);
        AssignItemDefinition(instance, prefabAsset, itemDefinition);
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(prefabAsset);
        AssetDatabase.SaveAssets();

        if (Application.isPlaying && NetworkSystem.Instance != null)
        {
            NetworkSystem.Instance.RebuildNetworkPrefabLookup();
        }

        EditorUtility.DisplayDialog("Create Network Item Prefab", "Created item definition and prefab for '" + _prefabName.Trim() + "'.", "OK");
    }

    private ItemDefinition CreateItemDefinition(GameObject prefabAsset)
    {
        ItemDefinition itemDefinition = CreateInstance<ItemDefinition>();
        itemDefinition.itemName = _prefabName.Trim();
        itemDefinition.itemDescription = _itemDescription;
        itemDefinition.itemPrefab = prefabAsset;
        itemDefinition.prefabID = _prefabId.Trim();
        itemDefinition.maxStackSize = Mathf.Max(1, _maxStackSize);
        itemDefinition.holdState = new ItemSnapshot
        {
            position = Vector3.zero,
            rotation = Quaternion.identity,
            scale = Vector3.one
        };

        AssetDatabase.CreateAsset(itemDefinition, GetItemDefinitionPath());
        EditorUtility.SetDirty(itemDefinition);
        return itemDefinition;
    }

    private static void AssignItemDefinition(GameObject instance, GameObject prefabAsset, ItemDefinition itemDefinition)
    {
        Item instanceItem = instance.GetComponent<Item>();
        if (instanceItem != null)
        {
            SerializedObject itemObject = new SerializedObject(instanceItem);
            SerializedProperty abstractItemProperty = itemObject.FindProperty("AbstractItem");
            if (abstractItemProperty != null)
            {
                abstractItemProperty.objectReferenceValue = itemDefinition;
                itemObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Item prefabItem = prefabAsset.GetComponent<Item>();
        if (prefabItem != null)
        {
            SerializedObject prefabItemObject = new SerializedObject(prefabItem);
            SerializedProperty abstractItemProperty = prefabItemObject.FindProperty("AbstractItem");
            if (abstractItemProperty != null)
            {
                abstractItemProperty.objectReferenceValue = itemDefinition;
                prefabItemObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefabAsset);
            }
        }

        PrefabUtility.SavePrefabAsset(prefabAsset);
    }

    private ValidationResult ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(_prefabName))
        {
            return ValidationResult.Invalid("Enter a prefab name.");
        }

        if (string.IsNullOrWhiteSpace(_prefabId))
        {
            return ValidationResult.Invalid("Enter a prefab ID.");
        }

        if (ItemDefinitionPrefabIdExists(_prefabId.Trim()))
        {
            return ValidationResult.Invalid("Prefab ID is already used by an item definition.");
        }

        if (File.Exists(GetPrefabPath()))
        {
            return ValidationResult.Invalid("A prefab already exists at this path.");
        }

        if (File.Exists(GetItemDefinitionPath()))
        {
            return ValidationResult.Invalid("An item definition already exists at this path.");
        }

        if (_maxStackSize < 1)
        {
            return ValidationResult.Invalid("Max stack size must be at least 1.");
        }

        return ValidationResult.Valid();
    }

    private void SuggestPrefabId()
    {
        string groupId = SanitizeGroupForId(_group);
        string prefabId = SanitizeId(_prefabName);
        string baseId = string.IsNullOrWhiteSpace(groupId) ? prefabId : groupId + "_" + prefabId;
        if (string.IsNullOrWhiteSpace(baseId))
        {
            baseId = "NewNetworkItem";
        }

        string candidate = baseId;
        int suffix = 2;
        while (ItemDefinitionPrefabIdExists(candidate))
        {
            candidate = baseId + "_" + suffix;
            suffix++;
        }

        _prefabId = candidate;
    }

    private static bool ItemDefinitionPrefabIdExists(string prefabID)
    {
        if (string.IsNullOrWhiteSpace(prefabID))
        {
            return false;
        }

        if (!AssetDatabase.IsValidFolder(PrefabRoot))
        {
            return false;
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { PrefabRoot });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            if (itemDefinition != null && itemDefinition.prefabID == prefabID)
            {
                return true;
            }
        }

        return false;
    }

    private string GetPrefabPath()
    {
        return GetTargetFolderPath() + "/" + SanitizeFileName(_prefabName) + ".prefab";
    }

    private string GetItemDefinitionPath()
    {
        return GetTargetFolderPath() + "/" + SanitizeFileName(_prefabName) + ".asset";
    }

    private string GetTargetFolderPath()
    {
        string groupPath = SanitizeGroupPath(_group);
        string itemFolder = SanitizeFileName(_prefabName);
        string basePath = string.IsNullOrWhiteSpace(groupPath) ? PrefabRoot : PrefabRoot + "/" + groupPath;
        return basePath + "/" + itemFolder;
    }

    private Vector3 GetScenePosition()
    {
        if (!_useSceneViewPosition)
        {
            return _spawnPosition;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        return sceneView != null ? sceneView.pivot : Vector3.zero;
    }

    private static T AddOrGetComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string SanitizeId(string value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        char[] chars = trimmed.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
            {
                chars[i] = '_';
            }
        }

        return new string(chars).Trim('_');
    }

    private static string SanitizeFileName(string value)
    {
        string sanitized = SanitizeId(value);
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidChar.ToString(), "_");
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "NewNetworkItem" : sanitized;
    }

    private static string SanitizeGroupPath(string value)
    {
        string[] parts = (value ?? string.Empty).Split('/', '\\');
        string path = "";
        foreach (string part in parts)
        {
            string sanitized = SanitizeFolderName(part);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                continue;
            }

            path = string.IsNullOrWhiteSpace(path) ? sanitized : path + "/" + sanitized;
        }

        return path;
    }

    private static string SanitizeFolderName(string value)
    {
        string sanitized = SanitizeId(value);
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidChar.ToString(), "_");
        }

        return sanitized;
    }

    private static string SanitizeGroupForId(string value)
    {
        return SanitizeGroupPath(value).Replace("/", "_");
    }

    private struct ValidationResult
    {
        public readonly bool IsValid;
        public readonly string Message;

        private ValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }

        public static ValidationResult Valid()
        {
            return new ValidationResult(true, string.Empty);
        }

        public static ValidationResult Invalid(string message)
        {
            return new ValidationResult(false, message);
        }
    }
}
