using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameSaveSystem : MonoBehaviour
{
    private const string SaveFileName = "save.json";
    private const string DefaultBoosterModuleId = "module_booster1";
    private const string DefaultCannonModuleId = "module_cannon1";

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

    public async UniTask<bool> LoadGame()
    {
        return await LoadCurrentGame(SavePath);
    }

    public static void SaveCurrentGame(string path = null)
    {
        string savePath = string.IsNullOrWhiteSpace(path) ? DefaultSavePath : path;
        GameSaveData saveData = CaptureSaveData();
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Saved game to {savePath}");
    }

    public static async UniTask<bool> LoadCurrentGame(string path = null)
    {
        string savePath = string.IsNullOrWhiteSpace(path) ? DefaultSavePath : path;
        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"Save file not found at {savePath}. Loading default save.");
            ApplySaveData(CreateDefaultSaveData());
            return true;
        }

        string json = await File.ReadAllTextAsync(savePath);
        await UniTask.SwitchToMainThread();

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

        if (GameCore.Instance != null && Connector.Instance != null)
        {
            saveData.InstalledModules = Connector.Instance.GetInstalledModuleSaveData();
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

    public static GameSaveData CreateDefaultSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        //
        saveData.InstalledModules.Add(new InstalledModuleSaveData((int)ModuleSlotName.back_left, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData((int)ModuleSlotName.back_right, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData(0, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData(1, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData(2, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData(3, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData(4, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData(5, DefaultBoosterModuleId));
        saveData.InstalledModules.Add(new InstalledModuleSaveData(8, DefaultBoosterModuleId));

        return saveData;
    }

    public static void ApplySaveData(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (GameCore.Instance != null && Connector.Instance != null)
        {
            Connector.Instance.LoadInstalledModules(saveData.InstalledModules).Forget();
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
    private async void LoadGameFromContextMenu()
    {
        await LoadGame();
    }
}
