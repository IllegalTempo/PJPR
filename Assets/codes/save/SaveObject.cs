using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SaveObject : MonoBehaviour
{
    readonly string savepath = Path.Combine(Application.persistentDataPath, "save.json");
    //Save Stuff about the spaceship, decoration
    public DecorationSaveData[] saved_decorations;
    public void Save()
    {
        //Save the decoration
        saved_decorations = new DecorationSaveData[transform.childCount];
        int decorationIndex = 0;
        foreach(Decoration dec in GameCore.instance.localSpaceship.GetDecorationByUUID_onShip.Values)
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
        string json = File.ReadAllText(savepath);
        string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(json));
        JsonUtility.FromJsonOverwrite(decoded, this);
        
        

    }
}


