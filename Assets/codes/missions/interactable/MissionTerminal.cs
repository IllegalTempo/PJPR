using UnityEngine;
using Assets.codes.Network.Messages;

[RequireComponent(typeof(NetworkPrefabIdentity))]
public class MissionTerminal : Interactable
{
    [SerializeField] private int missionsToShow = 3;
    private NetworkPrefabIdentity networkObject;


    void OnEnable()
    {
        networkObject = GetComponent<NetworkPrefabIdentity>();
    }

    public override void OnInteract_press(PlayerMain who)
    {
        if (MissionManager.Instance.IsVotingActive)
        {
            Debug.Log("[MissionTerminal] Voting is already in progress.");
            return;
        }

            // Client: request voting session from server
            string terminalId = networkObject != null ? networkObject.Identifier : "";
            var msg = new NMS_Client_RequestVotingSession(terminalId, missionsToShow);
            msg.SendMessageAsServerOrClient();
        
    }
}
