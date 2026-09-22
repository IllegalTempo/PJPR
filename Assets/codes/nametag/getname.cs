using Steamworks;
using UnityEngine;
using TMPro;

public class PlayerNameLabel : MonoBehaviour
{
    [SerializeField]
    private NetworkPlayerObject player;
    public TextMeshProUGUI myTextBox;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        string playername = player.Getname();
        myTextBox.text = playername;
    }
}

[System.Obsolete("Use PlayerNameLabel.")]
public class getname : PlayerNameLabel
{
}
