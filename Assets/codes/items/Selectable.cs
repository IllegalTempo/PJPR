using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(StaticOutline))]
public partial class Selectable : MonoBehaviour
{

    public StaticOutline outline;
    public bool LookedAt = false;
    public float ClickTimer = 0f;
    protected void OnEnable()
    {
        outline = GetComponent<StaticOutline>();

    }
    protected virtual void Update()
    {
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
    public virtual void OnClicked()
    {
        ClickTimer = 0.2f;
    }
}
