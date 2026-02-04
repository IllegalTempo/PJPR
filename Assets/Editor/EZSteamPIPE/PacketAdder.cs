using Steamworks;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using static FileUtils;

public class PacketAdder
{
    
    public bool AddPacketToEnum(bool isServerPacket, string packetName)
    {
        string enumName = isServerPacket ? "ServerPackets" : "ClientPackets";
        string path = FindFileByClass("packets");
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Error", "Could not find packets.cs file.", "OK");
            return false;
        }

        string content = File.ReadAllText(path);
        var enumPattern = @"public\s+enum\s+" + enumName + @"\s*\{([^}]*)\}";
        var match = Regex.Match(content, enumPattern, RegexOptions.Singleline);
        if (!match.Success)
        {
            EditorUtility.DisplayDialog("Error", "Could not find " + enumName + " enum in packets.cs", "OK");
            return false;
        }
        string enumContent = match.Groups[1].Value;
        if (Regex.IsMatch(enumContent, @"\b" + packetName + @"\b"))
        {
            EditorUtility.DisplayDialog("Error", "Packet '" + packetName + "' already exists in " + enumName + ".", "OK");
            return false;
        }
        int newValue = CalculateNextEnumValue(enumContent);
        content = InsertEnumEntry(content, match, packetName, newValue);
        File.WriteAllText(path, content);
        return true;
    }

    public void GenerateSendMethod(bool isServerPacket, string packetName)
    {
        string className = isServerPacket ? "ServerSend" : "ClientSend";
        string path = FindFileByClass(className);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Could not find " + className + ".cs file");
            return;
        }

        string content = File.ReadAllText(path);
        string methodName = packetName;
        if (content.Contains("public static Result " + methodName + "("))
        {
            Debug.Log("Send method " + methodName + " already exists");
            return;
        }

        string methodCode = GenerateSendMethodCode(isServerPacket, packetName);
        
        // Find the last closing brace of the class
        int lastBraceIndex = content.LastIndexOf('}');
        if (lastBraceIndex > 0)
        {
            content = content.Insert(lastBraceIndex, methodCode);
        }
        else
        {
            content += methodCode; // Fallback: append at end
        }

        File.WriteAllText(path, content);
        Debug.Log("Generated send method: " + methodName);
    }

    public void GenerateHandleMethod(bool isServerPacket, string packetName)
    {
        // Note: isServerPacket means packets FROM server, handled BY client
        string className = isServerPacket ? "ClientHandle" : "ServerHandle";
        string path = FindFileByClass(className);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Could not find " + className + ".cs file");
            return;
        }

        string content = File.ReadAllText(path);
        string methodName = packetName;
        if (content.Contains("public static void " + methodName + "("))
        {
            Debug.Log("Handle method " + methodName + " already exists");
            return;
        }

        string methodCode = GenerateHandleMethodCode(isServerPacket, methodName);
        
        // Find the last closing brace of the class
        int lastBraceIndex = content.LastIndexOf('}');
        if (lastBraceIndex > 0)
        {
            content = content.Insert(lastBraceIndex, methodCode);
        }
        else
        {
            content += methodCode; // Fallback: append at end
        }

        File.WriteAllText(path, content);
        Debug.Log("Generated handle method: " + methodName);
    }

    public void RegisterHandler(bool isServerPacket, string packetName)
    {
        // isServerPacket means packets FROM server, registered in GameClient
        string className = isServerPacket ? "GameClient" : "GameServer";
        string filePath = FindFileByClass(className);

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning("Could not find " + className + ".cs file");
            return;
        }

        string content = File.ReadAllText(filePath);
        string dictName = isServerPacket ? "ClientPacketHandles" : "ServerPacketHandles";
        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        string handleClass = isServerPacket ? "ClientHandle" : "ServerHandle";
        string methodName = handleClass + "." + packetName;

        content = InsertHandlerRegistration(content, dictName, enumType, packetName, methodName);

        File.WriteAllText(filePath, content);
        Debug.Log("Registered handler for " + packetName + " in " + className);
    }

    private int CalculateNextEnumValue(string enumContent)
    {
        var valueMatches = Regex.Matches(enumContent, @"=\s*(\d+)");
        int maxValue = -1;
        foreach (Match m in valueMatches)
        {
            if (int.TryParse(m.Groups[1].Value, out int val) && val > maxValue)
            {
                maxValue = val;
            }
        }
        return maxValue + 1;
    }

    private string InsertEnumEntry(string content, Match enumMatch, string packetName, int value)
    {
        string enumContent = enumMatch.Groups[1].Value;
        string trimmedEnum = enumContent.TrimEnd();
        bool needsComma = trimmedEnum.Length > 0 && !trimmedEnum.EndsWith(",");

        string insertText = (needsComma ? "," : "") + "\n        " + packetName + " = " + value;

        int enumStartPos = enumMatch.Groups[1].Index;
        int enumEndPos = enumStartPos + enumMatch.Groups[1].Length;

        return content.Substring(0, enumEndPos) + insertText + "\n    " + content.Substring(enumEndPos);
    }

    private string GenerateSendMethodCode(bool isServerPacket, string packetName)
    {
        var sb = new StringBuilder();
        sb.AppendLine();

        string enumType = isServerPacket ? "ServerPackets" : "ClientPackets";
        string methodName = packetName;

        if (isServerPacket)
        {
            sb.AppendLine("    public static Result " + methodName + "(NetworkPlayer target)");
            sb.AppendLine("    {");
            sb.AppendLine("        using (packet p = new packet((int)ServerPackets." + packetName + "))");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Write packet data here");
            sb.AppendLine("            // p.Write(...);");
            sb.AppendLine("            ");
            sb.AppendLine("            return target.SendPacket(p);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine("    public static Result " + methodName + "()");
            sb.AppendLine("    {");
            sb.AppendLine("        using (packet p = new packet((int)ClientPackets." + packetName + "))");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Write packet data here");
            sb.AppendLine("            // p.Write(...);");
            sb.AppendLine("            ");
            sb.AppendLine("            return SendToServer(p);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        return sb.ToString();
    }

    private string GenerateHandleMethodCode(bool isServerPacket, string methodName)
    {
        var sb = new StringBuilder();
        sb.AppendLine();

        if (isServerPacket)
        {
            // Client handles server packets
            sb.AppendLine("    public static void " + methodName + "(Connection c, packet packet)");
            sb.AppendLine("    {");
            sb.AppendLine("        // TODO: Read packet data here");
            sb.AppendLine("        // var data = packet.Read...();");
            sb.AppendLine("        ");
            sb.AppendLine("        // TODO: Handle the packet");
            sb.AppendLine("    }");
        }
        else
        {
            // Server handles client packets
            sb.AppendLine("    public static void " + methodName + "(NetworkPlayer p, packet packet)");
            sb.AppendLine("    {");
            sb.AppendLine("        // TODO: Read packet data here");
            sb.AppendLine("        // var data = packet.Read...();");
            sb.AppendLine("        ");
            sb.AppendLine("        // TODO: Handle the packet");
            sb.AppendLine("    }");
        }

        return sb.ToString();
    }

    private string InsertHandlerRegistration(string content, string dictName, string enumType, string packetName, string methodName)
    {
        int dictStart = content.IndexOf(dictName + " = new Dictionary");
        if (dictStart < 0)
        {
            Debug.LogWarning("Could not find " + dictName + " dictionary initialization");
            return content;
        }

        int braceStart = content.IndexOf('{', dictStart);
        if (braceStart < 0) return content;

        int braceEnd = FindMatchingClosingBrace(content, braceStart);
        if (braceEnd < 0)
        {
            Debug.LogWarning("Could not find closing brace for " + dictName);
            return content;
        }

        string dictContent = content.Substring(braceStart + 1, braceEnd - braceStart - 1);
        bool hasEntries = dictContent.Trim().Length > 0 && dictContent.Contains("(int)packets.");

        // Fixed: Add space after comma and before method name for consistency
        string newEntry = (hasEntries ? "," : "") + "\n            { (int)packets." + enumType + "." + packetName + ", " + methodName + " }";

        return content.Substring(0, braceEnd) + newEntry + "\n        " + content.Substring(braceEnd);
    }

    private int FindMatchingClosingBrace(string content, int braceStart)
    {
        int depth = 0;
        for (int i = braceStart; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }
}

