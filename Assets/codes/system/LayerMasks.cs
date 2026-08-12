using UnityEngine;
using UnityEngine.Serialization;
//liar mask!
public class LayerMasks : MonoBehaviour
{
    public LayerMask SelectableItems;
    public LayerMask MoveWith;
    [FormerlySerializedAs("InSpaceshipDetection")]
    public LayerMask InSpaceshipDetect;
}
