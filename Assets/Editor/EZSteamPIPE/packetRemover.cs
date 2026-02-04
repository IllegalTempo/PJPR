using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using static FileUtils;
public class PacketRemover
{
    public static void RemovePacket(string scope, string packetName)
    {
        bool isServerPacket = scope == "ServerPackets";

        try
        {
            bool anyRemoved = false;

            if (RemoveFromEnum(isServerPacket, packetName))
            {
                anyRemoved = true;
            }

            if (RemoveSendMethods(packetName, isServerPacket))
            {
                anyRemoved = true;
            }

            if (RemoveHandleMethods(packetName, isServerPacket))
            {
                anyRemoved = true;
            }

            if (UnregisterHandler(packetName, isServerPacket))
            {
                anyRemoved = true;
            }

            if (anyRemoved)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", "Packet '" + packetName + "' removed successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "No references to packet '" + packetName + "' were found.", "OK");
            }
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to remove packet: " + ex.Message, "OK");
        }
    }

    private static bool RemoveFromEnum(bool isServerPacket, string packetName)
    {
        string filePath = FindFileByClass("packets");
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        string content = File.ReadAllText(filePath);

        var entryPattern = @",?\s*" + packetName + @"\s*=\s*\d+\s*,?";
        var match = Regex.Match(content, entryPattern);

        if (match.Success)
        {
            string before = content.Substring(0, match.Index);
            string after = content.Substring(match.Index + match.Length);

            content = before + after;
            content = Regex.Replace(content, @",\s*,", ",");
            content = Regex.Replace(content, @",(\s*)\}", "$1}");

            File.WriteAllText(filePath, content);
            return true;
        }

        return false;
    }

    private static bool RemoveSendMethods(string packetName, bool isServerPacket)
    {
        string className = isServerPacket ? "ServerSend" : "ClientSend";
        string filePath = FindFileByClass(className);
        if (string.IsNullOrEmpty(filePath)) return false;

        string content = File.ReadAllText(filePath);
        bool removed = false;

        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";

        // Match methods that create packets with this packet type
        // Handle both "ServerPackets.XYZ" and "packets.ServerPackets.XYZ" patterns
        var methodPattern = @"public\s+static\s+Result\s+" + packetName + @"\s*\([^)]*\)\s*\{[^}]*new\s+packet\s*\(\s*\(int\)\s*(?:packets\s*\.)?" +
                           enumType + @"\." + packetName + @"[^}]*\}\s*;?\s*\}";

        var matches = Regex.Matches(content, methodPattern, RegexOptions.Singleline);

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            int startIndex = match.Index;
            while (startIndex > 0 && char.IsWhiteSpace(content[startIndex - 1]))
            {
                startIndex--;
            }

            content = content.Remove(startIndex, match.Index + match.Length - startIndex);
            removed = true;
        }

        if (removed)
        {
            File.WriteAllText(filePath, content);
        }

        return removed;
    }

    private static bool RemoveHandleMethods(string packetName, bool isServerPacket)
    {
        // isServerPacket means packets FROM server, handled BY client
        string className = isServerPacket ? "ClientHandle" : "ServerHandle";
        string filePath = FindFileByClass(className);
        if (string.IsNullOrEmpty(filePath)) return false;

        string content = File.ReadAllText(filePath);
        string methodName = packetName;

        var methodPattern = @"public\s+static\s+(async\s+)?void\s+" + methodName + @"\s*\([^)]*\)\s*\{";
        var match = Regex.Match(content, methodPattern);

        if (match.Success)
        {
            int braceCount = 1;
            int searchIndex = match.Index + match.Length;
            int endIndex = -1;

            while (searchIndex < content.Length && braceCount > 0)
            {
                if (content[searchIndex] == '{') braceCount++;
                else if (content[searchIndex] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        endIndex = searchIndex + 1;
                        break;
                    }
                }
                searchIndex++;
            }

            if (endIndex > 0)
            {
                int startIndex = match.Index;
                while (startIndex > 0 && char.IsWhiteSpace(content[startIndex - 1]))
                {
                    startIndex--;
                }

                content = content.Remove(startIndex, endIndex - startIndex);
                File.WriteAllText(filePath, content);
                return true;
            }
        }

        return false;
    }

    private static bool UnregisterHandler(string packetName, bool isServerPacket)
    {
        string className = isServerPacket ? "GameClient" : "GameServer";
        string filePath = FindFileByClass(className);

        if (string.IsNullOrEmpty(filePath)) return false;

        string content = File.ReadAllText(filePath);
        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        string handleType = isServerPacket ? "ClientHandle" : "ServerHandle";
        string methodName = packetName;

        var entryPattern = @"\{\s*\(int\)\s*packets\." + enumType + @"\." + packetName +
                          @"\s*,\s*" + handleType + @"\." + methodName + @"\s*\}\s*,?";

        var match = Regex.Match(content, entryPattern);

        if (match.Success)
        {
            string before = content.Substring(0, match.Index);
            string after = content.Substring(match.Index + match.Length);

            bool removePrecedingComma = before.TrimEnd().EndsWith(",");
            if (removePrecedingComma && !after.TrimStart().StartsWith("}"))
            {
                removePrecedingComma = false;
            }
            else if (removePrecedingComma)
            {
                before = before.TrimEnd();
                before = before.Substring(0, before.Length - 1) + "\n            ";
            }

            content = before + after;
            content = Regex.Replace(content, @",\s*,", ",");

            File.WriteAllText(filePath, content);
            return true;
        }

        return false;
    }

    
}

