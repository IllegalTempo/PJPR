# Mission Portal Additive Travel Design

## Goal

Winning a mission vote creates a distant, networked outbound portal instead of starting the mission immediately. When the authoritative spaceship enters that portal, every peer loads the selected mission as an additive Unity scene, the configured parts of `Main.unity` are paused without being destroyed, and the spaceship is moved to the mission. A nearby return portal is available immediately. Returning before mission completion fails the mission; returning after completion preserves the successful outcome. In both cases, the spaceship returns to the outbound portal's original position, `Main.unity` is restored, the mission scene is unloaded, and voting becomes available again.

The implementation is specific to mission travel. It will not introduce a generalized scene-travel or reusable portal framework without separate approval.

## Mission Travel Lifecycle

`MissionManager` owns a single server-authoritative travel state:

1. `Idle`: no vote or mission travel is active; voting may begin.
2. `Voting`: the current voting behavior is active.
3. `OutboundPortal`: a winner has been selected and the outbound portal is available.
4. `LoadingMission`: the portal has been entered and peers are loading the additive scene.
5. `MissionActive`: the spaceship is in the mission scene and the return portal is available.
6. `Returning`: the mission outcome is finalized, the ship returns to `Main`, and the mission scene unloads.

Invalid or repeated actions are rejected unless they match the current state. In particular, a new vote cannot start while a portal, load, mission, or return operation is active.

## Vote Result and Outbound Portal

At vote completion, the host records the winning `MissionData` but does not spawn or start the mission environment. It selects a random three-dimensional direction and a configurable distance within minimum and maximum bounds from `MainSpaceship.Instance`. The host spawns the registered `Mission_Portal` network prefab at that position through the existing `GameCore` and `NMS_Server_NewObject` path.

The outbound portal becomes the waypoint on every peer and uses the selected mission name in its label. The host records the portal network ID and its exact world position. That position is the return destination for the spaceship.

## Portal Component

The portal prefab receives a focused `MissionPortal` component and a trigger collider. The component identifies the portal to `MissionManager` by its network identity. It does not contain scene-loading or mission-selection policy.

Only the host processes trigger entry. The collider must belong to `MainSpaceship` or one of its children. Player, item, meteorite, and unrelated colliders are ignored. `MissionManager` determines whether the active portal is outbound or returning from its current state and registered portal ID.

The existing waypoint system tracks the currently active portal. Despawning or disabling that portal clears the waypoint. Late join synchronization restores the appropriate waypoint.

## Additive Mission Scenes

Create these additive scenes under `Assets/Scenes/Missions/`:

- `EscapeTheBlackhole.unity`
- `PeakOfEnergy.unity`
- `Mission3.unity`
- `Mission4.unity`
- `Mission5.unity`

Each scene is included in Unity build settings and contains a `MissionSpawnPoint` marker. The return portal position is derived from that marker using a configurable local offset.

The Escape the Blackhole and Peak of Energy scenes contain their existing gameplay prefabs and lifecycle wrappers. Placeholder mission scenes contain only the required travel markers until their gameplay is implemented. Entering a placeholder mission is valid; returning marks it failed because it cannot report completion.

`MissionData` stores the additive scene name. Mission data assets that represent the same named mission use the same scene. The previous mission-prefab field is no longer used to enter missions.

## Loading Handshake

Entering the outbound portal starts a unique travel session:

1. The host changes to `LoadingMission` and makes the outbound portal temporarily unusable.
2. The host broadcasts the session ID and selected mission scene name.
3. Every peer, including the host, calls `SceneManager.LoadSceneAsync` in additive mode.
4. Each remote client acknowledges the session only after the scene is loaded and its `MissionSpawnPoint` is available.
5. The host ignores stale, duplicate, or unknown acknowledgements.
6. Travel continues after the host and every currently ready client have acknowledged.

If loading fails, the scene is missing, the spawn marker is absent, or the readiness timeout expires, every peer rolls back any partial load. The manager returns to `OutboundPortal`, and the existing outbound portal becomes usable again. No mission is started and no `Main` objects are paused during a failed load.

## Entering the Mission

After the loading handshake succeeds:

