using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Loading Screen")]
    [SerializeField]
    private GameObject loadingScreenGroup;
    [SerializeField]
    private TMP_Text loadingStatusText;
    [SerializeField]
    private Slider progressBar;


    private void Awake()
    {
        Instance = this;


    }
    private void Start()
    {
        defaultState();

    }
    private void defaultState()
    {
        LoadingComplete();

    }
    public void ChangeLoadingStatus(string text, float progress)
    {
        loadingStatusText.text = text;
        progressBar.value = progress;
    }
    public void ShowLoadingScreen(string text)
    {
        loadingScreenGroup.SetActive(true);
        ChangeLoadingStatus(text, 0);
    }
    public void LoadingComplete()
    {
        //loadingScreenGroup.SetActive(false);
    }


}
