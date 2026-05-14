using System;
using System.IO;
using UnityEngine;

public class GameSaveSystem : MonoBehaviour
{
    private const string SaveFileName = "save.json";

    public static GameSaveSystem Instance;

    [SerializeField] private string saveFileName = SaveFileName;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    public static string DefaultSavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        SaveCurrentGame(SavePath);
    }

    public bool LoadGame()
    {
        return LoadCurrentGame(SavePath);
    }

    public static void SaveCurrentGame(string path = null)
    {
        string savePath = string.IsNullOrWhiteSpace(path) ? DefaultSavePath : path;
        GameSaveData saveData = CaptureSaveData();
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Saved game to {savePath}");
    }

    public static bool LoadCurrentGame(string path = null)
    {
        string savePath = string.IsNullOrWhiteSpace(path) ? DefaultSavePath : path;
        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"Save file not found at {savePath}");
            return false;
        }

        string json = File.ReadAllText(savePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
        if (saveData == null)
        {
            Debug.LogWarning($"Save file at {savePath} could not be parsed.");
            return false;
        }

        ApplySaveData(saveData);
        Debug.Log($"Loaded game from {savePath}");
        return true;
    }

    public static GameSaveData CaptureSaveData()
    {
        GameSaveData saveData = new GameSaveData();

        if (GameCore.Instance != null && GameCore.Instance.Connector != null)
        {
            saveData.InstalledModules = GameCore.Instance.Connector.GetInstalledModuleSaveData();
        }

        if (NetworkSystem.Instance != null)
        {
            foreach (NetworkPlayerObject player in NetworkSystem.Instance.PlayerList.Values)
            {
                if (player == null)
                {
                    continue;
                }

                saveData.PlayerLocations.Add(new PlayerLocationSaveData(
                    player.steamID.ToString(),
                    player.transform.position,
                    player.transform.rotation));
            }
        }

        return saveData;
    }

    public static void ApplySaveData(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (GameCore.Instance != null && GameCore.Instance.Connector != null)
        {
            GameCore.Instance.Connector.LoadInstalledModules(saveData.InstalledModules);
        }

        if (NetworkSystem.Instance == null)
        {
            return;
        }

        foreach (PlayerLocationSaveData playerLocation in saveData.PlayerLocations)
        {
            if (playerLocation == null || !ulong.TryParse(playerLocation.SteamID, out ulong steamID))
            {
                continue;
            }

            if (!NetworkSystem.Instance.PlayerList.TryGetValue(steamID, out NetworkPlayerObject player) || player == null)
            {
                continue;
            }

            player.transform.SetPositionAndRotation(playerLocation.Position, playerLocation.Rotation);
            player.SetMovement(playerLocation.Position, player.NetworkHeadRot, playerLocation.Rotation);
        }
    }

    [ContextMenu("Save Game")]
    private void SaveGameFromContextMenu()
    {
        SaveGame();
    }

    [ContextMenu("Load Game")]
    private void LoadGameFromContextMenu()
    {
        LoadGame();
    }
}
