using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class NetworkMessageWindow : EditorWindow
{
    private enum MessageDirection
    {
        Both,
        Server,
        Client
    }

    private enum FieldType
    {
        Int,
        Float,
        Short,
        Long,
        ULong,
        Bool,
        Guid,
        Vector3,
        Quaternion,
        StringUNICODE,
        ByteArray
    }

    [Serializable]
    private class MessageField
    {
        public FieldType Type;
        public string Name = "";
    }

    private const string PacketsPath = "Assets/codes/Network/Packets/packets.cs";
    private const string RouterPath = "Assets/codes/Network/NetworkRouter.cs";
    private const string MessagesRoot = "Assets/codes/Network/Messages";
    private const string RuntimeProjectPath = "Assembly-CSharp.csproj";

    private string _messageName = "";
    private MessageDirection _direction = MessageDirection.Both;
    private bool _addEnumEntry = true;
    private bool _createMessageClass = true;
    private bool _registerInRouter = true;
    private bool _updateCsproj = true;
    private readonly List<MessageField> _fields = new List<MessageField>();
    private Vector2 _existingMessagesScroll;
    private Vector2 _fieldScroll;

    [MenuItem("Tools/Network Messages/Add Message")]
    public static void ShowWindow()
    {
        NetworkMessageWindow window = GetWindow<NetworkMessageWindow>("Add Network Message");
        window.minSize = new Vector2(640, 700);
    }

    private void OnGUI()
    {
        GUILayout.Label("Add Network Message", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        _messageName = EditorGUILayout.TextField("Message Name", _messageName);
        _direction = (MessageDirection)EditorGUILayout.EnumPopup("Direction", _direction);

        EditorGUILayout.Space(6);
        DrawFieldsSection();

        EditorGUILayout.Space(8);
        GUILayout.Label("Generate", EditorStyles.boldLabel);
        _addEnumEntry = EditorGUILayout.Toggle("Enum Entry", _addEnumEntry);
        _createMessageClass = EditorGUILayout.Toggle("Message Class", _createMessageClass);
        _registerInRouter = EditorGUILayout.Toggle("Router Registration", _registerInRouter);
        _updateCsproj = EditorGUILayout.Toggle("Update .csproj", _updateCsproj);

        EditorGUILayout.Space(12);
        EditorGUILayout.HelpBox(GetPreviewText(), MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_messageName);
            if (GUILayout.Button("Add Message", GUILayout.Width(110)))
            {
                AddMessage();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(16);
        DrawExistingMessages();
    }

    private string GetPreviewText()
    {
        string normalizedName = NormalizeName(_messageName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = "ExampleMessage";
        }

        return "Creates " + GetClassName(_direction, normalizedName) + ".cs with " +
               _fields.Count + " field" + (_fields.Count == 1 ? "" : "s") + " and registers " +
               GetEnumName(_direction) + "." + normalizedName + ".";
    }

    private void AddMessage()
    {
        string messageName = NormalizeName(_messageName);
        if (!ValidateMessageName(messageName))
        {
            return;
        }

        if (!ValidateFields())
        {
            return;
        }

        try
        {
            string classPath = GetClassPath(_direction, messageName);

            if (_addEnumEntry)
            {
                AddEnumEntry(_direction, messageName);
            }

            if (_createMessageClass)
            {
                CreateMessageClass(_direction, messageName, classPath, _fields);
            }

            if (_registerInRouter)
            {
                RegisterInRouter(_direction, messageName);
            }

            if (_updateCsproj)
            {
                TryAddCompileInclude(classPath);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Network message '" + messageName + "' added.", "OK");
            Close();
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to add network message:\n" + ex.Message, "OK");
        }
    }

    private static bool ValidateMessageName(string messageName)
    {
        if (string.IsNullOrWhiteSpace(messageName))
        {
            EditorUtility.DisplayDialog("Error", "Message name cannot be empty.", "OK");
            return false;
        }

        if (!Regex.IsMatch(messageName, @"^[A-Z_a-z][A-Z_a-z0-9]*$"))
        {
            EditorUtility.DisplayDialog("Error", "Message name must be a valid C# identifier.", "OK");
            return false;
        }

        return true;
    }

    private static string NormalizeName(string rawName)
    {
        string trimmed = (rawName ?? "").Trim();
        if (trimmed.StartsWith("NMS_Both_"))
        {
            return trimmed.Substring("NMS_Both_".Length);
        }

        if (trimmed.StartsWith("NMS_Server_"))
        {
            return trimmed.Substring("NMS_Server_".Length);
        }

        if (trimmed.StartsWith("NMS_Client_"))
        {
            return trimmed.Substring("NMS_Client_".Length);
        }

        return trimmed;
    }

    private static string GetEnumName(MessageDirection direction)
    {
        switch (direction)
        {
            case MessageDirection.Server:
                return "ServerPackets";
            case MessageDirection.Client:
                return "ClientPackets";
            default:
                return "BothPackets";
        }
    }

    private static string GetDictionaryName(MessageDirection direction)
    {
        switch (direction)
        {
            case MessageDirection.Server:
                return "serverMessages";
            case MessageDirection.Client:
                return "clientMessages";
            default:
                return "bothMessages";
        }
    }

    private static string GetFolderName(MessageDirection direction)
    {
        switch (direction)
        {
            case MessageDirection.Server:
                return "NMS_Server";
            case MessageDirection.Client:
                return "NMS_Client";
            default:
                return "NMS_Both";
        }
    }

    private static string GetPrefix(MessageDirection direction)
    {
        switch (direction)
        {
            case MessageDirection.Server:
                return "NMS_Server_";
            case MessageDirection.Client:
                return "NMS_Client_";
            default:
                return "NMS_Both_";
        }
    }

    private static string GetClassName(MessageDirection direction, string messageName)
    {
        return GetPrefix(direction) + messageName;
    }

    private static string GetClassPath(MessageDirection direction, string messageName)
    {
        return Path.Combine(MessagesRoot, GetFolderName(direction), GetClassName(direction, messageName) + ".cs").Replace("\\", "/");
    }

    private static void AddEnumEntry(MessageDirection direction, string messageName)
    {
        if (!File.Exists(PacketsPath))
        {
            throw new FileNotFoundException("Could not find packets.cs.", PacketsPath);
        }

        string enumName = GetEnumName(direction);
        string content = File.ReadAllText(PacketsPath);
        Match match = Regex.Match(content, @"public\s+enum\s+" + enumName + @"\s*\{(?<body>.*?)\}", RegexOptions.Singleline);
        if (!match.Success)
        {
            throw new InvalidOperationException("Could not find enum " + enumName + ".");
        }

        string enumBody = match.Groups["body"].Value;
        if (Regex.IsMatch(enumBody, @"\b" + Regex.Escape(messageName) + @"\b"))
        {
            throw new InvalidOperationException(enumName + "." + messageName + " already exists.");
        }

        int value = CalculateNextEnumValue(enumBody);
        string trimmedBody = enumBody.TrimEnd();
        bool needsComma = trimmedBody.Length > 0 && !trimmedBody.EndsWith(",");
        string insertText = (needsComma ? "," : "") + "\n        " + messageName + " = " + value + ",";

        int insertIndex = match.Groups["body"].Index + match.Groups["body"].Length;
        content = content.Insert(insertIndex, insertText);
        File.WriteAllText(PacketsPath, content);
    }

    private static int CalculateNextEnumValue(string enumBody)
    {
        MatchCollection valueMatches = Regex.Matches(enumBody, @"=\s*(\d+)");
        int maxValue = -1;
        foreach (Match valueMatch in valueMatches)
        {
            if (int.TryParse(valueMatch.Groups[1].Value, out int value))
            {
                maxValue = Mathf.Max(maxValue, value);
            }
        }

        return maxValue + 1;
    }

    private void DrawFieldsSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Fields", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Field", GUILayout.Width(90)))
                {
                    _fields.Add(new MessageField { Type = FieldType.Int, Name = "value" });
                }
            }

            if (_fields.Count == 0)
            {
                EditorGUILayout.HelpBox("Add one or more fields to generate constructor, read, and write code.", MessageType.None);
                return;
            }

            _fieldScroll = EditorGUILayout.BeginScrollView(_fieldScroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(220));
            for (int i = 0; i < _fields.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    MessageField field = _fields[i];
                    field.Type = (FieldType)EditorGUILayout.EnumPopup(field.Type, GUILayout.Width(150));
                    field.Name = EditorGUILayout.TextField(field.Name);
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        _fields.RemoveAt(i);
                        i--;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private bool ValidateFields()
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < _fields.Count; i++)
        {
            MessageField field = _fields[i];
            field.Name = (field.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(field.Name))
            {
                EditorUtility.DisplayDialog("Error", "Field names cannot be empty.", "OK");
                return false;
            }

            if (!Regex.IsMatch(field.Name, @"^[A-Z_a-z][A-Z_a-z0-9]*$"))
            {
                EditorUtility.DisplayDialog("Error", "Field '" + field.Name + "' is not a valid C# identifier.", "OK");
                return false;
            }

            if (!names.Add(field.Name))
            {
                EditorUtility.DisplayDialog("Error", "Field name '" + field.Name + "' is duplicated.", "OK");
                return false;
            }
        }

        return true;
    }

    private static void CreateMessageClass(MessageDirection direction, string messageName, string classPath, IReadOnlyList<MessageField> fields)
    {
        if (File.Exists(classPath))
        {
            throw new InvalidOperationException("Message class already exists at " + classPath + ".");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(classPath));
        File.WriteAllText(classPath, GenerateClassCode(direction, messageName, fields));
    }

    private static string GenerateClassCode(MessageDirection direction, string messageName, IReadOnlyList<MessageField> fields)
    {
        string className = GetClassName(direction, messageName);
        string enumName = GetEnumName(direction);
        string baseType = direction == MessageDirection.Both ? "NMS_BOTH_SHARE" : "NMS";
        string interfaces = direction == MessageDirection.Both ? string.Empty : ", " + GetInterfaces(direction);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("using Steamworks;");
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine();
        builder.AppendLine("namespace Assets.codes.Network.Messages");
        builder.AppendLine("{");
        builder.AppendLine("    public class " + className + " : " + baseType + interfaces);
        builder.AppendLine("    {");
        AppendFieldDeclarations(builder, fields);
        builder.AppendLine("        public " + className + "(" + BuildConstructorParameters(fields) + ") : base((int)packets." + enumName + "." + messageName + ")");
        builder.AppendLine("        {");
        AppendConstructorAssignments(builder, fields);
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public static " + className + " Read(Packet packet)");
        builder.AppendLine("        {");
        if (fields.Count == 0)
        {
            builder.AppendLine("            return new " + className + "();");
        }
        else
        {
            builder.AppendLine("            return new " + className + "(" + BuildReadArguments(fields) + ");");
        }
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public override void Write(Packet packet)");
        builder.AppendLine("        {");
        AppendWriteStatements(builder, fields);
        builder.AppendLine("        }");
        AppendHandleSection(builder, direction);
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string GetInterfaces(MessageDirection direction)
    {
        switch (direction)
        {
            case MessageDirection.Server:
                return "IClientHandle";
            case MessageDirection.Client:
                return "IServerHandle";
            default:
                return "IClientHandle, IServerHandle";
        }
    }

    private static void AppendFieldDeclarations(StringBuilder builder, IReadOnlyList<MessageField> fields)
    {
        foreach (MessageField field in fields)
        {
            builder.AppendLine("        private readonly " + GetCSharpType(field.Type) + " " + field.Name + ";");
        }
    }

    private static string BuildConstructorParameters(IReadOnlyList<MessageField> fields)
    {
        if (fields.Count == 0)
        {
            return string.Empty;
        }

        List<string> parameters = new List<string>();
        foreach (MessageField field in fields)
        {
            parameters.Add(GetCSharpType(field.Type) + " " + field.Name);
        }

        return string.Join(", ", parameters);
    }

    private static void AppendConstructorAssignments(StringBuilder builder, IReadOnlyList<MessageField> fields)
    {
        foreach (MessageField field in fields)
        {
            builder.AppendLine("            this." + field.Name + " = " + field.Name + ";");
        }
    }

    private static string BuildReadArguments(IReadOnlyList<MessageField> fields)
    {
        List<string> arguments = new List<string>();
        foreach (MessageField field in fields)
        {
            arguments.Add(GetReadExpression(field.Type));
        }

        return string.Join(", ", arguments);
    }

    private static void AppendWriteStatements(StringBuilder builder, IReadOnlyList<MessageField> fields)
    {
        if (fields.Count == 0)
        {
            builder.AppendLine("            // No fields to write.");
            return;
        }

        foreach (MessageField field in fields)
        {
            builder.AppendLine("            " + GetWriteExpression(field.Type, field.Name) + ";");
        }
    }

    private static void AppendHandleSection(StringBuilder builder, MessageDirection direction)
    {
        if (direction == MessageDirection.Both)
        {
            builder.AppendLine();
            builder.AppendLine("        protected override void applyaction()");
            builder.AppendLine("        {");
            builder.AppendLine("            // TODO: Apply shared client/server behavior.");
            builder.AppendLine("        }");
            return;
        }

        if (direction == MessageDirection.Client)
        {
            builder.AppendLine();
            builder.AppendLine("        public void ServerHandle(NetworkPlayer player)");
            builder.AppendLine("        {");
            builder.AppendLine("            // TODO: Apply server-side behavior.");
            builder.AppendLine("        }");
            return;
        }

        builder.AppendLine();
        builder.AppendLine("        public void ClientHandle()");
        builder.AppendLine("        {");
        builder.AppendLine("            // TODO: Apply client-side behavior.");
        builder.AppendLine("        }");
    }

    private static string GetCSharpType(FieldType type)
    {
        switch (type)
        {
            case FieldType.Int:
                return "int";
            case FieldType.Float:
                return "float";
            case FieldType.Short:
                return "short";
            case FieldType.Long:
                return "long";
            case FieldType.ULong:
                return "ulong";
            case FieldType.Bool:
                return "bool";
            case FieldType.Guid:
                return "Guid";
            case FieldType.Vector3:
                return "Vector3";
            case FieldType.Quaternion:
                return "Quaternion";
            case FieldType.StringUNICODE:
                return "string";
            case FieldType.ByteArray:
                return "byte[]";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static string GetReadExpression(FieldType type)
    {
        switch (type)
        {
            case FieldType.Int:
                return "packet.Readint()";
            case FieldType.Float:
                return "packet.Readfloat()";
            case FieldType.Short:
                return "packet.Readshort()";
            case FieldType.Long:
                return "packet.Readlong()";
            case FieldType.ULong:
                return "packet.Readulong()";
            case FieldType.Bool:
                return "packet.Readbool()";
            case FieldType.Guid:
                return "packet.ReadGuid()";
            case FieldType.Vector3:
                return "packet.Readvector3()";
            case FieldType.Quaternion:
                return "packet.Readquaternion()";
            case FieldType.StringUNICODE:
                return "packet.ReadstringUNICODE()";
            case FieldType.ByteArray:
                return "packet.ReadBytesArray()";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static string GetWriteExpression(FieldType type, string fieldName)
    {
        switch (type)
        {
            case FieldType.Int:
            case FieldType.Float:
            case FieldType.Short:
            case FieldType.Long:
            case FieldType.ULong:
            case FieldType.Bool:
            case FieldType.Guid:
            case FieldType.Vector3:
            case FieldType.Quaternion:
                return "packet.Write(" + fieldName + ")";
            case FieldType.StringUNICODE:
                return "packet.Write(" + fieldName + ")";
            case FieldType.ByteArray:
                return "packet.Write(" + fieldName + ")";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static void RegisterInRouter(MessageDirection direction, string messageName)
    {
        if (!File.Exists(RouterPath))
        {
            throw new FileNotFoundException("Could not find NetworkRouter.cs.", RouterPath);
        }

        string content = File.ReadAllText(RouterPath);
        string dictionaryName = GetDictionaryName(direction);
        string enumName = GetEnumName(direction);
        string className = GetClassName(direction, messageName);

        if (content.Contains(className + ".Read"))
        {
            throw new InvalidOperationException(className + " is already registered in NetworkRouter.");
        }

        int dictionaryIndex = content.IndexOf(dictionaryName + " = new()", StringComparison.Ordinal);
        if (dictionaryIndex < 0)
        {
            throw new InvalidOperationException("Could not find " + dictionaryName + " initializer in NetworkRouter.");
        }

        int openBrace = content.IndexOf('{', dictionaryIndex);
        int closeBrace = FindMatchingBrace(content, openBrace);
        if (openBrace < 0 || closeBrace < 0)
        {
            throw new InvalidOperationException("Could not find " + dictionaryName + " braces in NetworkRouter.");
        }

        string dictionaryBody = content.Substring(openBrace + 1, closeBrace - openBrace - 1);
        string trimmedBody = dictionaryBody.TrimEnd();
        bool needsComma = trimmedBody.Length > 0 && !trimmedBody.EndsWith(",");
        string entry = (needsComma ? "," : "") + "\n            { (int)packets." + enumName + "." + messageName + ", " + className + ".Read },";

        content = content.Insert(closeBrace, entry);
        File.WriteAllText(RouterPath, content);
    }

    private static int FindMatchingBrace(string content, int openBrace)
    {
        if (openBrace < 0)
        {
            return -1;
        }

        int depth = 0;
        for (int i = openBrace; i < content.Length; i++)
        {
            if (content[i] == '{')
            {
                depth++;
            }
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static void TryAddCompileInclude(string classPath)
    {
        if (!File.Exists(RuntimeProjectPath))
        {
            return;
        }

        string includePath = classPath.Replace("/", "\\");
        string content = File.ReadAllText(RuntimeProjectPath);
        if (content.Contains("Include=\"" + includePath + "\""))
        {
            return;
        }

        int itemGroupEnd = content.IndexOf("</ItemGroup>", StringComparison.Ordinal);
        if (itemGroupEnd < 0)
        {
            return;
        }

        string includeLine = "    <Compile Include=\"" + includePath + "\" />\n";
        content = content.Insert(itemGroupEnd, includeLine);
        File.WriteAllText(RuntimeProjectPath, content);
    }

    private void DrawExistingMessages()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Existing Messages", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                Repaint();
            }
        }

        List<MessageInfo> messages = LoadExistingMessages();
        if (messages.Count == 0)
        {
            EditorGUILayout.HelpBox("No messages found in packets.cs.", MessageType.Warning);
            return;
        }

        DrawMessagesHeader();
        _existingMessagesScroll = EditorGUILayout.BeginScrollView(_existingMessagesScroll, GUILayout.MinHeight(170));
        foreach (MessageInfo message in messages)
        {
            DrawMessageRow(message);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawMessagesHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Direction", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Name", EditorStyles.boldLabel);
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("", GUILayout.Width(70));
        }
    }

    private void DrawMessageRow(MessageInfo message)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(message.Direction.ToString(), GUILayout.Width(80));
            GUILayout.Label(message.Name);
            GUILayout.Label(message.Id.ToString(), GUILayout.Width(70));
            if (GUILayout.Button("Delete", GUILayout.Width(70)))
            {
                DeleteMessage(message);
            }
        }
    }

    private void DeleteMessage(MessageInfo message)
    {
        string className = GetClassName(message.Direction, message.Name);
        bool confirm = EditorUtility.DisplayDialog(
            "Delete Network Message",
            "Delete " + className + "?\n\nThis will remove the enum entry, router registration, message class file, and .csproj include when present.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            string classPath = GetClassPath(message.Direction, message.Name);
            RemoveEnumEntry(message.Direction, message.Name);
            RemoveRouterRegistration(message.Direction, message.Name);
            RemoveCompileInclude(classPath);
            DeleteMessageClassFile(classPath);

            AssetDatabase.Refresh();
            Repaint();
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to delete network message:\n" + ex.Message, "OK");
        }
    }

    private static List<MessageInfo> LoadExistingMessages()
    {
        List<MessageInfo> messages = new List<MessageInfo>();
        if (!File.Exists(PacketsPath))
        {
            return messages;
        }

        string content = File.ReadAllText(PacketsPath);
        AddEnumMessages(content, MessageDirection.Server, messages);
        AddEnumMessages(content, MessageDirection.Client, messages);
        AddEnumMessages(content, MessageDirection.Both, messages);
        messages.Sort((left, right) =>
        {
            int directionCompare = left.Direction.CompareTo(right.Direction);
            return directionCompare != 0 ? directionCompare : string.CompareOrdinal(left.Name, right.Name);
        });
        return messages;
    }

    private static void AddEnumMessages(string content, MessageDirection direction, List<MessageInfo> messages)
    {
        string enumName = GetEnumName(direction);
        Match match = Regex.Match(content, @"public\s+enum\s+" + enumName + @"\s*\{(?<body>.*?)\}", RegexOptions.Singleline);
        if (!match.Success)
        {
            return;
        }

        MatchCollection entries = Regex.Matches(match.Groups["body"].Value, @"(?<name>[A-Z_a-z][A-Z_a-z0-9]*)\s*=\s*(?<id>\d+)");
        foreach (Match entry in entries)
        {
            if (!int.TryParse(entry.Groups["id"].Value, out int id))
            {
                continue;
            }

            messages.Add(new MessageInfo(direction, entry.Groups["name"].Value, id));
        }
    }

    private static void RemoveEnumEntry(MessageDirection direction, string messageName)
    {
        if (!File.Exists(PacketsPath))
        {
            return;
        }

        string enumName = GetEnumName(direction);
        string content = File.ReadAllText(PacketsPath);
        Match enumMatch = Regex.Match(content, @"public\s+enum\s+" + enumName + @"\s*\{(?<body>.*?)\}", RegexOptions.Singleline);
        if (!enumMatch.Success)
        {
            return;
        }

        string body = enumMatch.Groups["body"].Value;
        string updatedBody = Regex.Replace(
            body,
            @"\r?\n\s*" + Regex.Escape(messageName) + @"\s*=\s*\d+\s*,?",
            "");

        if (updatedBody == body)
        {
            return;
        }

        content = content.Substring(0, enumMatch.Groups["body"].Index) +
                  updatedBody +
                  content.Substring(enumMatch.Groups["body"].Index + enumMatch.Groups["body"].Length);
        File.WriteAllText(PacketsPath, content);
    }

    private static void RemoveRouterRegistration(MessageDirection direction, string messageName)
    {
        if (!File.Exists(RouterPath))
        {
            return;
        }

        string enumName = GetEnumName(direction);
        string className = GetClassName(direction, messageName);
        string content = File.ReadAllText(RouterPath);
        string pattern = @"\r?\n\s*\{\s*\(int\)packets\." +
                         Regex.Escape(enumName) +
                         @"\." +
                         Regex.Escape(messageName) +
                         @"\s*,\s*" +
                         Regex.Escape(className) +
                         @"\.Read\s*\}\s*,?";

        string updated = Regex.Replace(content, pattern, "");
        if (updated != content)
        {
            File.WriteAllText(RouterPath, updated);
        }
    }

    private static void RemoveCompileInclude(string classPath)
    {
        if (!File.Exists(RuntimeProjectPath))
        {
            return;
        }

        string includePath = classPath.Replace("/", "\\");
        string content = File.ReadAllText(RuntimeProjectPath);
        string pattern = @"\r?\n\s*<Compile Include=""" + Regex.Escape(includePath) + @""" />\s*";
        string updated = Regex.Replace(content, pattern, "\n");
        if (updated != content)
        {
            File.WriteAllText(RuntimeProjectPath, updated);
        }
    }

    private static void DeleteMessageClassFile(string classPath)
    {
        if (!File.Exists(classPath))
        {
            return;
        }

        if (!AssetDatabase.DeleteAsset(classPath))
        {
            File.Delete(classPath);
            string metaPath = classPath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
    }

    private readonly struct MessageInfo
    {
        public readonly MessageDirection Direction;
        public readonly string Name;
        public readonly int Id;

        public MessageInfo(MessageDirection direction, string name, int id)
        {
            Direction = direction;
            Name = name;
            Id = id;
        }
    }
}
