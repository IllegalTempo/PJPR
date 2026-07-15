using Steamworks;
using UnityEngine;
using TMPro;

public class getname : MonoBehaviour
{
    [SerializeField]
    private NetworkPlayerObject player;
    public TextMeshProUGUI myTextBox;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string playername = player.Getname();
        myTextBox.text = playername;
    }
}

