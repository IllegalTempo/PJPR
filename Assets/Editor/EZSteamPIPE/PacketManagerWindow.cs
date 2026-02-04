using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace EZSteamPIPE.PacketManager
{
    #region Data Models
    
    /// <summary>
    /// Represents a packet entry with its metadata
    /// </summary>
    public class PacketRow
    {
        public string Scope; // "ServerPackets" or "ClientPackets"
        public string Name;  // Enum member name
        public List<string> Senders = new List<string>();
        public List<string> Handlers = new List<string>();
        public string FullName => $"{Scope}.{Name}";
    }

    /// <summary>
    /// Result of packet analysis
    /// </summary>
    public class PacketAnalysisResult
    {
        public List<PacketRow> ServerPackets { get; set; } = new List<PacketRow>();
        public List<PacketRow> ClientPackets { get; set; } = new List<PacketRow>();
    }

    #endregion

    #region Main Window
    
    /// <summary>
    /// Main window for viewing and managing network packets
    /// </summary>
    public class PacketManagerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _search = string.Empty;
        private List<PacketRow> _serverPacketRows = new List<PacketRow>();
        private List<PacketRow> _clientPacketRows = new List<PacketRow>();
        private double _lastParseTime;

        private PacketAnalyzer _analyzer;
        
        // Custom background color
        private static readonly Color BackgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color HeaderColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        
        // Textures and icons
        private Texture2D _backgroundTexture;
        private Texture2D _logoTexture;

        [MenuItem("Tools/Packet Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<PacketManagerWindow>("Packet Manager");
            window.minSize = new Vector2(800, 400);
            window.maxSize = new Vector2(1400, 900);
        }

        private void OnEnable()
        {
            _analyzer = new PacketAnalyzer();
            LoadTextures();
        }
        
        private void OnDisable()
        {
            // Clean up textures if they were created dynamically
            if (_backgroundTexture != null && !AssetDatabase.Contains(_backgroundTexture))
            {
                DestroyImmediate(_backgroundTexture);
            }
        }
        
        private void LoadTextures()
        {
             _logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/EZSteamPIPE/logo.png");
            _backgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/EZSteamPIPE/background.png");
        }
        
        void OnGUI()
        {
            // Draw custom background
            DrawCustomBackground();
            
            DrawToolbar();

            if (ShouldRefreshPackets())
            {
                RefreshPacketList();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawServerPacketsSection();
            GUILayout.Space(12);
            DrawClientPacketsSection();

            EditorGUILayout.EndScrollView();
        }

        #region UI Drawing
        
        private void DrawCustomBackground()
        {
            if (Event.current.type == EventType.Repaint)
            {
                var rect = new Rect(0, 0, position.width, position.height);

                if (_backgroundTexture != null)
                {
                    GUI.DrawTextureWithTexCoords(rect, _backgroundTexture,
                        new Rect(0, 0, position.width / 256f, position.height / 256f));
                }
            }
        }

        private void DrawToolbar()
        {
            // Draw toolbar background
            var toolbarRect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(0, 0, position.width, 70), HeaderColor);
            }
            
            GUILayout.Space(5);
            
            // Draw logo/icon next to title
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_logoTexture != null)
                {
                    GUILayout.Label(_logoTexture, GUILayout.Width(48), GUILayout.Height(48));
                }
                GUILayout.Label("Packet Viewer", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                {
                    RefreshPacketList();
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Add Packet", GUILayout.Width(100)))
                {
                    AddPacketWindow.ShowWindow();
                }
                GUILayout.Space(8);
                _search = EditorGUILayout.TextField("Search", _search);
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawServerPacketsSection()
        {
            PacketTableRenderer.DrawSectionHeader("Server -> Client (ServerPackets)");
            PacketTableRenderer.DrawTableHeader();
            foreach (var row in FilterPackets(_serverPacketRows))
            {
                DrawPacketRow(row);
            }
        }

        private void DrawClientPacketsSection()
        {
            PacketTableRenderer.DrawSectionHeader("Client -> Server (ClientPackets)");
            PacketTableRenderer.DrawTableHeader();
            foreach (var row in FilterPackets(_clientPacketRows))
            {
                DrawPacketRow(row);
            }
        }

        private void DrawPacketRow(PacketRow row)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(row.FullName, GUILayout.Width(260));
                GUILayout.Label(row.Senders.Count > 0 ? string.Join(", ", row.Senders.Distinct()) : "-", GUILayout.Width(320));
                GUILayout.Label(row.Handlers.Count > 0 ? string.Join(", ", row.Handlers.Distinct()) : "-");
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (ConfirmRemoval(row))
                    {
                        PacketRemover.RemovePacket(row.Scope, row.Name);
                        RefreshPacketList();
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private bool ShouldRefreshPackets()
        {
            return _serverPacketRows.Count == 0 && 
                   _clientPacketRows.Count == 0 && 
                   (EditorApplication.timeSinceStartup - _lastParseTime) > 0.5f;
        }

        private IEnumerable<PacketRow> FilterPackets(IEnumerable<PacketRow> rows)
        {
            if (string.IsNullOrWhiteSpace(_search)) return rows;
            
            var searchTerm = _search.Trim();
            return rows.Where(r => 
                r.FullName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                r.Senders.Any(m => m.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                r.Handlers.Any(m => m.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private bool ConfirmRemoval(PacketRow row)
        {
            return EditorUtility.DisplayDialog("Confirm Removal", 
                $"Are you sure you want to remove packet '{row.Name}'?\n\n" +
                "This will remove:\n" +
                "- Enum entry\n" +
                "- Send method(s)\n" +
                "- Handle method(s)\n" +
                "- Handler registration(s)", 
                "Remove", "Cancel");
        }

        private void RefreshPacketList()
        {
            _lastParseTime = EditorApplication.timeSinceStartup;
            
            var analysisResult = _analyzer.AnalyzePackets();
            _serverPacketRows = analysisResult.ServerPackets;
            _clientPacketRows = analysisResult.ClientPackets;
            
            Repaint();
        }

        #endregion
    }

    #endregion

    #region UI Renderers
    
    /// <summary>
    /// Handles rendering of packet table UI elements
    /// </summary>
    public static class PacketTableRenderer
    {
        public static void DrawSectionHeader(string title)
        {
            GUILayout.Space(6);
            var rect = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 1f));
            EditorGUI.LabelField(rect, title, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft });
            GUILayout.Space(4);
        }

        public static void DrawTableHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Packet", EditorStyles.miniBoldLabel, GUILayout.Width(260));
                GUILayout.Label("Send Method(s)", EditorStyles.miniBoldLabel, GUILayout.Width(320));
                GUILayout.Label("Handle Method(s)", EditorStyles.miniBoldLabel);
                GUILayout.Space(20); // Space for Remove button
            }
        }
    }

    #endregion

    #region Packet Analyzer
    
    /// <summary>
    /// Analyzes packet code files to extract packet information
    /// </summary>
    public class PacketAnalyzer
    {
        public PacketAnalysisResult AnalyzePackets()
        {
            string serverSendText = FileUtils.ReadScriptTextByName("ServerSend");
            string clientSendText = FileUtils.ReadScriptTextByName("ClientSend");

            string gameClientText = FileUtils.ReadScriptTextByName("GameClient");
            string gameServerText = FileUtils.ReadScriptTextByName("GameServer");

            var serverSenders = new Dictionary<string, List<string>>();
            var clientSenders = new Dictionary<string, List<string>>();

            if (!string.IsNullOrEmpty(serverSendText))
            {
                ExtractSendersFromSendClass(serverSendText, serverSenders, true);
            }
            
            if (!string.IsNullOrEmpty(clientSendText))
            {
                ExtractSendersFromSendClass(clientSendText, clientSenders, false);
            }

            var serverHandlers = new Dictionary<string, List<string>>();
            var clientHandlers = new Dictionary<string, List<string>>();

            if (!string.IsNullOrEmpty(gameClientText))
            {
                foreach (var pair in ExtractHandlerMapFromManager(gameClientText, isClient: true))
                {
                    if (!serverHandlers.TryGetValue(pair.Key, out var list))
                    {
                        list = new List<string>();
                        serverHandlers[pair.Key] = list;
                    }
                    list.Add(pair.Value);
                }
            }

            if (!string.IsNullOrEmpty(gameServerText))
            {
                foreach (var pair in ExtractHandlerMapFromManager(gameServerText, isClient: false))
                {
                    if (!clientHandlers.TryGetValue(pair.Key, out var list))
                    {
                        list = new List<string>();
                        clientHandlers[pair.Key] = list;
                    }
                    list.Add(pair.Value);
                }
            }

            return new PacketAnalysisResult
            {
                ServerPackets = BuildRows("ServerPackets", GetEnumNamesSafe("ServerPackets"), serverSenders, serverHandlers),
                ClientPackets = BuildRows("ClientPackets", GetEnumNamesSafe("ClientPackets"), clientSenders, clientHandlers)
            };
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
            
            // Include any packets found in parsing but not in enum reflection
            foreach (var extra in senders.Keys.Concat(handlers.Keys))
            {
                var parts = extra.Split('.');
                if (parts.Length != 2 || parts[0] != scope) continue;
                if (rows.Any(r => r.Name == parts[1])) continue;
                
                var row = new PacketRow { Scope = parts[0], Name = parts[1] };
                if (senders.TryGetValue(extra, out var s)) row.Senders.AddRange(s);
                if (handlers.TryGetValue(extra, out var h)) row.Handlers.AddRange(h);
                rows.Add(row);
            }
            
            rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return rows;
        }

        private IEnumerable<string> GetEnumNamesSafe(string nestedEnumName)
        {
            try
            {
                var enumType = typeof(packets).GetNestedType(nestedEnumName, BindingFlags.Public);
                if (enumType != null && enumType.IsEnum)
                {
                    return Enum.GetNames(enumType);
                }
            }
            catch { }
            return Enumerable.Empty<string>();
        }

        private void ExtractSendersFromSendClass(string sendClassText,
            Dictionary<string, List<string>> senders, bool isServerPacket)
        {
            // Match patterns like: 
            // 1. new packet((int)ServerPackets.XYZ) - with "using static packets;"
            // 2. new packet((int)packets.ServerPackets.XYZ) - fully qualified
            var rxPacketNew = new Regex(@"new\s+packet\s*\(\s*\(int\)\s*(?:packets\s*\.)?\s*(?<scope>ServerPackets|ClientPackets)\s*\.\s*(?<name>\w+)\s*\)", RegexOptions.Multiline);
            var rxMethod = new Regex(@"public\s+static\s+Result\s+(?<method>\w+)\s*\(", RegexOptions.Multiline);

            var methodMatches = rxMethod.Matches(sendClassText).Cast<Match>().ToList();

            foreach (Match m in rxPacketNew.Matches(sendClassText))
            {
                var scope = m.Groups["scope"].Value;
                var name = m.Groups["name"].Value;
                var idx = m.Index;
                
                string methodName = FindNearestPreviousMethod(methodMatches, idx);
                if (string.IsNullOrEmpty(methodName)) continue;

                var key = $"{scope}.{name}";
                if (!senders.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    senders[key] = list;
                }
                if (!list.Contains(methodName)) list.Add(methodName);
            }
        }

        private string FindNearestPreviousMethod(List<Match> methodMatches, int position)
        {
            for (int i = methodMatches.Count - 1; i >= 0; i--)
            {
                if (methodMatches[i].Index <= position)
                {
                    return methodMatches[i].Groups["method"].Value;
                }
            }
            return null;
        }

        private IEnumerable<KeyValuePair<string, string>> ExtractHandlerMapFromManager(string managerText, bool isClient)
        {
            var dictName = isClient ? "ClientPacketHandles" : "ServerPacketHandles";
            int dictIndex = managerText.IndexOf(dictName, StringComparison.Ordinal);
            if (dictIndex < 0) yield break;

            int newDictIndex = managerText.IndexOf("new Dictionary", dictIndex, StringComparison.Ordinal);
            if (newDictIndex < 0) yield break;

            int braceStart = managerText.IndexOf('{', newDictIndex);
            if (braceStart < 0) yield break;

            string content = ExtractDictionaryContent(managerText, braceStart);
            if (string.IsNullOrEmpty(content)) yield break;

            // Match patterns like: { (int)packets.ServerPackets.XYZ, ClientHandle.MethodName }
            var rxEntry = new Regex(@"\{\s*\(int\)\s*packets\s*\.\s*(?<scope>ClientPackets|ServerPackets)\s*\.\s*(?<name>\w+)\s*,\s*(?:ClientHandle|ServerHandle)\s*\.\s*(?<handler>\w+)\s*\}", RegexOptions.Multiline);
            foreach (Match m in rxEntry.Matches(content))
            {
                var scope = m.Groups["scope"].Value;
                var name = m.Groups["name"].Value;
                var handler = m.Groups["handler"].Value;
                yield return new KeyValuePair<string, string>($"{scope}.{name}", handler);
            }
        }

        private string ExtractDictionaryContent(string text, int braceStart)
        {
            int depth = 0;
            int i = braceStart;
            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++;
                        break;
                    }
                }
            }
            
            if (depth != 0) return string.Empty;
            return text.Substring(braceStart, i - braceStart);
        }
    }

    #endregion

    

}

