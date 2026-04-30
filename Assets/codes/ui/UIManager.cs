using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
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


    [Header("StorageDisplay")]
    [SerializeField]
    private GameObject SD_Group;
    [SerializeField]
    private GameObject SD_slotPrefab;
    [SerializeField]
    private Transform SD_slotGroup;

    [Header("Detailed Item Display")]
    [SerializeField]
    private GameObject DID_Group;
    [SerializeField]
    private TMP_Text DID_itemName;
    [SerializeField]
    private Image DID_itemIcon;
    [SerializeField]
    private TMP_Text DID_itemDescription;
    
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
        SD_Group.SetActive(false);
        DID_Group.SetActive(false);
    }
    public void DisplayDetailedItemDisplay(ItemDefinition item)
    {

        DID_itemName.text = item.itemName;
        DID_itemIcon.sprite = item.itemIcon;
        DID_itemDescription.text = item.itemDescription;
        DID_Group.SetActive(true);
    }
    public void HideDetailedItemDisplay()
    {
        DID_Group.SetActive(false);
    }


    public void DisplayStorage(storage srg)
    {
        //clear children in slot group
        foreach (Transform child in SD_slotGroup)
        {
            Destroy(child.gameObject);
        }
        foreach (ItemDefinition item in srg.GetItems())
        {
            Instantiate(SD_slotPrefab, SD_slotGroup).GetComponent<StorageSlot>().InitSlot(item.itemIcon);
        }
        SD_Group.SetActive(true);

    }
    public void HideStorage()
    {
        SD_Group.SetActive(false);
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
