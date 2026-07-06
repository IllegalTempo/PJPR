using UnityEngine;
using Assets.codes.Network.Messages;

[RequireComponent(typeof(NetworkPrefabIdentity))]
public class MissionTerminal : Selectable, IUsable
{
    [SerializeField] private int missionsToShow = 3;
    private NetworkPrefabIdentity networkObject;

    protected override int Layer => 6;

    protected override void OnEnable()
    {
        base.OnEnable();
        networkObject = GetComponent<NetworkPrefabIdentity>();
    }

    public void OnInteract_press(PlayerMain who)
    {
        if (MissionManager.Instance.IsVotingActive)
        {
            Debug.Log("[MissionTerminal] Voting is already in progress.");
            return;
        }

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && !NetworkSystem.Instance.IsServer)
        {
            // Client: request voting session from server
            string terminalId = networkObject != null ? networkObject.Identifier : "";
            var msg = new NMS_Client_RequestVotingSession(terminalId, missionsToShow);
            NetworkRouter.Instance.SendMessageToServer(msg);
        }
        else
        {
            // Server or offline: start locally
            MissionManager.Instance.StartVotingSession(missionsToShow);
        }
    }
}
