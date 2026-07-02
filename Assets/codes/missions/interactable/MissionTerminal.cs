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

    public void OnInteract(PlayerMain who)
    {
        if (MissionManager.Instance.IsVotingActive)
        {
            Debug.Log("[MissionTerminal] Voting is already in progress.");
            return;
        }

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
        {
            string terminalId = networkObject != null ? networkObject.Identifier : "";
            var msg = new NMS_Client_RequestVotingSession(terminalId, missionsToShow);
            NetworkRouter.Instance.SendMessageToServer(msg);
        }
        else
        {
            // Offline / single-player: start locally
            MissionManager.Instance.StartVotingSession(missionsToShow);
        }
    }
}
