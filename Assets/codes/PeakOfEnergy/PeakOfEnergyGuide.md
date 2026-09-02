# Peak Of Energy — Unity Setup Guide (for complete beginners)

This guide assumes you have **zero** Unity experience. Follow it top to bottom, in order.
Everything you need to build is either already in the project or created by you in a few
clicks in the Unity editor. You do **not** need to write or edit any code.

> **The good news:** all the mission logic, networking, colors, spawns, and UI updates are
> already written in `Assets/codes/PeakOfEnergy/`. Your job is only to **build the 3D
> objects (prefabs)**, **register them**, and **place them in the scene**.

---

## 1. What you already have (the scripts)

| File | What it does |
| :--- | :--- |
| `PeakOfEnergyManager.cs` | The mission brain: RPM, stability (0.5 s window), the state machine (Idle → Spinning → Stabilizing → Cooldown → GameOver/Victory), charge hold logic, ring health, cooldown timer. Runs on the host; clients mirror it over the network automatically. |
| `PeakOfEnergyMission.cs` | Starts/stops the mission. The vote system calls this when "Peak of Energy" wins. Press **F6** to start it manually for testing. |
| `RingVisualController.cs` | Colors the ring: **Blue** = too slow, **Green** = sweet spot (pulsing), **Red/Orange** = too fast, **Flashing Yellow** = unstable (wobble). Also drives the ring's spin **purely with Unity physics**: it sets the Rigidbody's `angularVelocity` around the local Z axis. |
| `RustEater.cs` | The "stability breaker". Crawls to the ring, latches, and applies wobble (does NOT change average RPM — it only breaks your stability). Kill it with the **Hammer**. |
| `PeakOfEnergyMeteorite.cs` | Flies at the ring. Hits the ring → −10 health. Hits a player → bounces them away (no damage). Kill it with the Hammer (cannon comes later). |
| `PeakOfEnergySpawner.cs` | Spawns enemies while Spinning/Stabilizing/Cooldown. Spawns get faster each charge. A successful charge bursts and clears all enemies. |
| `ChargeInteractable.cs` | The center button. Hold it to channel a charge; it cancels instantly if RPM leaves the range or becomes unstable. |
| `PeakOfEnergyUI.cs` | The HUD: RPM gauge, target range, stability icon, charge progress, charge counter, health bar, cooldown timer. |
| *(Test input built into the manager)* | The `PeakOfEnergyManager` component includes a built-in test: hold **Left Shift** (default) to spin the ring up. This is a stand-in until the real spaceship pulling mechanic is built. |
| `HammerItem.cs` (edited) | The Hammer now also destroys Rust Eaters and mission meteorites. |

Networking is also done: 4 new network messages were added and registered
(`NMS_Server_PeakOfEnergyState`, `NMS_Server_PeakOfEnergyChargeEvent`,
`NMS_Client_PeakOfEnergyChargeRequest`, `NMS_Client_PeakOfEnergyHammerHit`).

**You do not need to connect any UnityEvents in the Inspector.** All event connections
are made in code.

---

## 2. How the mission works (30-second summary)

1. Players pull the ring (future mechanic; for now use the debug keys) until the RPM
   number is inside the target range **and** stable (no wobble).
2. The ring turns **green**. A player holds the **center button** for 2.5 s.
3. If a Rust Eater latches on (yellow flashing) or the RPM slips out of range, the
   charge **cancels**.
4. Charge done → cooldown 15 s (clear enemies) → next (harder) RPM target. 5 charges = win.
5. Meteorites damage the ring. Ring health hits 0 = game over.

---

## 3. Step 0 — Quick smoke test (optional, 2 minutes)

Before building anything, verify the code compiles and the mission can run:

1. Open the project in Unity, open the scene `Assets/Scenes/Main.unity`.
2. Press **Play**. Press **F6** (or select the `PeakOfEnergyMission` object in the
   Hierarchy and right-click the component → *Start Mission*).
3. You won't see the ring yet (we build it in Step 3) but the Console should log
   `[PeakOfEnergyManager] Mission started. Charge 1 of 5...` and you can already test
   the charge flow later once the button exists.

