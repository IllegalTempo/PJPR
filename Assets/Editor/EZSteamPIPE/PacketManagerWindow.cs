using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class PacketManagerWindow : EditorWindow
{
    private class PacketRow
    {
        public string Scope; // "ServerPackets" or "ClientPackets"
        public string Name;  // Enum member name
        public List<string> Senders = new List<string>();
        public List<string> Handlers = new List<string>();
        public string FullName => $"{Scope}.{Name}";
    }

    private enum Direction { ServerToClient, ClientToServer }

    private Vector2 _scroll;
    private string _search = string.Empty;
    private List<PacketRow> _serverPacketRows = new List<PacketRow>(); // Packets sent by server, handled by client
    private List<PacketRow> _clientPacketRows = new List<PacketRow>(); // Packets sent by client, handled by server
    private double _lastParseTime;

    // Add Packet UI state
    private bool _showAddUI = false;
    private Direction _newDirection = Direction.ServerToClient;
    private string _newPacketName = string.Empty;
    private string _newSendMethod = string.Empty;
    private string _newHandleMethod = string.Empty;
    private string _addError = string.Empty;

    [MenuItem("Tools/Packet Manager")]
    public static void ShowWindow()
    {
        GetWindow<PacketManagerWindow>("Packet Manager");
    }

    void OnGUI()
    {
        GUILayout.Label("Packet Viewer", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                TryParseAll();
            }
            GUILayout.Space(8);
            _search = EditorGUILayout.TextField("Search", _search);
            GUILayout.FlexibleSpace();
        }

        // Add Packet Section
        _showAddUI = EditorGUILayout.Foldout(_showAddUI, "Add Packet (enum + send + handle + registration)", true);
        if (_showAddUI)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var prevDir = _newDirection;
                _newDirection = (Direction)EditorGUILayout.EnumPopup("Direction", _newDirection);
                if (prevDir != _newDirection)
                {
                    UpdateSuggestedNames();
                }

                string nameInput = EditorGUILayout.TextField("Packet Name", _newPacketName);
                if (nameInput != _newPacketName)
                {
                    _newPacketName = SanitizePacketName(nameInput);
                    UpdateSuggestedNames();
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    _newSendMethod = EditorGUILayout.TextField("Send Method", _newSendMethod);
                    _newHandleMethod = EditorGUILayout.TextField("Handle Method", _newHandleMethod);
                }

                if (!string.IsNullOrEmpty(_addError))
                {
                    EditorGUILayout.HelpBox(_addError, MessageType.Error);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Add Packet", GUILayout.Width(140)))
                    {
                        TryAddPacket();
                    }
                }
            }
        }

        if (_serverPacketRows.Count == 0 && _clientPacketRows.Count == 0)
        {
            if ((EditorApplication.timeSinceStartup - _lastParseTime) > 0.5f)
            {
                TryParseAll();
            }
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSectionHeader("Server -> Client (ServerPackets)");
        DrawTableHeader();
        foreach (var row in Filter(_serverPacketRows))
        {
            DrawRow(row);
        }

        GUILayout.Space(12);

        DrawSectionHeader("Client -> Server (ClientPackets)");
        DrawTableHeader();
        foreach (var row in Filter(_clientPacketRows))
        {
            DrawRow(row);
        }

        EditorGUILayout.EndScrollView();
    }

    private void UpdateSuggestedNames()
    {
        var baseName = string.IsNullOrWhiteSpace(_newPacketName) ? "MyPacket" : _newPacketName;
        if (_newDirection == Direction.ServerToClient)
        {
            _newSendMethod = $"Server_Send_{baseName}";
            _newHandleMethod = $"Client_Handle_{baseName}";
        }
        else
        {
            _newSendMethod = $"Client_Send_{baseName}";
            _newHandleMethod = $"Server_Handle_{baseName}";
        }
    }

    private static string SanitizePacketName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        // Keep alphanumerics and underscore, remove others, and ensure it starts with a letter/underscore
        string cleaned = Regex.Replace(name, "[^a-zA-Z0-9_]", "");
        if (string.IsNullOrEmpty(cleaned)) return string.Empty;
        if (!char.IsLetter(cleaned[0]) && cleaned[0] != '_') cleaned = "_" + cleaned;
        return cleaned;
    }

    private void TryAddPacket()
    {
        _addError = string.Empty;
        if (string.IsNullOrWhiteSpace(_newPacketName))
        {
            _addError = "Packet name is required.";
            return;
        }

        string packetSendPath, packetHandlesPath, gameClientPath, gameServerPath;
        var packetSendText = ReadScriptTextByName("PacketSend", out packetSendPath);
        var packetHandlesText = ReadScriptTextByName("PacketHandles", out packetHandlesPath);
        var gameClientText = ReadScriptTextByName("GameClient", out gameClientPath);
        var gameServerText = ReadScriptTextByName("GameServer", out gameServerPath);

        if (string.IsNullOrEmpty(packetSendText) || string.IsNullOrEmpty(packetHandlesText)
            || string.IsNullOrEmpty(gameClientText) || string.IsNullOrEmpty(gameServerText))
        {
            _addError = "Required scripts not found. Ensure PacketSend, PacketHandles, GameClient, and GameServer exist.";
            return;
        }

        bool serverDirection = _newDirection == Direction.ServerToClient;
        string scope = serverDirection ? "ServerPackets" : "ClientPackets";
        string enumMember = _newPacketName;

        // 1) Update enum in PacketSend
        if (EnumMemberExists(packetSendText, scope, enumMember))
        {
            // ok; don't add duplicate
        }
        else
        {
            if (!InsertEnumMember(ref packetSendText, scope, enumMember))
            {
                _addError = $"Failed to insert enum member into {scope}.";
                return;
            }
        }

        // 2) Add send method in PacketSend
        if (!MethodExists(packetSendText, _newSendMethod))
        {
            if (!InsertSendMethod(ref packetSendText, scope, enumMember, _newSendMethod, serverDirection))
            {
                _addError = "Failed to insert send method.";
                return;
            }
        }

        // 3) Add handle method in PacketHandles_Method
        if (!MethodExists(packetHandlesText, _newHandleMethod))
        {
            if (!InsertHandleMethod(ref packetHandlesText, _newHandleMethod, serverDirection))
            {
                _addError = "Failed to insert handle method.";
                return;
            }
        }

        // 4) Register in GameClient/GameServer dictionaries
        if (serverDirection)
        {
            // ServerPackets -> client handles in GameClient.ClientPacketHandles
            if (!RegisterHandlerMapping(ref gameClientText, true, scope, enumMember, _newHandleMethod))
            {
                _addError = "Failed to register handler in GameClient.";
                return;
            }
        }
        else
        {
            // ClientPackets -> server handles in GameServer.ServerPacketHandles
            if (!RegisterHandlerMapping(ref gameServerText, false, scope, enumMember, _newHandleMethod))
            {
                _addError = "Failed to register handler in GameServer.";
                return;
            }
        }

        // Write all files back
        if (!WriteAll(
            (packetSendPath, packetSendText),
            (packetHandlesPath, packetHandlesText),
            (gameClientPath, gameClientText),
            (gameServerPath, gameServerText)))
        {
            _addError = "Failed to write modified scripts.";
            return;
        }

        AssetDatabase.Refresh();
        TryParseAll();
    }

    private bool WriteAll(params (string path, string content)[] files)
    {
        try
        {
            foreach (var f in files)
            {
                if (string.IsNullOrEmpty(f.path)) return false;
                File.WriteAllText(f.path, f.content);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            return false;
        }
    }

    private static bool EnumMemberExists(string text, string enumName, string member)
    {
        var names = GetEnumNamesFromText(text, enumName);
        return names.Contains(member);
    }

    private static HashSet<string> GetEnumNamesFromText(string text, string enumName)
    {
        var result = new HashSet<string>();
        int idx = text.IndexOf($"public enum {enumName}", StringComparison.Ordinal);
        if (idx < 0) return result;
        int braceStart = text.IndexOf('{', idx);
        if (braceStart < 0) return result;
        int depth = 0;
        int i = braceStart;
        for (; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) return result;
        string content = text.Substring(braceStart + 1, i - braceStart - 2);
        var rx = new Regex(@"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(=\s*[^,\n]+)?\s*,?", RegexOptions.Multiline);
        foreach (Match m in rx.Matches(content))
        {
            var name = m.Groups["name"].Value.Trim();
            if (!string.IsNullOrEmpty(name)) result.Add(name);
        }
        return result;
    }

    private static bool InsertEnumMember(ref string text, string enumName, string member)
    {
        int idx = text.IndexOf($"public enum {enumName}", StringComparison.Ordinal);
        if (idx < 0) return false;
        int braceStart = text.IndexOf('{', idx);
        if (braceStart < 0) return false;
        int depth = 0;
        int i = braceStart;
        for (; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) return false;
        // insert before the closing '}' (which is at i-1). Prefer before a line with '};'
        int insertPos = i - 1;
        // find previous non-whitespace position to decide if a trailing comma exists; always add a new line with comma
        string toInsert = "\n        " + member + ",";
        text = text.Insert(insertPos, toInsert);
        return true;
    }

    private static bool MethodExists(string text, string methodName)
    {
        return Regex.IsMatch(text, "\\b" + Regex.Escape(methodName) + "\\b");
    }

    private static bool InsertSendMethod(ref string text, string scope, string enumMember, string methodName, bool serverDirection)
    {
        string body;
        if (serverDirection)
        {
            body = "\n    public static Result " + methodName + "()\n" +
                   "    {\n" +
                   "        using (packet p = new packet((int)" + scope + "." + enumMember + "))\n" +
                   "        {\n" +
                   "            // TODO: Write payload\n" +
                   "            return BroadcastPacket(p);\n" +
                   "        }\n" +
                   "    }\n";
        }
        else
        {
            body = "\n    public static Result " + methodName + "()\n" +
                   "    {\n" +
                   "        using (packet p = new packet((int)" + scope + "." + enumMember + "))\n" +
                   "        {\n" +
                   "            // TODO: Write payload\n" +
                   "            return SendToServer(p);\n" +
                   "        }\n" +
                   "    }\n";
        }

        // Insert before end of class PacketSend specifically
        int classEnd = FindClassEndIndex(text, "PacketSend");
        if (classEnd < 0) return false;
        text = text.Insert(classEnd, body);
        return true;
    }

    private static bool InsertHandleMethod(ref string text, string methodName, bool serverDirection)
    {
        string sig = serverDirection
            ? "public static void " + methodName + "(Connection c, packet packet)"
            : "public static void " + methodName + "(NetworkPlayer p, packet packet)";

        string body = "\n    " + sig + "\n" +
                      "    {\n" +
                      "        // TODO: Read payload\n" +
                      "        Debug.Log(\"" + methodName + " called\");\n" +
                      "    }\n";

        // Append before end of class PacketHandles_Method
        int classEnd = FindClassEndIndex(text, "PacketHandles_Method");
        if (classEnd < 0) return false;
        text = text.Insert(classEnd, body);
        return true;
    }

    private static int FindClassEndIndex(string text, string className)
    {
        // Find "class {className}" and match braces to locate its closing '}' index suitable for insertion
        int classIdx = text.IndexOf("class " + className, StringComparison.Ordinal);
        if (classIdx < 0) return -1;
        int braceStart = text.IndexOf('{', classIdx);
        if (braceStart < 0) return -1;
        int depth = 0;
        for (int i = braceStart; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    // Insert before this '}'
                    return i;
                }
            }
        }
        return -1;
    }

    private static bool RegisterHandlerMapping(ref string managerText, bool isClient, string scope, string enumMember, string handlerMethod)
    {
        // Find dictionary block
        var dictName = isClient ? "ClientPacketHandles" : "ServerPacketHandles";
        int dictIndex = managerText.IndexOf(dictName, StringComparison.Ordinal);
        if (dictIndex < 0) return false;
        int newDictIndex = managerText.IndexOf("new Dictionary", dictIndex, StringComparison.Ordinal);
        if (newDictIndex < 0) return false;
        int braceStart = managerText.IndexOf('{', newDictIndex);
        if (braceStart < 0) return false;
        int depth = 0;
        int i = braceStart;
        for (; i < managerText.Length; i++)
        {
            char c = managerText[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) return false;
        string content = managerText.Substring(braceStart, i - braceStart);

        string entry = "{ (int)PacketSend." + scope + "." + enumMember + ",PacketHandles_Method." + handlerMethod + "},";
        if (content.Contains(entry)) return true; // already registered

        // insert before the closing brace of initializer
        int insertPos = i - 1;
        string toInsert = "\n            " + entry;
        managerText = managerText.Insert(insertPos, toInsert);
        return true;
    }

    private IEnumerable<PacketRow> Filter(IEnumerable<PacketRow> rows)
    {
        if (string.IsNullOrWhiteSpace(_search)) return rows;
        var s = _search.Trim();
        return rows.Where(r => r.FullName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                            || r.Senders.Any(m => m.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                            || r.Handlers.Any(m => m.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(6);
        var rect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 1f));
        EditorGUI.LabelField(rect, title, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft });
        GUILayout.Space(4);
    }

    private void DrawTableHeader()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Packet", EditorStyles.miniBoldLabel, GUILayout.Width(260));
            GUILayout.Label("Send Method(s)", EditorStyles.miniBoldLabel, GUILayout.Width(320));
            GUILayout.Label("Handle Method(s)", EditorStyles.miniBoldLabel);
        }
    }

    private void DrawRow(PacketRow row)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(row.FullName, GUILayout.Width(260));
            GUILayout.Label(row.Senders.Count > 0 ? string.Join(", ", row.Senders.Distinct()) : "-", GUILayout.Width(320));
            GUILayout.Label(row.Handlers.Count > 0 ? string.Join(", ", row.Handlers.Distinct()) : "-");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog(
                    "Remove Packet",
                    $"Are you sure you want to remove packet '{row.FullName}'?\n\n" +
                    $"This will:\n" +
                    $"• Remove the enum member from PacketSend\n" +
                    $"• Remove {row.Senders.Count} send method(s)\n" +
                    $"• Remove {row.Handlers.Count} handler method(s)\n" +
                    $"• Unregister the packet from GameClient/GameServer\n\n" +
                    $"This action cannot be undone.",
                    "Remove",
                    "Cancel"))
                {
                    TryRemovePacket(row);
                }
            }
        }
    }

    private void TryParseAll()
    {
        _lastParseTime = EditorApplication.timeSinceStartup;

        string packetSendText = ReadScriptTextByName("PacketSend");
        string gameClientText = ReadScriptTextByName("GameClient");
        string gameServerText = ReadScriptTextByName("GameServer");

        var serverSenders = new Dictionary<string, List<string>>(); // key: ServerPackets.<Name>
        var clientSenders = new Dictionary<string, List<string>>(); // key: ClientPackets.<Name>

        if (!string.IsNullOrEmpty(packetSendText))
        {
            ExtractSendersFromPacketSend(packetSendText, serverSenders, clientSenders);
        }

        var serverHandlers = new Dictionary<string, List<string>>(); // key: ServerPackets.<Name> -> Client handlers
        var clientHandlers = new Dictionary<string, List<string>>(); // key: ClientPackets.<Name> -> Server handlers

        if (!string.IsNullOrEmpty(gameClientText))
        {
            foreach (var pair in ExtractHandlerMapFromManager(gameClientText, isClient: true))
            {
                if (!serverHandlers.TryGetValue(pair.Key, out var list)) { list = new List<string>(); serverHandlers[pair.Key] = list; }
                list.Add(pair.Value);
            }
        }
        if (!string.IsNullOrEmpty(gameServerText))
        {
            foreach (var pair in ExtractHandlerMapFromManager(gameServerText, isClient: false))
            {
                if (!clientHandlers.TryGetValue(pair.Key, out var list)) { list = new List<string>(); clientHandlers[pair.Key] = list; }
                list.Add(pair.Value);
            }
        }

        _serverPacketRows = BuildRows("ServerPackets", GetEnumNamesSafe("ServerPackets"), serverSenders, serverHandlers);
        _clientPacketRows = BuildRows("ClientPackets", GetEnumNamesSafe("ClientPackets"), clientSenders, clientHandlers);
        Repaint();
    }

    private List<PacketRow> BuildRows(string scope, IEnumerable<string> enumNames,
        Dictionary<string, List<string>> senders,
        Dictionary<string, List<string>> handlers)
    {
        var rows = new List<PacketRow>();
        foreach (var name in enumNames)
        {
            var key = $"{scope}.{name}";
            var row = new PacketRow { Scope = scope, Name = name };
            if (senders.TryGetValue(key, out var s)) row.Senders.AddRange(s);
            if (handlers.TryGetValue(key, out var h)) row.Handlers.AddRange(h);
            rows.Add(row);
        }
        // Also include any packets that were found in parsing but not present in enum reflection (fallback)
        foreach (var extra in senders.Keys.Concat(handlers.Keys))
        {
            var parts = extra.Split('.');
            if (parts.Length != 2) continue;
            if (parts[0] != scope) continue;
            if (rows.Any(r => r.Name == parts[1])) continue;
            var row = new PacketRow { Scope = parts[0], Name = parts[1] };
            if (senders.TryGetValue(extra, out var s)) row.Senders.AddRange(s);
            if (handlers.TryGetValue(extra, out var h)) row.Handlers.AddRange(h);
            rows.Add(row);
        }
        rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return rows;
    }

    private static IEnumerable<string> GetEnumNamesSafe(string nestedEnumName)
    {
        try
        {
            var enumType = typeof(PacketSend).GetNestedType(nestedEnumName, BindingFlags.Public);
            if (enumType != null && enumType.IsEnum)
            {
                return Enum.GetNames(enumType);
            }
        }
        catch { }
        return Enumerable.Empty<string>();
    }

    private static void ExtractSendersFromPacketSend(string packetSendText,
        Dictionary<string, List<string>> serverSenders,
        Dictionary<string, List<string>> clientSenders)
    {
        // Find all "new packet((int)ServerPackets.XYZ)" and map to the containing method name
        var rxPacketNew = new Regex(@"new\s+packet\s*\(\s*\(int\)\s*(?<scope>ServerPackets|ClientPackets)\s*\.\s*(?<name>\w+)\s*\)", RegexOptions.Multiline);
        var rxMethod = new Regex(@"public\s+static\s+Result\s+(?<method>\w+)\s*\(", RegexOptions.Multiline);

        var methodMatches = rxMethod.Matches(packetSendText).Cast<Match>().ToList();

        foreach (Match m in rxPacketNew.Matches(packetSendText))
        {
            var scope = m.Groups["scope"].Value;
            var name = m.Groups["name"].Value;
            var idx = m.Index;
            // Find nearest previous method declaration
            string methodName = null;
            for (int i = methodMatches.Count - 1; i >= 0; i--)
            {
                if (methodMatches[i].Index <= idx)
                {
                    methodName = methodMatches[i].Groups["method"].Value;
                    break;
                }
            }
            if (string.IsNullOrEmpty(methodName)) continue;

            var key = $"{scope}.{name}";
            var dict = scope == "ServerPackets" ? serverSenders : clientSenders;
            if (!dict.TryGetValue(key, out var list)) { list = new List<string>(); dict[key] = list; }
            if (!list.Contains(methodName)) list.Add(methodName);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ExtractHandlerMapFromManager(string managerText, bool isClient)
    {
        // Extract the initializer content of the dictionary in GameClient or GameServer
        // GameClient: ClientPacketHandles maps PacketSend.ServerPackets.X -> PacketHandles_Method.Client_Handle_*
        // GameServer: ServerPacketHandles maps PacketSend.ClientPackets.X -> PacketHandles_Method.Server_Handle_*
        var dictName = isClient ? "ClientPacketHandles" : "ServerPacketHandles";
        int dictIndex = managerText.IndexOf(dictName, StringComparison.Ordinal);
        if (dictIndex < 0) yield break;

        int newDictIndex = managerText.IndexOf("new Dictionary", dictIndex, StringComparison.Ordinal);
        if (newDictIndex < 0) yield break;

        int braceStart = managerText.IndexOf('{', newDictIndex);
        if (braceStart < 0) yield break;

        // Find matching closing brace for the initializer
        int depth = 0;
        int i = braceStart;
        for (; i < managerText.Length; i++)
        {
            char c = managerText[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) yield break;

        string content = managerText.Substring(braceStart, i - braceStart);

        var rxEntry = new Regex(@"\{\s*\(int\)\s*PacketSend\s*\.\s*(?<scope>ClientPackets|ServerPackets)\s*\.\s*(?<name>\w+)\s*,\s*PacketHandles_Method\s*\.\s*(?<handler>\w+)\s*\}", RegexOptions.Multiline);
        foreach (Match m in rxEntry.Matches(content))
        {
            var scope = m.Groups["scope"].Value;
            var name = m.Groups["name"].Value;
            var handler = m.Groups["handler"].Value;
            yield return new KeyValuePair<string, string>($"{scope}.{name}", handler);
        }
    }

    private static string ReadScriptTextByName(string className)
    {
        // Prefer AssetDatabase search to be path-agnostic
        var guids = AssetDatabase.FindAssets(className + " t:Script");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(className + ".cs", StringComparison.OrdinalIgnoreCase)) continue;
            var fullPath = ToFullPath(path);
            try
            {
                return File.ReadAllText(fullPath);
            }
            catch { }
        }
        return string.Empty;
    }

    private static string ReadScriptTextByName(string className, out string fullPath)
    {
        fullPath = null;
        var guids = AssetDatabase.FindAssets(className + " t:Script");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(className + ".cs", StringComparison.OrdinalIgnoreCase)) continue;
            fullPath = ToFullPath(path);
            try
            {
                return File.ReadAllText(fullPath);
            }
            catch { }
        }
        return string.Empty;
    }

    private static string ToFullPath(string assetRelativePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    // Removal utilities
    private void TryRemovePacket(PacketRow row)
    {
        _addError = string.Empty;

        string packetSendPath, packetHandlesPath, gameClientPath, gameServerPath;
        var packetSendText = ReadScriptTextByName("PacketSend", out packetSendPath);
        var packetHandlesText = ReadScriptTextByName("PacketHandles", out packetHandlesPath);
        var gameClientText = ReadScriptTextByName("GameClient", out gameClientPath);
        var gameServerText = ReadScriptTextByName("GameServer", out gameServerPath);

        if (string.IsNullOrEmpty(packetSendText) || string.IsNullOrEmpty(packetHandlesText)
            || string.IsNullOrEmpty(gameClientText) || string.IsNullOrEmpty(gameServerText))
        {
            _addError = "Required scripts not found.";
            return;
        }

        string scope = row.Scope;
        string enumMember = row.Name;
        bool serverDirection = scope == "ServerPackets";

        // Collect handler names and unregister mappings
        var handlersToRemove = new List<string>();
        if (serverDirection)
        {
            foreach (var pair in ExtractHandlerMapFromManager(gameClientText, isClient: true))
                if (pair.Key == $"{scope}.{enumMember}") handlersToRemove.Add(pair.Value);
            UnregisterHandlerMapping(ref gameClientText, true, scope, enumMember);
        }
        else
        {
            foreach (var pair in ExtractHandlerMapFromManager(gameServerText, isClient: false))
                if (pair.Key == $"{scope}.{enumMember}") handlersToRemove.Add(pair.Value);
            UnregisterHandlerMapping(ref gameServerText, false, scope, enumMember);
        }
        if (handlersToRemove.Count == 0) handlersToRemove.AddRange(row.Handlers);

        // Remove handler method(s)
        foreach (var h in handlersToRemove.Distinct())
            RemoveMethodByName(ref packetHandlesText, h);

        // Remove send methods and enum member
        RemoveSendMethodsByPacket(ref packetSendText, scope, enumMember);
        RemoveEnumMember(ref packetSendText, scope, enumMember);

        if (!WriteAll(
            (packetSendPath, packetSendText),
            (packetHandlesPath, packetHandlesText),
            (gameClientPath, gameClientText),
            (gameServerPath, gameServerText)))
        {
            _addError = "Failed to write modified scripts.";
            return;
        }

        AssetDatabase.Refresh();
        TryParseAll();
    }

    private static void RemoveEnumMember(ref string text, string enumName, string member)
    {
        int idx = text.IndexOf($"public enum {enumName}", StringComparison.Ordinal);
        if (idx < 0) return;
        int braceStart = text.IndexOf('{', idx);
        if (braceStart < 0) return;
        int depth = 0;
        int i = braceStart;
        for (; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) return;

        int blockStart = braceStart;
        int blockEnd = i;
        string block = text.Substring(blockStart, blockEnd - blockStart);

        string patternLine = @"^[\t ]*" + Regex.Escape(member) + @"\s*(=\s*[^,\n]+)?\s*,?\s*\r?\n";
        string newBlock = Regex.Replace(block, patternLine, string.Empty, RegexOptions.Multiline);
        if (newBlock == block)
        {
            string patternInline = @",\s*" + Regex.Escape(member) + @"\s*(=\s*[^,\n]+)?";
            newBlock = Regex.Replace(block, patternInline, string.Empty);
            if (newBlock == block)
            {
                string patternFallback = Regex.Escape(member) + @"\s*(=\s*[^,\n]+)?\s*,?";
                newBlock = Regex.Replace(block, patternFallback, string.Empty);
            }
        }
        text = text.Remove(blockStart, blockEnd - blockStart).Insert(blockStart, newBlock);
    }

    private static void RemoveSendMethodsByPacket(ref string text, string scope, string enumMember)
    {
        var rxMethod = new Regex(@"public\s+static\s+Result\s+(?<name>\w+)\s*\(", RegexOptions.Multiline);
        var matches = rxMethod.Matches(text).Cast<Match>().ToList();
        for (int mi = matches.Count - 1; mi >= 0; mi--)
        {
            var m = matches[mi];
            int sigIndex = m.Index;
            int brace = text.IndexOf('{', sigIndex);
            if (brace < 0) continue;
            int end = FindMatchingBrace(text, brace);
            if (end < 0) continue;
            string body = text.Substring(brace, end - brace + 1);
            string pattern = @"new\s+packet\s*\(\s*\(int\)\s*" + Regex.Escape(scope) + @"\s*\.\s*" + Regex.Escape(enumMember) + @"\s*\)";
            if (Regex.IsMatch(body, pattern, RegexOptions.Multiline))
            {
                int removeStart = FindLineStart(text, sigIndex);
                text = text.Remove(removeStart, end - removeStart + 1);
            }
        }
    }

    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        int depth = 0;
        for (int i = openBraceIndex; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    private static int FindLineStart(string text, int index)
    {
        int i = index;
        while (i > 0 && text[i - 1] != '\n') i--;
        return i;
    }

    private static void RemoveMethodByName(ref string text, string methodName)
    {
        var rx = new Regex(@"public\s+static\s+(?:async\s+)?void\s+" + Regex.Escape(methodName) + @"\s*\(", RegexOptions.Multiline);
        var m = rx.Match(text);
        if (!m.Success) return;
        int sigIndex = m.Index;
        int brace = text.IndexOf('{', sigIndex);
        if (brace < 0) return;
        int end = FindMatchingBrace(text, brace);
        if (end < 0) return;
        int removeStart = FindLineStart(text, sigIndex);
        text = text.Remove(removeStart, end - removeStart + 1);
    }

    private static void UnregisterHandlerMapping(ref string managerText, bool isClient, string scope, string enumMember)
    {
        var dictName = isClient ? "ClientPacketHandles" : "ServerPacketHandles";
        int dictIndex = managerText.IndexOf(dictName, StringComparison.Ordinal);
        if (dictIndex < 0) return;
        int newDictIndex = managerText.IndexOf("new Dictionary", dictIndex, StringComparison.Ordinal);
        if (newDictIndex < 0) return;
        int braceStart = managerText.IndexOf('{', newDictIndex);
        if (braceStart < 0) return;
        int depth = 0;
        int i = braceStart;
        for (; i < managerText.Length; i++)
        {
            char c = managerText[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) return;
        int blockStart = braceStart;
        int blockEnd = i;
        string block = managerText.Substring(blockStart, blockEnd - blockStart);
        string pattern = @"^[\t ]*\{\s*\(int\)\s*PacketSend\s*\.\s*" + Regex.Escape(scope) + @"\s*\.\s*" + Regex.Escape(enumMember) + @"\s*,\s*PacketHandles_Method\s*\.\s*\w+\s*\}\s*,?\s*\r?\n?";
        string newBlock = Regex.Replace(block, pattern, string.Empty, RegexOptions.Multiline);
        if (newBlock != block)
        {
            managerText = managerText.Remove(blockStart, blockEnd - blockStart).Insert(blockStart, newBlock);
        }
    }
}
