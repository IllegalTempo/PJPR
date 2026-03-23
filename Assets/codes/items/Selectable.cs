using UnityEngine;

[RequireComponent(typeof(StaticOutline))]
public partial class Selectable : MonoBehaviour
{

    public StaticOutline outline;
    private bool lookedAt = false;
    public float ClickTimer = 0f;
    protected virtual int Layer => 6; // Default layer for selectable objects

    protected virtual void OnEnable()
    {
        outline = GetComponent<StaticOutline>();
        gameObject.layer = Layer;
        onLookedAway();
    }

    protected virtual void Update()
    {
        if (outline == null)
            return;

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
    }
    public void onLookedAt()
    {
        lookedAt = true;
        if (outline != null)
            outline.enabled = true;

    }
    public void onLookedAway()
    {
        lookedAt = false;
        if (outline != null)
            outline.enabled = false;

    }
    public virtual void OnClicked()
    {
        ClickTimer = 0.2f;
    }

    //public virtual bool IsFunctionKeyOnly()
    //{
    //    return false;
    //}
}
