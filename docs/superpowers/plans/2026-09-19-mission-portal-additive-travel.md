# Mission Portal Additive Travel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace immediate post-vote mission startup with a server-authoritative networked portal journey into additive mission scenes while preserving and pausing `Main.unity`, with an immediately usable return portal.

**Architecture:** `MissionManager` owns a mission-specific state machine and coordinates existing network-object spawning, additive Unity scene loading, peer readiness, waypoint selection, ship teleportation, mission lifecycle callbacks, and return cleanup. Small mission-specific data types keep state and packet serialization testable; runtime behavior remains host-authoritative and uses the existing Facepunch relay message router.

**Tech Stack:** Unity 6.3, C#, Unity SceneManager additive loading, UniTask, NUnit/Unity Test Framework, existing `NMS` packet system, existing `NetworkGameObject` registry/spawn APIs.

**Spec:** `docs/superpowers/specs/2026-09-19-mission-portal-additive-travel-design.md`

## Global Constraints

- Keep `Main.unity` loaded; mission travel must not destroy its objects.
- Pause only explicitly configured `Main` roots, preserving and restoring each root's prior active state.
- The host is authoritative for portal spawning/use, scene-transition state, mission activation/outcome, and spaceship transforms.
- Use the existing registered network prefab ID `Mission_Portal` for both portal directions.
- Place the outbound portal within configurable minimum/maximum distance; return to its exact position and the ship rotation captured on entry.
- The return portal is immediately usable. An unfinished mission fails on return; a completed mission stays successful.
- A new vote may begin only after return cleanup reaches `Idle`.
- Create additive scenes for Escape the Blackhole, Peak of Energy, Mission3, Mission4, and Mission5.
- Do not introduce a general-purpose portal or scene-travel framework without separate user approval.
- Preserve unrelated working-tree changes. Use Unity editor APIs, not hand-edited scene YAML.
- Runtime changes must pass `dotnet build Assembly-CSharp.csproj --no-restore`; editor changes must also pass `dotnet build Assembly-CSharp-Editor.csproj --no-restore`.

## Review Focus

- A ready client disconnects during loading: remove it from the awaited set so the remaining peers can continue.
- Several ship child colliders enter in one physics step: accept only the first valid transition for the active portal ID and phase.
- A mission scene is already loaded after an interrupted run: reuse it only for the same active session/scene; otherwise unload it first.
- A pause root starts inactive: record `false` and keep it inactive after restoration.
- A late join occurs during loading, mission activity, or return: apply the snapshot idempotently without starting/failing twice.

---

## File Structure

**Create runtime files**

- `Assets/codes/missions/travel/MissionTravelState.cs` — phases, outcome, snapshot, readiness/session rules.
- `Assets/codes/missions/travel/MissionPortalPlacement.cs` — deterministic distance-bounded placement.
- `Assets/codes/missions/travel/MissionPortal.cs` — host-only ship trigger forwarding and waypoint ownership.
- `Assets/codes/missions/travel/MissionSpawnPoint.cs` — additive-scene ship and return-portal marker.
- `Assets/codes/missions/travel/MissionPauseState.cs` — exact pause-root state capture/restoration.
- `Assets/codes/missions/MissionManager.Travel.cs` — additive loading, handshake, entry, outcome, return, rollback.
- `Assets/codes/Network/Messages/NMS_Server/NMS_Server_LoadMissionScene.cs`
- `Assets/codes/Network/Messages/NMS_Client/NMS_Client_MissionSceneReady.cs`
- `Assets/codes/Network/Messages/NMS_Server/NMS_Server_EnterMission.cs`
- `Assets/codes/Network/Messages/NMS_Server/NMS_Server_ReturnFromMission.cs`
- `Assets/codes/Network/Messages/NMS_Server/NMS_Server_AbortMissionLoad.cs`

**Create editor/test files**

