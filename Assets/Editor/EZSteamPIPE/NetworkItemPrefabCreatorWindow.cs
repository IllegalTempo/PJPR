using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class NetworkItemPrefabCreatorWindow : EditorWindow
{
    private const string PrefabRoot = "Assets/Prefabs";
    private const string RegistryPath = PrefabRoot + "/NetworkPrefabRegistry.asset";

    private string _group = "";
    private string _prefabName = "NewNetworkItem";
    private string _prefabId = "NewNetworkItem";
    private string _itemDescription = "";
    private int _maxStackSize = 64;
    private bool _isModule;
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
        EditorGUILayout.HelpBox("Creates PrefabDefinition assets and registry entries in the selected folder. No scene GameObject or prefab asset is created.", MessageType.Info);

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
        _isModule = EditorGUILayout.Toggle("Is Module", _isModule);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Target Folder", GetTargetFolderPath());
        EditorGUILayout.LabelField("Definition Path", GetItemDefinitionPath());
        if (_isModule)
        {
            EditorGUILayout.LabelField("Controller Definition Path", GetControllerDefinitionPath());
        }

        ValidationResult validation = ValidateInput();
        if (!validation.IsValid)
        {
            EditorGUILayout.HelpBox(validation.Message, MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!validation.IsValid))
        {
            if (GUILayout.Button("Create Definition", GUILayout.Height(32)))
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
            groups.Add(groupPath);
            AddGroupsRecursive(subFolder, groups);
        }
    }

    private void CreatePrefab()
    {
        EnsureFolder(GetTargetFolderPath());

        PrefabDefinition itemDefinition = CreateItemDefinition();
        AddRegistryEntry(_prefabId.Trim(), itemDefinition);

        if (_isModule && itemDefinition is ModuleDefinition moduleDefinition)
        {
            PrefabDefinition controllerDefinition = CreateControllerDefinition();
            moduleDefinition.controlPrefab = controllerDefinition;
            EditorUtility.SetDirty(moduleDefinition);
            AddRegistryEntry(GetControllerPrefabId(), controllerDefinition);
        }

        Selection.activeObject = itemDefinition;
        EditorGUIUtility.PingObject(itemDefinition);
        AssetDatabase.SaveAssets();

        if (Application.isPlaying && NetworkSystem.Instance != null)
        {
            NetworkSystem.Instance.RebuildNetworkPrefabLookup();
        }

        EditorUtility.DisplayDialog("Create Network Item Prefab", "Created definition assets for '" + _prefabName.Trim() + "'.", "OK");
    }

    private PrefabDefinition CreateItemDefinition()
    {
        PrefabDefinition itemDefinition = _isModule
            ? CreateInstance<ModuleDefinition>()
            : CreateInstance<PrefabDefinition>();

        itemDefinition.itemName = _prefabName.Trim();
        itemDefinition.itemDescription = _itemDescription;
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

    private PrefabDefinition CreateControllerDefinition()
    {
        PrefabDefinition controllerDefinition = CreateInstance<PrefabDefinition>();
        controllerDefinition.itemName = GetControllerName();
        controllerDefinition.itemDescription = _prefabName.Trim() + " controller";
        controllerDefinition.maxStackSize = 1;
        controllerDefinition.holdState = new ItemSnapshot
        {
            position = Vector3.zero,
            rotation = Quaternion.identity,
            scale = Vector3.one
        };

        AssetDatabase.CreateAsset(controllerDefinition, GetControllerDefinitionPath());
        EditorUtility.SetDirty(controllerDefinition);
        return controllerDefinition;
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

        if (File.Exists(GetItemDefinitionPath()))
        {
            return ValidationResult.Invalid("A definition already exists at this path.");
        }

        if (_isModule && File.Exists(GetControllerDefinitionPath()))
        {
            return ValidationResult.Invalid("A controller definition already exists at this path.");
        }

        if (_isModule && ItemDefinitionPrefabIdExists(GetControllerPrefabId()))
        {
            return ValidationResult.Invalid("Controller prefab ID is already used by an item definition.");
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

        NetworkPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<NetworkPrefabRegistry>(RegistryPath);
        if (registry == null)
        {
            return false;
        }

        foreach (NetworkPrefabRegistry.Entry entry in registry.Entries)
        {
            if (entry != null && entry.PrefabId == prefabID)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddRegistryEntry(string prefabId, PrefabDefinition itemDefinition)
    {
        NetworkPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<NetworkPrefabRegistry>(RegistryPath);
        if (registry == null)
        {
            EnsureFolder(PrefabRoot);
            registry = ScriptableObject.CreateInstance<NetworkPrefabRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        SerializedObject registryObject = new SerializedObject(registry);
        SerializedProperty entries = registryObject.FindProperty("entries");
        int index = entries.arraySize;
        entries.InsertArrayElementAtIndex(index);

        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("PrefabId").stringValue = prefabId;
        entry.FindPropertyRelative("PrefabDefinition").objectReferenceValue = itemDefinition;

        registryObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
    }

    private string GetItemDefinitionPath()
    {
        return GetTargetFolderPath() + "/" + SanitizeFileName(_prefabName) + ".asset";
    }

    private string GetControllerDefinitionPath()
    {
        return GetTargetFolderPath() + "/" + SanitizeFileName(GetControllerName()) + ".asset";
    }

    private string GetControllerName()
    {
        return _prefabName.Trim() + "_controller";
    }

    private string GetControllerPrefabId()
    {
        return _prefabId.Trim() + "_controller";
    }

    private string GetTargetFolderPath()
    {
        string groupPath = SanitizeGroupPath(_group);
        string itemFolder = SanitizeFileName(_prefabName);
        string basePath = string.IsNullOrWhiteSpace(groupPath) ? PrefabRoot : PrefabRoot + "/" + groupPath;
        return basePath + "/" + itemFolder;
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
