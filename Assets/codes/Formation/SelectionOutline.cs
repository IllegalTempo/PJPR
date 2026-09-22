using UnityEngine;

public abstract class SelectionOutline : MonoBehaviour
{
    public abstract float OutlineWidth { get; set; }

    public virtual bool OutlineVisible
    {
        get => enabled;
        set => enabled = value;
    }
}