---

## 4. Step 1 — Create the enemy prefabs

A *prefab* is a reusable 3D object saved in your Project window. You'll make two.

### 4a. Mission Meteorite prefab (easiest — copy an existing one)

1. In the **Project** window, go to `Assets/Resources/Prefabs/EscapeTheBlackholeData/`.
2. Right-click `SmallMeteoritePrefab.prefab` → **Duplicate**.
3. Rename the duplicate: **`PeakOfEnergyMeteoritePrefab`**.
4. Double-click it to open the prefab editor.
5. On the root object, find the **Meteorite** component (grey script icon). Right-click it
   → **Remove Component**. (We replace it with our own meteorite behavior.)
6. Click **Add Component** → type `PeakOfEnergyMeteorite` → add it.
7. On that component assign:
   - *Fragment Prefab* → drag in `Assets/Resources/Prefabs/Meteorite_Fragment.prefab`
   - *Break Effect Prefab* → optional, any explosion particle you like.
8. Make sure the root has these components (add any that are missing):
   `Rigidbody` (**KINEMATIC** — make sure "Is Kinematic" is ticked, otherwise the server's
   movement control is undone by physics), a **Collider** (e.g. Sphere Collider),
   `NetworkGameObject`, `NetworkPrefabIdentity`, `StaticOutline` (auto-added with the script).
9. Click **Save** (top-left of the prefab editor).

### 4b. Rust Eater prefab (from scratch)

1. In the Project window, go to `Assets/Prefabs/Mission/Peak of Energy/`.
2. Right-click inside that folder → **Create → Empty**. Name it **`RustEaterPrefab`**.
3. Right-click it → **Create Empty** child (inside it) named `Body`. Add a
   **Sphere** (right-click child → 3D Object → Sphere) as a grandchild, scale it to
   ~0.6, and give it a red/dark material so you can see it.
4. Select the **root** `RustEaterPrefab` and:
   - Add Component → `Rigidbody`, tick **Is Kinematic**.
   - Add Component → **Sphere Collider** (or any collider on the Body).
   - Add Component → `RustEater`.
   - Add Component → `NetworkGameObject`.
   - Add Component → `NetworkPrefabIdentity`.
   - (StaticOutline is added automatically.)
5. On the `RustEater` component, set *Death Effect Prefab* to any small explosion
   particle, and leave `Wobble Intensity = 8` (default is fine).
6. Save it as a prefab: drag the object from the Hierarchy into the
   `Assets/Prefabs/Mission/Peak of Energy/` folder. You can delete the scene copy after.

---

## 5. Step 2 — Register the prefabs with the network

Enemies are created over the network by ID, so each prefab needs a "definition" and a
registry entry.

1. In the Project window: right-click anywhere (e.g. `Assets/codes/definition/itemDefinition`)
   → **Create → Game → Item**. This makes a *PrefabDefinition* asset.
   - Name it **`PeakOfEnergyMeteoriteDef`**.
   - In the Inspector, drag `PeakOfEnergyMeteoritePrefab` into **Item Prefab**.
   - Make sure **Is Pool Prefab** is **unticked**.
2. Make a second one: **`RustEaterDef`**, drag `RustEaterPrefab` into **Item Prefab**,
   Is Pool Prefab unticked.
3. Open the registry: double-click `Assets/Prefabs/NetworkPrefabRegistry.asset`.
   - Click **+** at the bottom of the *Entries* list.
   - Set **Prefab Id** to **`PeakMeteorite`** and drag `PeakOfEnergyMeteoriteDef` into
     **Prefab Definition**.
   - Click **+** again: **Prefab Id** = **`RustEater`**, Definition = `RustEaterDef`.
4. Select the **NetworkSystem** object in the scene Hierarchy. In the Inspector find the
   **Network Prefab Registry** field and make sure it points at the
   `NetworkPrefabRegistry` asset (it should already). Press **Play** once so the registry
   reloads.

