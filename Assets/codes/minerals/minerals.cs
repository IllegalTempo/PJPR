using UnityEngine;

public class minerals : Selectable
{
    public MineralDefinition MineralType;
    public void onMined()
    {
        mineralItem item = Instantiate(MineralType.MinedPrefab, transform.position + new Vector3(0,2,0), Quaternion.identity).GetComponent<mineralItem>();
        //launch upward in random direction
        item.onDropped();

    }
}