1. Every peer saves the active state of the explicitly configured `Main` roots and deactivates only those roots.
2. Networking, UI, players, the spaceship, persistent managers, and the portal transition code remain active.
3. The host despawns the outbound portal.
4. The host moves the spaceship to the mission's `MissionSpawnPoint` and resets its linear and angular velocity.
5. Existing spaceship rigidbody synchronization moves remote copies to the authoritative transform.
6. The mission lifecycle wrapper starts only on the host.
7. The host network-spawns a return portal near the spaceship.
8. The manager enters `MissionActive`, and the return portal becomes the shared waypoint.

The spaceship remains owned by `Main.unity`; it is repositioned but not moved between Unity scenes. This ensures unloading the additive mission scene cannot destroy the spaceship.

## Pausing and Restoring Main

`MissionManager` has an explicit serialized list of `Main` roots to pause. It records each root's active state before travel, deactivates those roots during the mission, and restores the exact recorded state on return. It does not enumerate and disable every root in `Main.unity`, because doing so could deactivate networking or other required systems.

No `Main.unity` object is destroyed as part of mission travel. The ship's return position is the saved outbound portal position, and its return rotation is the spaceship rotation captured when it entered the outbound portal. Its velocities are cleared to prevent it immediately leaving or re-entering the destination.

## Mission Outcome and Return

The return portal is usable immediately after arrival.

- If the active mission reports completion before return, `MissionManager` records success and leaves the return portal active.
- If the spaceship enters the return portal while the mission is unfinished, `MissionManager` invokes the mission's failure path once and records failure.
- Placeholder missions remain unfinished and therefore fail when the ship returns.

On return, the host enters `Returning`, destroys the return portal, and commands all peers to restore `Main`. The ship is placed at the saved outbound portal position, configured `Main` roots regain their prior active states, and the additive mission scene is unloaded. When cleanup completes, the manager clears session data and enters `Idle`, allowing the next vote.

## Mission Lifecycle Integration

`EscapeBlackholeMission` and `PeakOfEnergyMission` notify `MissionManager` when they end. The notification includes mission identity and success or failure. `MissionManager` accepts the result only for the current mission and only once.

When an early return requests failure, the manager invokes the active wrapper's existing `EndMission(false)` behavior. Re-entrant completion callbacks are ignored after the outcome has been recorded.

Mission activation remains server-authoritative. Clients load visuals and receive synchronized gameplay state through the project's existing mission-specific network messages.

## Network Messages and Late Join

The network layer adds narrowly scoped messages for:

- Server command to load an additive mission scene for a travel-session ID.
- Client acknowledgement that the requested mission scene and spawn marker are ready.
- Server command confirming mission entry and the authoritative destination.
- Server command to return to `Main` and unload the mission scene.

Packet IDs are added to the correct server/client enums and all messages are registered in `NetworkRouter`.

The existing scene synchronization snapshot is extended with the current travel state, session ID, mission identity, scene name, active portal network ID, saved return transform, and recorded outcome. A late joiner uses that state to load the additive mission scene when necessary, pause the correct `Main` roots, and restore the current portal waypoint before it is considered fully synchronized.

## Configuration and Validation

Inspector configuration includes:

- Outbound portal minimum and maximum distance.
- Return portal offset from `MissionSpawnPoint`.
- Portal network prefab ID, defaulting to `Mission_Portal`.
- Readiness timeout.
- Explicit `Main` roots to pause.
- Additive scene name on each `MissionData` asset.

Runtime validation reports clear errors for missing mission data, unregistered portal prefabs, invalid distance ranges, unavailable scenes, missing spawn markers, missing spaceship references, and incomplete peer loading. Errors restore a stable state rather than leaving voting permanently locked.

## Verification

Automated tests cover:

- Legal and illegal travel-state transitions.
- Random portal placement staying within configured distance bounds.
- Replacement of one vote per player, vote-session starts only from `Idle`, and vote casting only during `Voting`.
- Host-only portal activation and rejection of non-spaceship colliders.
- Ready acknowledgement tracking, duplicate/stale acknowledgement rejection, and timeout rollback.
- Successful mission entry and configured-root pausing.
- Early return recording failure exactly once.
- Completed mission return preserving success.
- Exact restoration of prior root active states.
- Late-join travel snapshot serialization and application.

Final verification includes `dotnet build Assembly-CSharp.csproj --no-restore` and targeted Unity play-mode checks with a host and client for outbound travel, early failure return, successful return, timeout rollback, and late joining.
