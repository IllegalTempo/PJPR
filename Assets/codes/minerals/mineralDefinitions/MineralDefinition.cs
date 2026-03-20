using UnityEngine;

[CreateAssetMenu(fileName = "New Mineral", menuName = "Game/Mineral")]
public class MineralDefinition : ScriptableObject
{
    public string mineralName;
    public int hardness;
    public GameObject MinedPrefab;

}
