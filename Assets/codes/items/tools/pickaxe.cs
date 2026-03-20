using UnityEngine;

public class pickaxe : tools
{
    protected override void onUse(Selectable lookat)
    {
        if (lookat is minerals l)
        {
            l.onMined();
        }
    }
}