> ⚠️ The spawner looks up these exact IDs (`PeakMeteorite` and `RustEater`). If you change
> them in the registry, change them on the `PeakOfEnergySpawner` component too.

---

## 6. Step 3 — Build the ring device (the arena)

Everything mission-related sits on **one scene object**. Build it in the scene first,
then save it as a prefab so you can reuse it.

1. In the Hierarchy: right-click → **Create Empty**. Name it **`PeakOfEnergyRing`**.
2. Child 1 — the wheel: right-click it → **3D Object → Cylinder**, name it `Wheel`.
   - Scale it flat and wide so it looks like a big wheel/disc (e.g. scale
     `x = 12, y = 0.25, z = 12`).
   - Give it a material you can see (grey metal look is fine).
3. On the **root** `PeakOfEnergyRing`, add these components **in this order**:
   - **`PeakOfEnergyManager`** (mission brain)
   - **`PeakOfEnergySpawner`** (spawns enemies around this object's position)
   - **`RingVisualController`** (colors + spinning)

   On `PeakOfEnergySpawner`, set the **Ring Radius** field to a single number matching
   your ring's real radius (distance from the ring's center to its outer edge, e.g. `25`).
   This single number is the source of truth for where enemies spawn and where Rust Eaters
   latch. (Access via *Ring Visuals Root* is not needed for the spawner anymore.)
   - **Ring Visuals Root** → also set this to your `OUTER_RING` object so the spawner can
     spot the **individual ring components** (the 16 pieces). Enemies then sometimes aim at
     a specific component and otherwise scatter naturally around the ring (see
     `Component Aim Chance` and `Target Scatter Radius` below the radius field).
