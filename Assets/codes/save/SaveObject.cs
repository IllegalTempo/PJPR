using Steamworks;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SaveObject : MonoBehaviour
{
    string savepath;
    //Save Stuff about the spaceship, decoration
    public DecorationSaveData[] saved_decorations = new DecorationSaveData[0];
    public static SaveObject instance;
    private void Awake()
    {
        savepath = Path.Combine(Application.persistentDataPath, "save.json");

        instance = this;
        Load();

    }
    public void Save()
    {
        //Save the decoration
        saved_decorations = new DecorationSaveData[GameCore.INSTANCE.Local_PlayerSpaceship.GetDecorationByUUID_onShip.Count];
        int decorationIndex = 0;
        foreach(Decoration dec in GameCore.INSTANCE.Local_PlayerSpaceship.GetDecorationByUUID_onShip.Values)
        {
            saved_decorations[decorationIndex] = new DecorationSaveData(dec.DecorationID, dec.transform.localPosition,dec.transform.localRotation);
            decorationIndex++;
        }
        string json = JsonUtility.ToJson(this);
        string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        File.WriteAllText(savepath, encoded);

    }
    public void Load() //run this after the spaceship is loaded, so the decorations can be loaded on the spaceship
    {
        //if file dont exists
        if (!File.Exists(savepath))
        {

        } else
        {
            string json = File.ReadAllText(savepath);
            string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(json));
            JsonUtility.FromJsonOverwrite(decoded, this);
        }
        transform.GetChild(0).gameObject.SetActive(true);
        
        
        

    }
}


