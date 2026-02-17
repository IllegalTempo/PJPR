using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerSettingsMenu : MonoBehaviour
{
    private const string FunctionInteractKeyPref = "functionInteractKey";

    [Header("Function Interaction Keybind")]
    [SerializeField] private Key defaultFunctionInteractKey = Key.F;

    private readonly Rect settingsPanelRect = new Rect(0f, 0f, 420f, 240f);
    private readonly Color settingsOverlayColor = new Color(0f, 0f, 0f, 0.58f);

    private bool settingsMenuOpen = false;
    private bool waitingForFunctionKey = false;
    private PlayerMain playerMain;

    public bool IsMenuOpen => settingsMenuOpen;

    public static Key CurrentFunctionInteractKey { get; private set; } = Key.F;
    private static bool keybindLoaded = false;

    private void Awake()
    {
        playerMain = GetComponent<PlayerMain>();
        EnsureKeybindLoaded(defaultFunctionInteractKey);
    }

    private void Update()
    {
        if (!IsLocalPlayer())
        {
            return;
        }

        HandleMenuToggleInput();
        HandleFunctionKeyRebindInput();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsLocalPlayer())
        {
            OpenSettingsMenu();
        }
    }

    private void OnGUI()
    {
        if (!IsLocalPlayer() || !settingsMenuOpen)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = settingsOverlayColor;
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
        GUI.color = previousColor;

        float panelX = (Screen.width - settingsPanelRect.width) * 0.5f;
        float panelY = (Screen.height - settingsPanelRect.height) * 0.5f;
        Rect centeredRect = new Rect(panelX, panelY, settingsPanelRect.width, settingsPanelRect.height);

        GUILayout.BeginArea(centeredRect, "Settings", GUI.skin.window);
        DrawSettingsPanel();
        GUILayout.EndArea();
    }

    public static Key GetFunctionInteractKey()
    {
        EnsureKeybindLoaded(Key.F);
        return CurrentFunctionInteractKey;
    }

    public static string GetFunctionInteractKeyLabel()
    {
        EnsureKeybindLoaded(Key.F);
        return CurrentFunctionInteractKey.ToString().ToUpperInvariant();
    }

    private static void EnsureKeybindLoaded(Key defaultKey)
    {
        if (keybindLoaded)
        {
            return;
        }

        string savedFunctionKey = PlayerPrefs.GetString(FunctionInteractKeyPref, defaultKey.ToString());
        if (System.Enum.TryParse(savedFunctionKey, out Key parsedKey))
        {
            CurrentFunctionInteractKey = parsedKey;
        }
        else
        {
            CurrentFunctionInteractKey = defaultKey;
        }

        keybindLoaded = true;
    }

    private bool IsLocalPlayer()
    {
        return playerMain != null && playerMain.networkinfo != null && playerMain.networkinfo.IsLocal;
    }

    private void HandleMenuToggleInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsMenuOpen)
            {
                CloseSettingsMenu();
            }
            else
            {
                OpenSettingsMenu();
            }
        }
    }

    private void HandleFunctionKeyRebindInput()
    {
        if (!waitingForFunctionKey || Keyboard.current == null)
        {
            return;
        }

        foreach (KeyControl keyControl in Keyboard.current.allKeys)
        {
            if (!keyControl.wasPressedThisFrame)
            {
                continue;
            }

            CurrentFunctionInteractKey = keyControl.keyCode;
            PlayerPrefs.SetString(FunctionInteractKeyPref, CurrentFunctionInteractKey.ToString());
            waitingForFunctionKey = false;
            break;
        }
    }

    private void OpenSettingsMenu()
    {
        settingsMenuOpen = true;
        waitingForFunctionKey = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseSettingsMenu()
    {
        settingsMenuOpen = false;
        waitingForFunctionKey = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void DrawSettingsPanel()
    {
        GUILayout.BeginVertical();
        GUILayout.Label("Function Interact (Window/Light)");
        GUILayout.Label($"Current Key: {GetFunctionInteractKeyLabel()}");

        if (!waitingForFunctionKey)
        {
            if (GUILayout.Button("Change Key"))
            {
                waitingForFunctionKey = true;
            }
        }
        else
        {
            GUILayout.Label("Press any key...");
            if (GUILayout.Button("Cancel"))
            {
                waitingForFunctionKey = false;
            }
        }

        GUILayout.Space(12f);
        if (GUILayout.Button("Close (Esc)"))
        {
            CloseSettingsMenu();
        }

        GUILayout.EndVertical();
    }
}