4. Wire up `RingVisualController` on the same object:
   - **Ring Visuals Root** → drag the whole **`OUTER_RING`** object in here. This colors
     every ring piece together. (Leave *Ring Renderer* empty when using it.)
   - **Spin Rigidbody** → the `OUTER_RING`'s **Rigidbody** (auto-found if left empty). The
     spin is driven with **Unity physics**: `RingVisualController` sets its `angularVelocity`
     and Unity integrates the rotation.
   - **Rigidbody constraints (recommended, you've done this):** on the Rigidbody, in the
     Inspector freeze **X, Y, Z position** AND freeze **X, Y rotation**, leaving only
     **Z rotation** free. This keeps the ring perfectly in place while it spins.
   - **Make sure `OUTER_RING`'s local Z-axis is perpendicular to the ring's face** (i.e.
     Z points along the ring's hole axis). The spin is around the **local Z axis**, so Z
     must point through the ring's hole for the ring to spin flat like a coin (face stays
     tilted, position never changes, no tumbling). Its 16 mesh sections rotate together
     as one piece.
   - *Ring Light* → optional: add a Point Light child for the glow.
   - *Button Renderer / Button Light* → the center button mesh you make next.
5. Child 2 — the center button: right-click `PeakOfEnergyRing` → **3D Object → Cube**,
   name it `CoreButton`.
   - Position it at the center of the wheel, slightly above it (e.g. `y = 0.6`), scale
     small (e.g. `0.8`).
   - Add Component → **`ChargeInteractable`**.
   - Give it a material (it will glow green when ready).
6. The **wheel needs a collider** so meteorites can hit it: on the `Wheel`, add
   **Capsule Collider** (or Box Collider) sized to cover the wheel.
7. Select the `Wheel`'s MeshRenderer and, in *RingVisualController*, also check the
   *Wobble Shake* and pulse settings are as you like (defaults are fine).
8. **Save the ring as a prefab:** drag `PeakOfEnergyRing` from the Hierarchy into
   `Assets/Prefabs/Mission/Peak of Energy/`, then delete the scene copy and drag the
   prefab back into the scene (this keeps everything working from one prefab).

> 💡 The ring is a plain scene object — it does **not** need a network identity. Only the
> *spawned* enemies need identities, and those are assigned automatically at spawn time.

---

## 7. Step 4 — Mission object + HUD

### Mission object
1. In the Hierarchy: right-click → **Create Empty**. Name it **`PeakOfEnergyMission`**.
2. Add Component → **`PeakOfEnergyMission`**.
3. *Mission Manager* field: leave empty — it finds the ring's manager automatically
   (or drag the `PeakOfEnergyRing` object in).

### HUD (heads-up display)
1. In the Hierarchy: right-click → **UI → Canvas**. (Leave it as *Screen Space -
   Overlay* — easiest.)
2. Add Component to the Canvas → **`PeakOfEnergyUI`**.
3. Create the UI pieces as children of the Canvas and drag them into the matching
   fields on `PeakOfEnergyUI`. Every field is optional — add them one at a time:
   - **RPM value**: UI → Text - TextMeshPro, name `RPMText`.
   - **Target range**: another TMP Text, name `TargetText`.
   - **Stability icons**: two small TMP Texts or Images, e.g. a green "✔" and a red "✘"
     (name them `StableIcon` / `UnstableIcon`).
   - **Charge radial**: UI → Image, set its *Image Type* to **Filled**, *Fill Method*
     **Radial 360**, then drag it into `Charge Radial Fill`. Create an empty parent
     GameObject named `ChargeGroup` containing it, and drag the parent into
     `Charge Group`.
   - **Charge counter**: TMP Text, e.g. "Charge 1 / 5".
   - **Health bar**: UI → Image, *Image Type* **Filled**, *Fill Method* **Horizontal**.
   - **Cooldown**: another radial-filled Image inside a `CooldownGroup` parent.
   - **Status text**: TMP Text showing "SPIN THE RING!" etc.

   (If you prefer the health bar floating *above the ring in 3D*, change the Canvas to
   **World Space** and place it above the ring — the same script works.)

---

## 8. Step 5 — (Recommended) Create the Hammer for EVA melee

There is no Hammer prefab in the project yet, so make one:

1. In the Hierarchy: right-click → **Create Empty**. Name it `Hammer`.
2. Add a child **Cube** scaled like a mallet head (e.g. `0.15, 0.1, 0.5`) and a child
   **Cylinder** scaled like the handle — position them to look like a hammer.
3. On the **root** `Hammer` add: `HammerItem`, `NetworkGameObject`,
   `NetworkPrefabIdentity`, a **Rigidbody** (kinematic), and a **Collider** on the head.
4. Save it as a prefab in `Assets/Prefabs/Mission/Peak of Energy/`.
5. Give it a definition + registry entry like Step 2 (any Prefab Id, e.g. **`Hammer`**):
   5a. Create (or edit) a `PrefabDefinition` asset named `Hammer` and set its
       **Item Prefab** to the Hammer prefab you just saved (drag it in) — remove the `.asset`
       if it's empty/misconfigured and recreate it so `Item Prefab` is filled.
   5b. Open `Assets/Prefabs/NetworkPrefabRegistry.asset`, click **+**, set its **Prefab Id**
       to **`Hammer`**, and drag the `Hammer` definition into **Prefab Definition**.
   5c. The scene `NetworkSystem` object must rebuild its lookup on this registry — press
       **Play** once so `NetworkPrefabIdentity.PrefabID` fills in with `Hammer` (verify it in
       the Inspector: it must NOT be blank).
6. **Don't hand-place the hammer in the scene.** Instead, add the **`PeakOfEnergyHammerSpawner`**
   component to any object (for example the `PeakOfEnergyMission` object). At Play it calls
   `NetworkSystem.CreateNetworkObject("Hammer", …)` so the hammer spawns as a proper
   networked item (exactly like meteorites/Rust Eaters) and is pickable with **F**.
   You can set `Spawn Point` to position it, or leave it to spawn near the ship.
7. The scene `NetworkSystem` must rebuild its lookup when the registry changes — just press
   **Play** once after adding the `Hammer` registry entry (the spawned item gets its
   `PrefabID`/`Identifier` automatically through `NetworkPrefabIdentity.OnInstantiate`).

> 🧹 EVA melee = walk out of the ship in zero-g (space = jetpack), look at the enemy,
> hold the **Interact** button while holding the Hammer. Rust Eaters die instantly.

> 📛 **Still says "Cannot pick up 'Hammer': no NetworkObject / identifier"?** That warning fires
> when the item has no registry mapping. Check, in order:
> (a) `Hammer.asset` (PrefabDefinition)'s **Item Prefab** is set, (b) the
> `NetworkPrefabRegistry` has an entry `PrefabId: Hammer` → the `Hammer` definition, (c) you
> **spawned** the hammer (via `PeakOfEnergyHammerSpawner` / `CreateNetworkObject`) rather than
> hand-placing a loose scene object, and (d) the prefab has `NetworkPrefabIdentity` +
> `NetworkGameObject` on the same root as `HammerItem`. The code is not the problem — the item
> must be a registered, spawned networked item.

---

## 9. Step 6 — Put the mission in the vote (mission projection)

1. In the Project window: right-click → **Create → Missions → Mission**. Name the asset
   **`Peak of Energy`**.
   - Mission Name: **`Peak of Energy`** (must match exactly — this is how the vote
     system finds your mission).
   - Fill in description, reward credits, difficulty, duration as you like.
2. In the scene, select the **MissionManager** object (search "MissionManager" in the
   Hierarchy). Drag the `Peak of Energy` asset into the **Available Missions** list.
3. Now when players vote on missions, "Peak of Energy" appears, and when it wins the
   mission starts automatically.

---

## 10. Step 7 — Playtest

### Offline test
1. The test spin input is already built into the **`PeakOfEnergyManager`** component on the
   ring (fields: *Test Spin Key Enabled*, *Test Spin Key*, *Test Spin Rate Per Second*,
   default Left Shift). Nothing to attach.
2. **Hold Left Shift** to spin the ring — the gauge and ring color respond immediately
   (even before you start the mission, the manager lets you test-spin).
3. Press **Play**. Start the mission with **F6**.
4. Hold **Left Shift** — watch the ring turn **blue → green** as RPM enters the range.
   The wheel should spin and the HUD gauge should climb.
5. Walk to the center button (jetpack with **Space**), look at it, and **hold the
   Interact key** (left mouse). The charge radial fills. Keep the RPM inside the range
   and stable (release Shift!) until the charge completes → **cooldown**.
6. Test failure: while charging, hold **Shift** (RPM goes up) or let a Rust Eater latch —
   the charge cancels with a flash and the ring flashes yellow.

### Online test (host + client)
1. Host starts the game (creates lobby) — they are the server, so all mission logic runs
   on their machine.
2. The client joins. Both see the same RPM gauge, colors, health, and charge progress
   (the host broadcasts state ~10×/second).
3. The client can press the center button too — the server validates and everyone sees
   the charge. Rust Eater wobble is visible on both machines.

---

## 11. Tuning knobs (all on `PeakOfEnergyManager`)

| Field | Default | Meaning |
| :--- | :--- | :--- |
| `Target RPM Min / Max` | 100–140 … 260–300 | The 5 charge tiers. Change to any ranges you like. |
| `Charge Hold Duration` | 2.5 s | How long to hold the button. |
| `RPM Fluctuation Tolerance` | 5 | Max stable fluctuation (±RPM). |
| `Stability Window Seconds` | 0.5 | Window used to measure fluctuation. |
| `Cooldown Duration` | 15 s | Wait between charges. |
| `Ring Max Health` | 100 | Meteorite hits do 10 → 10 hits kills the ring. |
| `Meteorite Damage` | 10 | Flat damage per meteorite impact. |
| `RPM Decay Per Second` | 2 | RPM slowly falls while Spinning (so pulling matters). Set 0 for none. |

On `PeakOfEnergySpawner`: **`Ring Radius`** (set once to match your ring), `Spawn Distance
Margin`, `Meteorite Base/Min Interval`, `Rust Eater Base/Min Interval`, `Max Rust Eaters`,
`Max Active Meteorites`.

---

## 12. Troubleshooting

| Symptom | Fix |
| :--- | :--- |
| "No PeakOfEnergyManager found" | The ring prefab with the manager isn't in the scene, or `PeakOfEnergyMission`/`ChargeInteractable` can't find it. Check the ring is active. |
| "Failed to create 'PeakMeteorite'" | The prefab isn't registered in `NetworkPrefabRegistry` (Step 2), or the PrefabDefinition's *Item Prefab* is empty. |
| Enemies/button can't be looked at | `Selectable` objects use **Layer 6**. Select the `GameCore` object → *Masks → Selectable Items* → make sure **Layer 6 (Selectable)** is ticked. |
| Ring doesn't take damage / no meteorites | Check the ring pieces have colliders, the spawner's `Ring Radius` is set to your real ring radius, and `Meteorite Base Interval` isn't huge. Meteorites spawn OUTSIDE the ring (radius + margin) and fly in. |
| Charge never starts | RPM must be **inside the range AND stable** (green, not flashing yellow) and the mission state must be Spinning. Kill Rust Eaters first. |
| The vote wins but the mission doesn't start | Read the Console: a warning tells you the cause. Either (a) there is **no `PeakOfEnergyMission` object in the scene** (create one, guide Step 4), or (b) the **MissionData's *Mission Name*** doesn't exactly match the `missionName` on the `PeakOfEnergyMission` component (it must be the same, case-sensitive, e.g. "Peak of Energy"). |
| Enemies are far from the ring or meteorites score instantly | Your spawner's `Ring Radius` doesn't match the real ring. Set it to the actual radius (center to outer edge); enemies spawn at `radius + margin` and Rust Eaters latch at `radius × 0.95`. |
| Ring flips / tumbles / spins the wrong way | The spin is pure physics: `RingVisualController` sets `angularVelocity` around the **local Z axis**. Freeze the Rigidbody's position XYZ + rotation XY, leave Z free. Make sure `OUTER_RING`'s Z-axis is perpendicular to the ring's face (points through the ring's hole) so it spins flat like a coin. |
| Rust Eater flies away or meteorite hangs mid-air | On BOTH enemy prefabs, make the **Rigidbody KINEMATIC** (tick "Is Kinematic") and untick gravity. Movement is server-driven via transform; a dynamic rigidbody fights it. Meteorites also self-destruct after a timeout, so they never hang forever. |
| RPM gauge doesn't rise when holding Left Shift | This project uses the **new Input System** (legacy `Input.GetKey` is disabled), so the test key is read through `Keyboard.current`. Check the `PeakOfEnergyManager` on the ring: `Test Spin Key Enabled` = on, `Test Spin Key` = Left Shift. (The old `PlayerShipController` was removed.) |
| Enemies spawn way out in space, nowhere near the ring | Set the spawner's **`Ring Radius`** to your real ring radius. Enemies are placed relative to the ring device's center (`Ring geometry…` console line prints it), never far away. |
| Ring doesn't change color / only one piece changes | Drag the whole `OUTER_RING` into **Ring Visuals Root** on `RingVisualController` (colors all pieces together). |
| Clients don't see enemies move | The host must spawn them (server-only). Check the host's Console for the "Failed to create" warnings. |
| Ring spins too fast/slow visually | Adjust `Spin Visual Multiplier` on `RingVisualController`. |
| Wobble doesn't break stability | `Wobble Intensity` (8) must exceed `RPM Fluctuation Tolerance` (5) in effect; raise it or lower the tolerance. |

---

## 13. Final checklist

- [ ] Meteorite + Rust Eater prefabs exist and are registered (`PeakMeteorite`, `RustEater`).
- [ ] Ring prefab with Manager + Spawner + VisualController + core button exists in scene.
- [ ] `PeakOfEnergyMission` object in scene (F6 starts it).
- [ ] `MissionData` "Peak of Energy" added to `MissionManager.availableMissions`.
- [ ] HUD canvas wired to `PeakOfEnergyUI`.
- [ ] Hammer prefab exists (recommended) so Rust Eaters can be killed.
- [ ] Tested: 5 different RPM ranges, wobble ≠ RPM change, meteorites bounce players only,
      hold-to-charge cancels on fluctuation, works offline + host + client.
