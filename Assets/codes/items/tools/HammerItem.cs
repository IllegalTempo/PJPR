using UnityEngine;

public class HammerItem : Item
{
    protected new void OnEnable()
    {
        isRepairTool = true;
        base.OnEnable();

        if (string.IsNullOrWhiteSpace(ItemName))
        {
            ItemName = "Hammer";
        }
    }

    public override bool IsRepairTool => true;
}
