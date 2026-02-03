using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public string ItemName;
    public string ItemDescription;
    public bool LookedAt = false;
    public float ClickTimer = 0f;
    public string UUID;
    public StaticOutline outline;


    private void OnEnable()
    {
        outline = GetComponent<StaticOutline>();

    }
    private void Start()
    {
        if (!string.IsNullOrEmpty(UUID) && UUID.Length < 5)
        {
            GameCore.instance.GetItemByUUID.Add(UUID, this);
        }
    }
    public abstract void OnSelect();
    public void OnClicked()
    {
        ClickTimer = 0.2f;
        OnSelect();
    }
    public void SetAsTarget()
    {
        outline.OutlineColor = Color.yellow;
        outline.enabled = true;
    }
    public void SetAsNotTarget()
    {
        outline.OutlineColor = Color.white;
        outline.enabled = false;

    }
    protected virtual void update()
    {
    }
    private void Update()

    {
        update();
        if (ClickTimer > 0)
        {
            ClickTimer -= Time.deltaTime;
            outline.OutlineWidth = 10f;
        }
        else
        {
            ClickTimer = 0f;
            outline.OutlineWidth = 5f;

        }
        if (LookedAt)
        {
            outline.enabled = true;
            LookedAt = false;
        }
        else
        {
            outline.enabled = false;



        }
    }
}
