using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

public partial class GameCore
{

    public void StartMissionLoop()
    {
        int index = Random.Range(0, getMissionWithLevel(CurrentMissionLevel).Length);
        StartMission(CurrentMissionLevel, index);
    }

    public async void onSelectPlanet(string selectedPlanetId)
    {
        Debug.Log($"Planet {selectedPlanetId} selected by the party leader!");
        
        // TODO: Trigger spaceship (or connector) movement/warp drive sequence towards the selected planet
        
        // minigames if they are related to the selected planet ? or unrelated cuz random
        // int minigamesToPlay = Random.Range(3, 5); 
        // await ProcessJourneyMinigames(minigamesToPlay);
        
        Debug.Log($"Arrived at planet {selectedPlanetId}!");
        // TODO: something to be done on the planet or other minigames etc.
    }

    // private async UniTask ProcessJourneyMinigames(int minigameCount)
    // {
    //     for (int i = 0; i < minigameCount; i++)
    //     {
    //         Debug.Log($"Starting journey minigame {i + 1}/{minigameCount}...");
    //         // StartMissionLoop(); 
            
    //         // TODO: Replace this with an actual wait until the minigame is fully completed by all players.

    //         await UniTask.Delay(5000); //placeholder delay (5s)
    //     }
    // }
}
