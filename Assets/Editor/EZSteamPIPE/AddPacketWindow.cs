using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class AddPacketWindow : EditorWindow
{
    private string _packetName = "";
    private int _packetTypeIndex = 0;
    private readonly string[] _packetTypes = { "ServerPackets (Server -> Client)", "ClientPackets (Client -> Server)" };
    private bool _generateSendMethod = true;
    private bool _generateHandleMethod = true;
    private bool _registerHandler = true;

    public static void ShowWindow()
    {
        var window = GetWindow<AddPacketWindow>("Add New Packet");
        window.minSize = new Vector2(400, 250);
        window.maxSize = new Vector2(400, 250);
    }

    void OnGUI()
    {
        GUILayout.Label("Add New Packet", EditorStyles.boldLabel);
        GUILayout.Space(10);

        _packetName = EditorGUILayout.TextField("Packet Name:", _packetName);
        GUILayout.Space(5);

        _packetTypeIndex = EditorGUILayout.Popup("Packet Type:", _packetTypeIndex, _packetTypes);
        GUILayout.Space(10);

        GUILayout.Label("Options:", EditorStyles.boldLabel);
        _generateSendMethod = EditorGUILayout.Toggle("Generate Send Method Template", _generateSendMethod);
        _generateHandleMethod = EditorGUILayout.Toggle("Generate Handle Method Template", _generateHandleMethod);
        _registerHandler = EditorGUILayout.Toggle("Register Handler in Manager", _registerHandler);

        GUILayout.Space(20);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_packetName);
            if (GUILayout.Button("Add Packet", GUILayout.Width(100)))
            {
                AddPacket();
            }
            GUI.enabled = true;
        }
    }

    private void AddPacket()
    {
        if (!ValidatePacketName())
        {
            return;
        }

        bool isServerPacket = _packetTypeIndex == 0;

        try
        {
            var adder = new PacketAdder();

            if (!adder.AddPacketToEnum(isServerPacket, _packetName))
            {
                return;
            }

            if (_generateSendMethod)
            {
                adder.GenerateSendMethod(isServerPacket, _packetName);
            }

            if (_generateHandleMethod)
            {
                adder.GenerateHandleMethod(isServerPacket, _packetName);
            }

            if (_registerHandler && _generateHandleMethod)
            {
                adder.RegisterHandler(isServerPacket, _packetName);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Packet '" + _packetName + "' added successfully!", "OK");
            Close();
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to add packet: " + ex.Message, "OK");
        }
    }

    private bool ValidatePacketName()
    {
        if (string.IsNullOrWhiteSpace(_packetName))
        {
            EditorUtility.DisplayDialog("Error", "Packet name cannot be empty.", "OK");
            return false;
        }

        _packetName = _packetName.Trim();
        if (!Regex.IsMatch(_packetName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            EditorUtility.DisplayDialog("Error", "Packet name must be a valid C# identifier.", "OK");
            return false;
        }

        return true;
    }
}

/// <summary>
/// Handles adding new packets to the codebase
/// </summary>
