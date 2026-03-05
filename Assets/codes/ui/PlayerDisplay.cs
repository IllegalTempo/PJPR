using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class PlayerDisplay : MonoBehaviour
{
    [SerializeField]
    private RawImage playerIcon;
    [SerializeField]
    private TMP_Text playerName;
    public void Init(Texture2D playericon, string playername)
    {
        playerIcon.texture = playericon;
        playerName.text = playername;
    }
}