- `Assets/Editor/MissionTravel/MissionTravelSceneSetup.cs` — idempotent scene/prefab/data/build-settings setup.
- `Assets/Editor/Tests/MissionTravelStateTests.cs`
- `Assets/Editor/Tests/MissionPortalPlacementTests.cs`
- `Assets/Editor/Tests/MissionPortalTests.cs`
- `Assets/Editor/Tests/MissionTravelPacketTests.cs`
- `Assets/Editor/Tests/MissionPauseStateTests.cs`
- `Assets/Editor/Tests/MissionReturnTests.cs`
- `Assets/Editor/Tests/MissionLateJoinTests.cs`
- `Assets/Editor/Tests/MissionTravelAssetTests.cs`

**Modify existing files/assets**

- `Assets/codes/missions/MissionData.cs`
- `Assets/codes/missions/MissionManager.cs`
- `Assets/codes/missions/interactable/MissionTerminal.cs`
- `Assets/codes/Network/Packets/packets.cs`
- `Assets/codes/Network/NetworkRouter.cs`
- `Assets/codes/Network/Messages/NMS_Client/NMS_Client_RequestVotingSession.cs`
- `Assets/codes/Network/Messages/NMS_Server/NMS_Server_VoteResult.cs`
- `Assets/codes/Network/Messages/NMS_Server/NMS_Server_SyncScene.cs`
- `Assets/codes/spaceship/MainSpaceship.cs`
- `Assets/codes/EscapeTheBlackhole/EscapeBlackholeMission.cs`
- `Assets/codes/missions/PeakOfEnergy/PeakOfEnergyMission.cs`
- `Assets/Prefabs/Portal.prefab`
- `Assets/Prefabs/systems.prefab` and its `Main.unity` instance overrides
- Mission `MissionData` assets
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/Scenes/Missions/*.unity`

### Task 1: Mission Travel State and Placement

**Files:**
- Create: `Assets/codes/missions/travel/MissionTravelState.cs`
- Create: `Assets/codes/missions/travel/MissionPortalPlacement.cs`
- Test: `Assets/Editor/Tests/MissionTravelStateTests.cs`
- Test: `Assets/Editor/Tests/MissionPortalPlacementTests.cs`

**Interfaces:**
- Produces: `MissionTravelPhase`, `MissionOutcome`, `MissionTravelSnapshot`, `MissionTravelState`.
- Produces: `MissionPortalPlacement.Calculate(Vector3 origin, float minimumDistance, float maximumDistance, Vector3 direction, float distance01)`.

- [ ] **Step 1: Write failing lifecycle/readiness tests**

```csharp
[Test]
public void Loading_RemovesDisconnectedPeerAndRejectsStaleAck()
{
    var state = new MissionTravelState();
    Assert.That(state.TryBeginVote(), Is.True);
    state.SetOutboundPortal("Peak of Energy", "PeakOfEnergy", "portal-1", new Vector3(10, 0, 0));
    Assert.That(state.TryBeginLoading(7, new ulong[] { 11, 22 }), Is.True);
    Assert.That(state.Acknowledge(11, 6), Is.False);
    Assert.That(state.Acknowledge(11, 7), Is.True);
    state.MarkHostLoaded(7);
    Assert.That(state.AllPeersLoaded, Is.False);
    state.RemoveExpectedPeer(22);
    Assert.That(state.AllPeersLoaded, Is.True);
}

[Test]
public void Outcome_IsRecordedOnlyOnce()
{
    var state = MissionTravelStateTestFactory.CreateActive();
    Assert.That(state.RecordOutcome(MissionOutcome.Succeeded), Is.True);
    Assert.That(state.RecordOutcome(MissionOutcome.Failed), Is.False);
    Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.Succeeded));
}
```

- [ ] **Step 2: Run `dotnet build Assembly-CSharp-Editor.csproj --no-restore`**

Expected: FAIL because the mission travel types do not exist.

- [ ] **Step 3: Implement the state types and legal transitions**

```csharp
public enum MissionTravelPhase { Idle, Voting, OutboundPortal, LoadingMission, MissionActive, Returning }
public enum MissionOutcome { None, Succeeded, Failed }

[System.Serializable]
public readonly struct MissionTravelSnapshot
{
    public readonly MissionTravelPhase Phase;
    public readonly int SessionId;
    public readonly string MissionName;
    public readonly string SceneName;
    public readonly string ActivePortalId;
    public readonly Vector3 ReturnPosition;
    public readonly Quaternion ReturnRotation;
    public readonly MissionOutcome Outcome;
}
```

`MissionTravelState` exposes `Phase`, `SessionId`, `Outcome`, `Snapshot`, `TryBeginVote()`, `SetOutboundPortal(...)`, `TryBeginLoading(...)`, `MarkHostLoaded(int)`, `Acknowledge(ulong,int)`, `RemoveExpectedPeer(ulong)`, `AllPeersLoaded`, `EnterMission(...)`, `RecordOutcome(...)`, `BeginReturning()`, `Reset()`, and `ApplySnapshot(...)`. Use `HashSet<ulong>` for expected/ready clients and a separate host-loaded flag.

- [ ] **Step 4: Write failing deterministic placement tests**

```csharp
[TestCase(100f, 300f, 0f, 100f)]
[TestCase(100f, 300f, 0.5f, 200f)]
[TestCase(100f, 300f, 1f, 300f)]
public void Calculate_UsesConfiguredDistance(float min, float max, float sample, float expected)
{
    Vector3 result = MissionPortalPlacement.Calculate(Vector3.one, min, max, Vector3.right, sample);
    Assert.That(Vector3.Distance(Vector3.one, result), Is.EqualTo(expected).Within(0.001f));
}

[Test]
public void Calculate_RejectsInvalidRange()
{
    Assert.Throws<System.ArgumentOutOfRangeException>(() =>
        MissionPortalPlacement.Calculate(Vector3.zero, 20f, 10f, Vector3.forward, 0.5f));
}
```

- [ ] **Step 5: Implement placement by normalizing the supplied direction and using `Mathf.Lerp(min,max,Mathf.Clamp01(distance01))`; reject zero direction, negative minimum, and maximum below minimum**

- [ ] **Step 6: Run the two Unity EditMode fixtures and both builds**

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add Assets/codes/missions/travel Assets/Editor/Tests/MissionTravelStateTests.cs Assets/Editor/Tests/MissionPortalPlacementTests.cs
git commit -m "feat: add mission travel state model"
```

### Task 2: Spaceship Teleport Primitive

**Files:**
- Modify: `Assets/codes/spaceship/MainSpaceship.cs`
- Test: `Assets/Editor/Tests/MainSpaceshipTeleportTests.cs`

**Interfaces:**
- Produces: `MainSpaceship.Teleport(Vector3, Quaternion)`.

- [ ] **Step 1: Write the failing teleport test**

```csharp
[Test]
public void Teleport_ClearsVelocityAndAcceleration()
{
    MainSpaceship ship = CreateStartedShip();
    ship.GetComponent<Rigidbody>().linearVelocity = Vector3.one * 20f;
    ship.SetHandleSpeed(3);
    ship.Teleport(new Vector3(50, 1, -20), Quaternion.Euler(0, 90, 0));
    Assert.That(ship.transform.position, Is.EqualTo(new Vector3(50, 1, -20)));
    Assert.That(ship.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));
    Assert.That(ship.GetAcceleration(), Is.EqualTo(Vector3.zero));
}
```

- [ ] **Step 2: Run the fixture**

Expected: FAIL because the APIs are absent.

- [ ] **Step 3: Implement `MainSpaceship.Teleport`**

Call `StopMovement()`; set rigidbody position/rotation if present; zero `linearVelocity` and `angularVelocity`; otherwise use `transform.SetPositionAndRotation`.

- [ ] **Step 4: Run the focused fixture and runtime build**

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add Assets/codes/spaceship/MainSpaceship.cs Assets/Editor/Tests/MainSpaceshipTeleportTests.cs
git commit -m "feat: add authoritative spaceship teleport"
```

### Task 3: Networked Portal Entry and Additive Mission Loading

**Files:**
- Create: `Assets/codes/missions/travel/MissionPortal.cs`
- Create: `Assets/codes/missions/travel/MissionSpawnPoint.cs`
- Create: `Assets/codes/missions/travel/MissionPauseState.cs`
- Create: `Assets/codes/missions/MissionManager.Travel.cs`
- Create: the five mission travel message files listed in File Structure
- Modify: `Assets/codes/missions/MissionData.cs`
- Modify: `Assets/codes/missions/MissionManager.cs`
- Modify: `Assets/codes/missions/interactable/MissionTerminal.cs`
- Modify: `Assets/codes/Network/Packets/packets.cs`
- Modify: `Assets/codes/Network/NetworkRouter.cs`
- Modify: `Assets/codes/Network/Messages/NMS_Client/NMS_Client_RequestVotingSession.cs`
- Modify: `Assets/codes/Network/Messages/NMS_Server/NMS_Server_VoteResult.cs`
- Test: `Assets/Editor/Tests/MissionPortalTests.cs`
- Test: `Assets/Editor/Tests/MissionTravelPacketTests.cs`
- Test: `Assets/Editor/Tests/MissionManagerPortalTests.cs`
- Test: `Assets/Editor/Tests/MissionPauseStateTests.cs`

**Interfaces:**
- Consumes: `MissionTravelState`, `MissionPortalPlacement`, `MainSpaceship.Teleport`, `NetworkSystem.CreateNetworkObject`.
- Produces: `MissionPortal`, `MissionSpawnPoint`, `MissionPauseState`, guarded voting, outbound portal creation, all load/readiness/enter/abort messages, and their fully implemented manager handlers.

- [ ] **Step 1: Write failing portal filtering and pause-state tests**

```csharp
[Test]
public void Portal_AcceptsOnlyCurrentShipHierarchy()
{
    MainSpaceship ship = CreateStartedShip();
    var hull = new GameObject("Hull");
    hull.transform.SetParent(ship.transform);
    Assert.That(MissionPortal.IsSpaceshipCollider(hull.AddComponent<BoxCollider>()), Is.True);
    Assert.That(MissionPortal.IsSpaceshipCollider(
        new GameObject("Rock").AddComponent<SphereCollider>()), Is.False);
}

[Test]
public void Restore_PreservesOriginallyInactiveRoots()
{
    var active = new GameObject("Active");
    var inactive = new GameObject("Inactive");
    inactive.SetActive(false);
    var pause = new MissionPauseState(new[] { active, inactive });
    pause.Pause();
    pause.Restore();
    Assert.That(active.activeSelf, Is.True);
    Assert.That(inactive.activeSelf, Is.False);
}
```

Add null/duplicate root and repeated Pause/Restore cases.

- [ ] **Step 2: Implement the focused portal, marker, and pause types**

`MissionPortal.OnTriggerEnter` returns unless `NetworkSystem.Instance.IsWorldManager`, the collider belongs to `MainSpaceship.Instance.transform`, and `MissionManager.TryUsePortal(NetworkId, collider)` accepts the current portal/phase. Read `NetworkId` from `NetworkPrefabIdentity.Identifier`. Waypoint methods set the portal transform/label and clear only the active mission waypoint.

`MissionPauseState` captures each unique non-null root's `activeSelf` once and restores exactly that value.

```csharp
public sealed class MissionSpawnPoint : MonoBehaviour
{
    [SerializeField] private Vector3 returnPortalLocalOffset = new(15f, 0f, 0f);
    public Vector3 ShipPosition => transform.position;
    public Quaternion ShipRotation => transform.rotation;
    public Vector3 ReturnPortalPosition => transform.TransformPoint(returnPortalLocalOffset);
}
```

- [ ] **Step 3: Write failing message round-trip tests**

```csharp
[Test]
public void LoadMissionScene_RoundTripsUnicodeAndSession()
{
    var sent = new NMS_Server_LoadMissionScene(42, "Peak of Energy", "PeakOfEnergy");
    using var writer = new Packet(sent.PacketID);
    sent.Write(writer);
    using var reader = new Packet(writer.GetPacketData(9), null);
    var received = NMS_Server_LoadMissionScene.Read(reader);
    Assert.That(received.SessionId, Is.EqualTo(42));
    Assert.That(received.MissionName, Is.EqualTo("Peak of Energy"));
    Assert.That(received.SceneName, Is.EqualTo("PeakOfEnergy"));
    Assert.That(reader.BytesRemaining, Is.Zero);
}
```

Add equivalent tests for ready, enter, return, and abort, including empty strings and exact packet exhaustion.

- [ ] **Step 4: Add packet IDs, payloads, registration, and complete handlers**

```csharp
// ServerPackets
LoadMissionScene = 1026,
EnterMission = 1027,
ReturnFromMission = 1028,
AbortMissionLoad = 1029,

// ClientPackets
MissionSceneReady = 2009,
```

Use exact payloads:

```text
LoadMissionScene: int sessionId, string missionName, string sceneName
MissionSceneReady: int sessionId, string sceneName
EnterMission: int sessionId, Vector3 shipPosition, Quaternion shipRotation, string returnPortalId
ReturnFromMission: int sessionId, Vector3 returnPosition, Quaternion returnRotation, bool succeeded
AbortMissionLoad: int sessionId, string sceneName, string reason
```

Register server messages in `serverMessages` and readiness in `clientMessages`. Client/server handlers call the fully implemented manager methods added in Steps 8–10; keep this task uncommitted until those steps compile and pass.

- [ ] **Step 5: Add additive scene identity to MissionData**

```csharp
[Tooltip("Unity scene name loaded additively for this mission.")]
public string missionSceneName;

[HideInInspector]
public GameObject missionScene;
```

Keep the old serialized prefab reference for migration, but remove all runtime reads.

- [ ] **Step 6: Write failing vote-gate and outbound-spawn tests**

```csharp
[Test]
public void Voting_IsRejectedOutsideIdle()
{
    MissionManager manager = CreateManagerWithMissionData();
    manager.TestTravelState.SetOutboundPortal("Mission3", "Mission3", "p", Vector3.zero);
    Assert.That(manager.CanStartVote, Is.False);
    Assert.That(manager.TryStartVotingSession(1), Is.False);
}

[Test]
public async Task Winner_SpawnsOnePortalWithinBounds()
{
    var h = CreateOfflineManagerHarness(100f, 200f);
    await h.Manager.PrepareWinningMissionAsync(h.Mission);
    Assert.That(h.SpawnedPortalCount, Is.EqualTo(1));
    Assert.That(Vector3.Distance(h.ShipPosition, h.SpawnPosition), Is.InRange(100f, 200f));
    Assert.That(h.Manager.TravelSnapshot.Phase, Is.EqualTo(MissionTravelPhase.OutboundPortal));
}
```

Use a mission-specific overridable spawn method or `#if UNITY_EDITOR` delegate, not a general dependency-injection framework.

- [ ] **Step 7: Implement guarded voting and outbound portal creation**

Make `MissionManager` partial. Add `CanStartVote`, `TravelSnapshot`, and `TryStartVotingSession(int)`. The terminal and request server action use the guarded method; vote casting works only during `Voting`. At tally, resolve exact `MissionData`, validate `missionSceneName`, calculate placement from `Random.onUnitSphere` and `Random.value`, create exactly one `Mission_Portal`, and record its ID/position. Remove immediate `SpawnMissionScene` and mission-start-hook calls. `NMS_Server_VoteResult` displays the winner only.

- [ ] **Step 8: Implement additive loading and ready acknowledgements**

When the active outbound portal is used: atomically change to `LoadingMission`; capture ship rotation; snapshot ready remote Steam IDs; increment session; disable repeat use; broadcast load; load locally.

For every peer: reject older sessions; reuse an already-loaded scene only for the same session/name; unload a different leftover mission scene; call `SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)`; locate exactly one `MissionSpawnPoint` in that scene; host marks itself loaded and clients send `NMS_Client_MissionSceneReady`.

While loading, prune remote IDs no longer present/ready in `GameServer.NetworkUsers`. Ignore stale/duplicate acknowledgements. At `NetworkSystem.TIMEOUTSECONDS`, broadcast abort, unload the partial scene, restore `OutboundPortal`, and reactivate the original portal.

- [ ] **Step 9: Implement synchronized entry and client cleanup handlers**

After all ready: generate the return portal ID; broadcast enter; every peer captures/pauses configured Main roots exactly once; host despawns outbound portal, teleports the ship, spawns `Mission_Portal` at `MissionSpawnPoint.ReturnPortalPosition` with the predetermined ID, enters `MissionActive`, and invokes the selected mission start hook. Clients apply pause/teleport idempotently and receive the portal via `NMS_Server_NewObject`. Label it `Return to Main`.

Implement `HandleAbortMissionLoad` to unload only the matching partial scene and restore the outbound state. Implement `HandleReturnFromMission` to teleport from its payload, restore captured roots, unload the active additive scene, clear the waypoint, and reset client state idempotently. Task 4 adds the host-side decision that emits the return message; the client handler is complete here so every new message compiles and behaves safely at this task boundary.

- [ ] **Step 10: Add edge-case tests**

Test two ship child colliders create one load; disconnected peer unblocks readiness; stale and duplicate acknowledgements are ignored; timeout restores the outbound portal; a same-session loaded scene is reused; a different loaded mission scene is unloaded; duplicate enter commands capture/pause once; missing scene/spawn marker rolls back with a clear reason.

- [ ] **Step 11: Run all Task 3 fixtures and both builds**

Expected: portal, packet, manager, and pause tests PASS; both builds exit `0`.

- [ ] **Step 12: Commit the complete entry flow**

```powershell
git add Assets/codes/missions Assets/codes/Network/Packets/packets.cs Assets/codes/Network/NetworkRouter.cs Assets/codes/Network/Messages Assets/Editor/Tests
git commit -m "feat: enter additive missions through portal"
```

### Task 4: Mission Outcome and Return

**Files:**
- Modify: `Assets/codes/EscapeTheBlackhole/EscapeBlackholeMission.cs`
- Modify: `Assets/codes/missions/PeakOfEnergy/PeakOfEnergyMission.cs`
- Modify: `Assets/codes/missions/MissionManager.Travel.cs`
- Test: `Assets/Editor/Tests/MissionReturnTests.cs`

**Interfaces:**
- Produces: `ReportMissionEnded(string,bool)`, `RequestActiveMissionFailure()`, `ReturnToMainAsync()`.

- [ ] **Step 1: Write failing early/completed return tests**

```csharp
[Test]
public async Task EarlyReturn_FailsExactlyOnceAndRestoresIdle()
{
    var h = CreateActiveMissionHarness();
    await h.UseReturnPortalTwice();
    Assert.That(h.FailInvocationCount, Is.EqualTo(1));
    Assert.That(h.Manager.TravelSnapshot.Phase, Is.EqualTo(MissionTravelPhase.Idle));
    Assert.That(h.ShipPosition, Is.EqualTo(h.OutboundPortalPosition));
}

[Test]
public async Task CompletedReturn_DoesNotFailMission()
{
    var h = CreateActiveMissionHarness();
    h.Manager.ReportMissionEnded(h.MissionName, true);
    await h.UseReturnPortal();
    Assert.That(h.FailInvocationCount, Is.Zero);
    Assert.That(h.LastReturnSucceeded, Is.True);
}
```

- [ ] **Step 2: Run the fixture**

Expected: FAIL with missing return APIs.

- [ ] **Step 3: Wire lifecycle reporting**

At each wrapper's successful/failed end, call `MissionManager.Instance?.ReportMissionEnded(missionName, won)`. Add `FailMissionForReturn()` that invokes `EndMission(false)` only while active. Match exact current mission name. Placeholder missions directly record failure.

- [ ] **Step 4: Implement synchronized return**

Immediately enter `Returning`; if unfinished, fail once; despawn return portal; broadcast return payload; teleport host/client to saved portal position and captured entry rotation; restore roots; unload additive scene; clear waypoint/session; reset to `Idle`. Repeat messages/triggers are no-ops.

- [ ] **Step 5: Run return tests and runtime build**

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add Assets/codes/EscapeTheBlackhole/EscapeBlackholeMission.cs Assets/codes/missions/PeakOfEnergy/PeakOfEnergyMission.cs Assets/codes/missions/MissionManager.Travel.cs Assets/Editor/Tests/MissionReturnTests.cs
git commit -m "feat: return from additive missions"
```

### Task 5: Late-Join Synchronization

**Files:**
- Modify: `Assets/codes/Network/Messages/NMS_Server/NMS_Server_SyncScene.cs`
- Modify: `Assets/codes/missions/MissionManager.Travel.cs`
- Modify: `Assets/Editor/Tests/MissionTravelPacketTests.cs`
- Test: `Assets/Editor/Tests/MissionLateJoinTests.cs`

**Interfaces:**
- Produces: `ApplyLateJoinTravelSnapshotAsync(MissionTravelSnapshot)`.

- [ ] **Step 1: Write failing sync snapshot tests for `Idle`, `OutboundPortal`, `LoadingMission`, `MissionActive`, and `Returning`**

```csharp
[TestCase(MissionTravelPhase.OutboundPortal)]
[TestCase(MissionTravelPhase.LoadingMission)]
[TestCase(MissionTravelPhase.MissionActive)]
[TestCase(MissionTravelPhase.Returning)]
public void SyncScene_RoundTripsTravelSnapshot(MissionTravelPhase phase)
{
    MissionTravelSnapshot expected = SnapshotFactory.Create(phase);
    NMS_Server_SyncScene received = RoundTripSyncScene(expected);
    Assert.That(received.TravelSnapshot, Is.EqualTo(expected));
}
```

- [ ] **Step 2: Serialize the snapshot after existing voting data**

Write/read in this exact order: phase int, session ID, mission name, scene name, portal ID, return position, return rotation, outcome int. Expose `TravelSnapshot` read-only.

- [ ] **Step 3: Apply travel before final ready state**

`Idle`: nothing. `OutboundPortal`: restore waypoint. `LoadingMission`: load/ack. `MissionActive`: load if necessary, pause once, restore portal waypoint. `Returning`: finish return idempotently. Move `UpdateReadyState(SyncNetworkObjects)` after travel application.

- [ ] **Step 4: Test duplicate snapshot, newer return during load, and stale session rejection**

Expected: one scene load, one pause capture, correct cancellation/restore, older session ignored.

- [ ] **Step 5: Run late-join tests and both builds**

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add Assets/codes/Network/Messages/NMS_Server/NMS_Server_SyncScene.cs Assets/codes/missions/MissionManager.Travel.cs Assets/Editor/Tests
git commit -m "feat: sync mission travel for late joiners"
```

### Task 6: Generate Mission Scenes and Configure Assets

**Files:**
- Create: `Assets/Editor/MissionTravel/MissionTravelSceneSetup.cs`
- Test: `Assets/Editor/Tests/MissionTravelAssetTests.cs`
- Modify through Unity APIs: portal prefab, mission data assets, systems/Main configuration, build settings
- Create through Unity APIs: `Assets/Scenes/Missions/*.unity`

**Interfaces:**
- Produces: idempotent `MissionTravelSceneSetup.GenerateAll()`.

- [ ] **Step 1: Implement the editor setup entry point**

```csharp
[MenuItem("Tools/Missions/Generate Additive Mission Scenes")]
public static void GenerateAll()
{
    EnsurePortalPrefab();
    EnsureMissionScene("EscapeTheBlackhole", EscapePrefabPath, false);
    EnsureMissionScene("PeakOfEnergy", PeakPrefabPath, true);
    EnsureMissionScene("Mission3", null, false);
    EnsureMissionScene("Mission4", null, false);
    EnsureMissionScene("Mission5", null, false);
    UpdateMissionDataAssets();
    UpdateBuildSettings();
    ConfigureMainPauseRoots();
    AssetDatabase.SaveAssets();
}
```

Define the asset constants exactly:

```csharp
private const string EscapePrefabPath = "Assets/Prefabs/Mission/Escape The Blackhole/EscapeBlackholeMission.prefab";
private const string PeakPrefabPath = "Assets/Prefabs/Mission/Peak of Energy/Peak Of Energy.prefab";
private const string PortalPrefabPath = "Assets/Prefabs/Portal.prefab";
private const string MainScenePath = "Assets/Scenes/Main.unity";
private const string MissionSceneFolder = "Assets/Scenes/Missions";
```

Use `PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset` to add one trigger `SphereCollider` and `MissionPortal`. Use `EditorSceneManager.NewScene/SaveScene`, instantiate gameplay prefabs, and add exactly one `MissionSpawnPoint`. Repeated execution updates rather than duplicates.

- [ ] **Step 2: Map every MissionData by exact name**

```text
Escape the Blackhole -> EscapeTheBlackhole
Peak of Energy -> PeakOfEnergy
Mission3 -> Mission3
Mission4 -> Mission4
Mission5 -> Mission5
```

Apply to all `AssetDatabase.FindAssets("t:MissionData")` results, including duplicate Escape assets. Log and skip unknown names.

- [ ] **Step 3: Configure explicit Main pause roots safely**

Open `Main.unity`, locate the manager instance, and assign the existing root objects named `Docks`, `Directional Light`, and `Global Volume`. Require each name exactly once; throw and leave the scene unsaved if any is absent or duplicated. Explicitly leave `Scene`, `Main Camera`, `EventSystem`, and `PlayerSpawn` active; `Scene` contains core prefab instances and must never be paused as a whole. Print the three selected root names before saving.

- [ ] **Step 4: Run the setup**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\jedts\PJPR' -executeMethod MissionTravelSceneSetup.GenerateAll -logFile 'C:\Users\jedts\PJPR\Logs\mission-travel-setup.log'
```

Expected: exit `0`; five scenes, five unique mappings, portal configuration, build entries, and pause-root names logged.

- [ ] **Step 5: Add asset validation tests**

Open each scene and assert exactly one spawn point; implemented scenes have the proper lifecycle wrapper; placeholders have none; all five scenes are enabled in build settings; the portal has `NetworkPrefabIdentity`, `NetworkGameObject`, `MissionPortal`, and a trigger; registry ID `Mission_Portal` resolves.

- [ ] **Step 6: Run asset tests and editor build**

Expected: PASS.

- [ ] **Step 7: Commit generated/configured assets**

```powershell
git add Assets/Editor/MissionTravel Assets/Prefabs/Portal.prefab Assets/Scenes/Missions Assets/codes/missions/Data Assets/Resources/Prefabs/EscapeTheBlackholeData Assets/Prefabs/systems.prefab Assets/Scenes/Main.unity ProjectSettings/EditorBuildSettings.asset Assets/Editor/Tests
git commit -m "feat: configure additive mission scenes"
```

### Task 7: Full Verification

**Files:**
- Modify only files required by observed failures.

**Interfaces:**
- Produces no new API.

- [ ] **Step 1: Run all mission EditMode tests**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\jedts\PJPR' -runTests -testPlatform EditMode -testFilter Mission -testResults 'C:\Users\jedts\PJPR\Logs\mission-travel-tests.xml' -logFile 'C:\Users\jedts\PJPR\Logs\mission-travel-tests.log'
```

Expected: exit `0`, zero failed tests.

- [ ] **Step 2: Run both builds**

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Expected: both exit `0`.

- [ ] **Step 3: Run host/client Play Mode smoke checks**

Verify: one outbound portal/waypoint; unrelated colliders ignored; both peers load the same scene; configured Main roots pause while networking/UI/ship stay active; immediate return fails and restores; completed return stays successful; loading-client disconnect does not deadlock; late join loads the active mission; bad scene rolls back to the outbound portal.

- [ ] **Step 4: Inspect final scope and serialization**

```powershell
git diff --check
git status --short
git diff --stat
```

Confirm no generated `.csproj`, recovery scenes, unrelated user files, or broad unintended `Main.unity` hierarchy changes are staged.

- [ ] **Step 5: Commit only fixes found during verification, if any**

Stage exact files individually and commit with `git commit -m "fix: harden mission portal travel"`.

- [ ] **Step 6: Request final review**

Use `superpowers:requesting-code-review` against the complete diff. Fix critical/important findings, rerun Steps 1–4, and explicitly report any Play Mode checks that still need user interaction.

