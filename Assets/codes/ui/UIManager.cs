using Cysharp.Threading.Tasks;
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

    [Header("Interaction Indicator")]
    [SerializeField]
    private GameObject[] interactionIndicatorGroup;
    [SerializeField]
    private TMP_Text[] interactionName;
    [SerializeField]
    private TMP_Text[] interactionKey;


    [Header("SocialTab")]
    [SerializeField]
    private GameObject playerDisplayPrefab;
    [SerializeField]
    private Transform playerDisplayGroup;
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
        HideAllInteraction();

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
        loadingScreenGroup.SetActive(false);
    }
    public void ShowInteraction(string interactionname, string key, int index)
    {
        interactionIndicatorGroup[index].SetActive(true);
        interactionName[index].text = interactionname;
        interactionKey[index].text = key;
    }
    public void HideAllInteraction()
    {
        for (int i = 0; i < interactionIndicatorGroup.Length; i++)
        {
            HideInteraction(i);
        }
    }
    public void HideInteraction(int index)
    {
        interactionIndicatorGroup[index].SetActive(false);

    }
    public async UniTask NewPlayerDisplay(ulong steamid,string name)
    {
        Texture2D icon = await GameCore.Instance.GetIcon(steamid);
        GameObject newDisplay = Instantiate(playerDisplayPrefab, playerDisplayGroup);
        newDisplay.GetComponent<PlayerDisplay>().Init(icon, name);
    }

}
