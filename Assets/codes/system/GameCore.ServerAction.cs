using UnityEngine;
using System.Collections;

public partial class GameCore
{

    public void StartMissionLoop()
    {
        int index = Random.Range(0, getMissionWithLevel(CurrentMissionLevel).Length);
        StartMission(CurrentMissionLevel, index);

    }
}
