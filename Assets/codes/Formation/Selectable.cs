using UnityEngine;
using UnityEngine.Events;

public readonly struct SelectionContext
{
    public readonly Selectable Selectable;
    public readonly Item Item;
    public readonly Slot Slot;
    public readonly Interactable Usable;

    public SelectionContext(Selectable selectable, Item item, Slot slot, Interactable usable)
    {
        Selectable = selectable;
        Item = item;
        Slot = slot;
        Usable = usable;
    }
}

[RequireComponent(typeof(Collider))]
public class Selectable : MonoBehaviour
{

    protected SelectionOutline outline;
    [SerializeField]
    private bool lookedAt = false;
    private float ClickTimer = 0f;
    public UnityEvent OnSelect;
    [Header("Interaction Resolution Overrides")]
    public Item itemOverride;
    public Slot slotOverride;

    public Interactable usableOverride;
    protected virtual int Layer => 6; // Default layer for selectable objects

    protected virtual void OnEnable()
    {
        outline = GetSelectionOutline();
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
            outline.OutlineVisible = true;

    }
    public void onLookedAway()
    {
        lookedAt = false;
        if (outline != null)
            outline.OutlineVisible = false;

    }
    public virtual void OnClicked()
    {
        ClickTimer = 0.2f;
        OnSelect?.Invoke();
    }

    public SelectionContext GetInteractionContext()
    {
        return new SelectionContext(this, ResolveItem(), ResolveSlot(), ResolveUsable());
    }

    private Item ResolveItem()
    {
        return itemOverride != null ? itemOverride : GetComponent<Item>();
    }

    private Slot ResolveSlot()
    {
        return slotOverride != null ? slotOverride : GetComponent<Slot>();
    }

    private Interactable ResolveUsable()
    {
        return usableOverride != null ? usableOverride : GetComponent<Interactable>();
    }

    private SelectionOutline GetSelectionOutline()
    {
        UIOutline uiOutline = GetComponent<UIOutline>();
        if (uiOutline != null)
            return uiOutline;

        return GetComponent<SelectionOutline>();
    }


    

    

    //public virtual bool IsFunctionKeyOnly()
    //{
    //    return false;
    //}
}
