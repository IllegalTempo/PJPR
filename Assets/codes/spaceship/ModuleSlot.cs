using UnityEngine;
using System.Collections;


public class ModuleSlot : MonoBehaviour
{
    [SerializeField]
    public slotType type;
}
public enum slotType
{
    horizontal,
    vertical,
}
