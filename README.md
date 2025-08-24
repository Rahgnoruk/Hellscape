# **Hellscape**

**Genre:** 2D top-down co-op survival-run  
 **Engine:** Unity 6 (6000.2.0f1) · URP (2D) · New Input System  
 **Netcode:** Netcode for GameObjects (NGO) \+ Unity Transport \+ Unity Relay (DTLS native / WSS WebGL)  
 **Platforms:** PC · WebGL · iOS/Android  
 **Modes:** Single-player (local host) & 2–16 player drop-in co-op

Drop-in co-op. Kite huge enemy waves across a large arena. Over time, stronger elites appear—and so do weapon drops. Survive as long as you can, coordinate counters, and chase big scores.

---

## **Why this repo is interesting (skills showcase)**

* **Hexagonal architecture (Ports & Adapters)**  
   Pure **Domain** (no `UnityEngine` refs) with **App/Net/Platform/Presentation** adapters. Clean boundaries, testability, and platform swap-ability.

* **Deterministic server simulation**  
   Authoritative `ServerSim` ticks at a fixed step; domain-local `Vector2`, `IRng` (XorShift) and snapshot codec for reproducible replays.

* **Authoritative multiplayer with Relay**  
   Host authoritative movement; clients send compact input via RPC; state replicated via NetworkVariables; Relay auto-selects DTLS/WSS.

* **TDD-first workflow**  
   Domain features land with NUnit unit tests (EditMode). Unity CLI runner script for automation. Snapshot round-trip “golden master” tests.

* **Modern Input**  
   New Input System with owner-only reads; mouse & touch-friendly aim abstraction; clean piping to authoritative sim.

* **UI Toolkit**  
   Main Menu (Host/Join), In-Game HUD (team/personal score, health), hit vignette, pause/in-game menu.

* **Content pipelines kept lightweight**  
   Stylized 2D sprites, 2D lights, “juice” via color flash / squash-stretch / particles. No skeletal rigs.

* **Pragmatic networking glue**  
   Minimal `RelayBootstrap` for UGS setup, `NetPlayer` thin owner controller, `SimGameServer` bridge to the Domain.

---

## **Current status (vertical slice)**

* ✅ **Relay host/join** (desktop & WebGL transport setup)

* ✅ **Two+ players moving** with owner-only input → server motion integration → replicated net positions

* ✅ **Aiming & visible shots** (toy bullets/impacts), hits register

* ✅ **Enemies spawn & chase** players; constrained arena (walled rectangle)

* ✅ **Players can die & revive** (simple timer rule; host authoritative)

* ✅ **UI Toolkit screens**: Main Menu, In-Game HUD, Pause menu, basic vignette feedback

* 🚧 **Weapon pickups & inventory (4 slots)**: base weapon (infinite) \+ three pickup slots (authoritative mergers/swap/drop) – *rolling out now*

* 📝 **GDD** below> shapes tiers, taxonomies, counters, and milestones

Scope intentionally minimal per slice; features land behind tests and keep Domain pure.

---

## **Architecture**
`Hellscape/`  
`├─ Assets/`  
`│  ├─ Hellscape/`  
`│  │  ├─ Scripts/`  
`│  │  │  ├─ Domain/                # Pure C# (no UnityEngine)`  
`│  │  │  │  ├─ IClock.cs, IRng.cs, DeterministicRng.cs`  
`│  │  │  │  ├─ CityGrid.cs, NightSystem.cs, Director.cs`  
`│  │  │  │  ├─ MovementConstants.cs, InputCommand.cs`  
`│  │  │  │  ├─ WorldSnapshot.cs, SnapshotCodec.cs`  
`│  │  │  │  └─ Weapons/ (Inventory models/logic, types)`  
`│  │  │  ├─ Net/                   # NGO + Relay adapters`  
`│  │  │  │  ├─ RelayBootstrap.cs`  
`│  │  │  │  ├─ NetPlayer.cs`  
`│  │  │  │  └─ (Network models, pickups)`  
`│  │  │  ├─ Platform/              # Platform services`  
`│  │  │  │  └─ UnityClock.cs`  
`│  │  │  ├─ App/                   # Composition / glue`  
`│  │  │  │  ├─ GameApp.cs`  
`│  │  │  │  └─ SimGameServer.cs`  
`│  │  │  └─ Presentation/          # UI, VFX/SFX, spawners`  
`│  │  ├─ Prefabs/                  # NetPlayer, WeaponPickup, etc.`  
`│  │  └─ UI/                       # UI Toolkit UXML/USS`  
`│  └─ Scripts/ (Unity-generated input actions etc.)`  
`├─ docs/`  
`│  ├─ gdd.md                       # Product truth`  
`│  ├─ architecture/overview.md     # Dev notes`  
`│  └─ tools/unity-test.ps1         # CLI test runner`  
`└─ Assets/Hellscape.Tests/         # NUnit EditMode tests`

### **Module boundaries (asmdefs)**

* `Hellscape.Domain` — **no Unity deps**

* `Hellscape.Net` — NGO/Relay bridge & serial models

* `Hellscape.Platform` — clocks, RNG, (pathfinding later)

* `Hellscape.Presentation` — UI Toolkit, VFX/SFX, spawners

* `Hellscape.App` — bootstraps & composition glue

* `Hellscape.Tests` — NUnit EditMode (unit/integration)

### **Networking model (thin but robust)**

* **Owner client → Server:** `InputCommand {tick, move, aim, buttons}` via throttled ServerRpc

* **Server:** integrates kinematic motion; authoritative positions; applies combat & pickup rules

* **Server → Clients:** replicated `NetworkVariable`s (e.g., `netPos`, inventory slots, health)

* **Relay:** DTLS for native; WSS for WebGL; anonymous UGS auth for frictionless join

### **Determinism & snapshots**

* Fixed tick (`IClock.FixedDelta`, 0.02s)

* Domain `Vector2` & XorShift RNG (seeded)

* Binary snapshot codec with round-trip tests

---

## **Getting started**

### **Requirements**

* **Unity 6** `6000.2.0f1`

* Packages: NGO, Unity Transport, UGS Core/Authentication/Relay, Input System, URP (2D renderer)

* UGS project set up (Relay enabled)

### **Running**

1. Open the project in Unity 6\.

2. Open the game scene (contains `NetworkManager`, `RelayBootstrap`, arena walls, spawners, UI).

3. **Host:** Press “Host via Relay” (debug UI) or Main Menu → Host.

4. **Join:** Enter join code on client (Main Menu → Join).

5. Move with **WASD/left stick**; Aim with **mouse/right stick**; **Fire** to shoot.

---

## **UI (UI Toolkit)**

* **Main Menu:** Host / Join (Relay join code)

* **In-Game HUD:** Team score, personal score, health, hit vignette, pause button

* **Pause/In-Game Menu:** Resume · Exit

* **Inventory Strip:** 4 slots (slot 0 \= base infinite, slots 1–3 pickups). Visuals wired; logic lands via authoritative inventory.

## **Roadmap**

### **Near-term**

* Authoritative **weapon pickups** (spawn/merge/swap/drop) ✅ *in progress*

* **Weapon switching & ammo HUD**, simple rarity coloring

* **Scoring** (team & personal) and **game over** screen with restart

* **Wave/Director** pacing tables and spawn ramps

* **Interpolation** buffers for higher RTT on WebGL

### **Pending (platform/services)**

* **Firebase backend (config as data)**  
   Centralize tunables for:

  * weapons (damage, fire rates, AP split, ammo bundles)

  * enemies (TTK bands, tags, spawn weights)

  * player stats, arena size, spawn ramps  
     Provide remote overrides per environment (dev/stage/prod) and hot-reload on host.

* **Lobby Finder (UGS Lobby/Matchmaker)**  
   Public room listings with: map/seed, current players, ping/region, privacy, join code copy.  
   From Main Menu, surface live lobbies and allow one-click join.  


# Hellscape — Game Design Document (GDD)

**Version:** 0.1 (living doc) **Project:** Hellscape **Genre:** 2D top‑down co‑op survival‑run **Camera:** Orthographic (URP 2D), top‑down **Platforms:** PC, WebGL, iOS/Android (Unity 6, NGO \+ Relay) **Mode(s):** Single‑player (local host) & 2–16 player drop‑in co‑op

Drop-in co-op constant waves of enemies come at you and your friends. You can freely move around a huge map to kite enemies as you fight them. Over time, higher elite enemies appear, but so do weapons lying on the floor.  
You must coordinate with your team to grab different weapons to better synergize with each other when fighting the infernal hordes.  
The game continues until all players die, scoring points based on how many enemies they killed.

---

## 1\. Vision & Pillars

The first version of the game will include Tiers 0, 1 and 2 of enemies and weapons

**Elevator pitch:** constant waves of enemies come at you and your friends. You can freely move around a huge map to kite enemies as you fight them. Over time, higher elite enemies appear, but so do weapons lying on the floor.  
You must coordinate with your team to grab different weapons to better synergize with each other when fighting the infernal hordes.  
The game continues until all players die, scoring points based on how many enemies they killed.

**Description:** Peer to peer multiplayer. One player hosts the game. You start with the basic gun, which you keep forever and has unlimited ammo. Each player has 4 more weapon slots and they find weapons on the floor around the play area.

Enemies have clear taxonomies. Normal enemies have one specialization, and stronger enemies start to combine them.  
In the beginning, you fight tier 0 enemies, the basic one. This first stage gives the chance for players to gather their first weapons and feel the game.

Very simple move and shoot. Find weapons on the floor. 2D top down view, the camera sticks to the player.   
No skeletal nor frame animations. “Animation” via sprite swap, color flash, scale squash/stretch, micro‑hops, particles, trails. Slower enemies Hop, faster enemies slide.   
For example, super heavy enemies have big slow, heavy hops that can even be accompanied by camera shake. And faster enemies just move towards you.  
Enemies have an idle sprite (when they haven't noticed you), an aggro sprite (when they notice you and start attacking) and some have a hurt sprite (when they get hit)  
Melee attacks from enemies are just a slash visual effect

Peer to peer matchmaking. One player hosts the game.

Weapons have taxonomies that make them especially effective against some enemies

There are no melee weapons.

Rifles are armor piercing, so they are more effective against armored enemies. But they fire slowly so they aren't good against swarms.

Submachine guns have a very high fire rate, so they are very good at mowing down a single, big, unarmored high HP enemy. But they aren't very accurate and the damage per bullet isn't that high, so they aren't good with swarms either.

Shotguns deal damage in a cone and push back, so they are good both with fast enemies and swarms, because you don't have to be so precise. But they don't deal damage very fast nor are they armor piercing, so they don't perform against armored or high HP enemies.

Higher tier weapons combine these taxonomies.

Assault rifles are the combination of rifles and machine guns, so they are both rapid fire and armor piercing, which makes them good against high HP and armored enemies, but especially effective against a tier 2 enemy like the Bulwark Brute (armored \+ high hp)

Enemies flinch when hit (they stop walking). Only the Unstoppable enemies don't.

## Win Condition

The objective is to survive as long as you can. You score points for every enemy killed.  
Every enemy type gives a set amount of points.

## Lose Condition

Team wipe

## Dead & Respawn

When a player dies, their camera moves to a surviving player.  
They can be revived if at least one player returns to their base.

**Pillars**

1. **Increasing Pressure:** A growing danger as stronger enemies appear with every wave.  
2. **Risk‑Weighted Looting:** Looted weapons have finite ammo. And since weapons counter enemies, players should consider letting other players grab specialized weapons to synergyze when fighting.  
3. **Toy‑First Combat:** Satisfying, readable “toys” (pushback shots, CC goo, AP rounds) with simple counters.  
4. **Drop‑in Co‑op:** Start/Join fast via Relay; short, intense runs.  
5. **Readable Minimalism:** Stylized sprites, no skeletal anim; juice via flashes, squash, particles, and 2D lights.

Success looks like: immediate pick‑up‑and‑play, clear risk/reward decisions, and memorable team “we pulled it off” moments.

---

## 2\. Core Loop

**Run → Grab Weapons → Fight → Score Points:** press deeper for better loot or peel out to survive the night.

**Objective:** Survive as long as possible while killing enemies to score points.

**Lose Conditions (tentative):**

* Squad wipe with no revives available.

---

## 3\. Progression

### Director (Pacing)

* Design varied waves of enemies combining enemy types.  
* Use synergetic enemy types to increase tension and difficulty and spawn simplistic waves to release it.

---

## 4\. Enemies

* *AP:* damage splits (True/Normal) to beat **Armored**.

* *CC:* immobilize/displace to beat **Fast/Jumper/Berserk**.

* *AoE/Spread:* cone/DoT to beat **Swarm/Poison**.

* *Sustain DPS:* time-to-damage against **Meaty/Ranged**.

* *Burst/Precision:* kill **Screamer/Ranged** quickly.

**Enemies flinch**: All except **Unstoppable** flinch on hit (brief move cancel).

### Tier 0

No taxonomies

#### Basic (Damned)

| Color | Misty Rose |
| :---- | :---- |
| Health | Base |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | None |
| Heuristics | Basic enemy |

### 4.1 Tier 1

The basic taxonomies.

#### Fast

| Color | Yellow |
| :---- | :---- |
| Health | Base |
| Speed | More |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | None |
| Heuristics | Easy to kill. Roots/nets/sonic push |

#### Armored

| Color | Gray |
| :---- | :---- |
| Health | Base |
| Speed | Base |
| Size | Base |
| Armor | Base |
| Damage | Base |
| Special | None |
| Heuristics | Use armor piercing weapons to kill them faster |

#### Meaty

| Color | Red |
| :---- | :---- |
| Health | More |
| Speed | Base |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | None |
| Heuristics | Rapid fire weapons and DoT (like fire) |

#### Ranged

| Color | Cyan |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Dodgeable Ranged Attacks |
| Heuristics | Keep moving to avoid the attacks. |

#### Swarm

| Color | Tan |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Less |
| Armor | None |
| Damage | Base |
| Special | Multiple units at once |
| Heuristics | AoE weapons (grenades, launchers, fire) |

#### Poison

| Color | Green |
| :---- | :---- |
| Health | Less |
| Speed | Less |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | DoT ranged attacks |
| Heuristics |  |

#### Screamer

| Color | Magenta |
| :---- | :---- |
| Health | Less |
| Speed | Less |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Attracts aggro |
| Heuristics | Focus on them, they are squishy |

#### Jumper

| Color | Orange |
| :---- | :---- |
| Health | Base |
| Speed | None |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Moves by jumping randomly towards the player |
| Heuristics | Predict where they will land and shoot them between jumps |

#### Berserk

| Color | Brown |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | More |
| Special | None |
| Heuristics | Team effort to slow and burst them |

#### Unstoppable

| Color | Purple |
| :---- | :---- |
| Health | Base |
| Speed | Base |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | Immune to CC |
| Heuristics | Burst or they'll reach you |

#### Explosive

| Color | Blue |
| :---- | :---- |
| Health | Base |
| Speed | Less |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | Explodes when killed |
| Heuristics | Kill with ranged |

**TTK bands (solo baseline, mid gear):** Swarm 0.2–0.6s; Fast 0.5–1.0s; Screamer 0.5–0.8s; Ranged 0.8–1.2s (1.5–2.0s in cover); Poison 0.8–1.5s; Armored 2–3s (0.6–1.0s with AP); Meaty 3–5s (≈2s with combo); Jumper 1.0–1.8s; Berserk 1.5–2.5s (6–8s if uncountered).

### 4.2 Tier 2

Every type combines 2 of the basic taxonomies

#### Riot Runner

| Color | Yellow and Grey |
| :---- | :---- |
| Health | Base |
| Speed | More |
| Size | Base |
| Armor | Base |
| Damage | Base |
| Special | None |
| Heuristics | Root and Armor Piercing burst |

#### Bile Hulk

| Color | Red and Green |
| :---- | :---- |
| Health | More |
| Speed | Less |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | DoT ranged attacks |
| Heuristics | Rapid fire weapons and DoT (like fire) |

#### Howler Marksman

| Color | Magenta and Cyan |
| :---- | :---- |
| Health | Less |
| Speed | Less |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Attracts aggro, Dodgeable Ranged Attacks |
| Heuristics | Focus on them, they are squishy |

#### Shock Pouncer

| Color | Brown and Orange |
| :---- | :---- |
| Health | Less |
| Speed | None |
| Size | Base |
| Armor | None |
| Damage | More |
| Special | Moves by jumping randomly towards the player |
| Heuristics | Team effort to focus them between jumps |

#### Brood Spitter

| Color | Cyan and Tan |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Ranged Dodgeable Attacks, Multiple Units Together |
| Heuristics | Use AoE and DoTs to wipe them |

#### Bulwark Brute

| Color | Red and Grey |
| :---- | :---- |
| Health | More |
| Speed | Base |
| Size | More |
| Armor | Base |
| Damage | Base |
| Special | None |
| Heuristics | Use assault rifles to mow it down |

**V1 (Demo) ENDS HERE**

**Spawn rules:** Early waves avoid synergy pairs (e.g., Screamer+Swarm), but they become more common with progression.

### 4.3 Tier 3

Every type combines 3 of the basic taxonomies

### 4.4 Tier 4

Every type combines 4 of the basic taxonomies

### 4.5 Tier 5 \- Elites

Every type combines 2 of the basic taxonomies

### 4.6 Tier 6 \- Bosses

6 taxonomies combined in each

#### Black Mother

| Color | Black |
| :---- | :---- |
| Health | A Lot |
| Speed | Base |
| Size | Huge |
| Armor | None |
| Damage | A Lot |
| Special | Dodgeable Ranged attacks, Poison Zone, Immune To CC, Attracts Aggro |
| Heuristics | You need to keep moving to dodge, and manage to hit her between the aggro. |

#### White Charger

| Color | White |
| :---- | :---- |
| Health | A Lot |
| Speed | Very Fast |
| Size | Huge |
| Armor | A Lot |
| Damage | A Lot |
| Special | Explodes when killed, Attracts Aggro |
| Heuristics | CC, DoT, chip him down and then kill with ranged |

---

## 5\. Weapons & Gear

### Effects

#### Armor & Armor Piercing

Armor is a second hp bar that absorbs a certain amount of damage. So if a creature has an armor of 4 and is hit by a weapon with damage of 6, the armor absorbs 4 of the 6 damage points and only 2 damage points reduce health. The armor's durability (the second hp bar) is reduced by the absorbed amount. Armor piercing damage is split between true damage and normal damage. So if a weapon deals 3 true damage and 3 normal damage, and the enemy has 2 armor, then the weapon deals its 3 true damage, and the armor blocks 2 of the 3 normal damage. In total the hit caused 4 damage. If the same weapon hits against an unarmored enemy, it would deal 6 damage. 

Armor model (ablative, per-hit):  
\- Armor \= (BlockPerHit, Durability).  
\- A damage payload \= (True, Normal).  
\- Per hit:  
  blocked \= min(BlockPerHit, Durability, Normal)  
  Durability \-= blocked  
  HealthDamage \= True \+ (Normal \- blocked)  
  HP \= max(0, HP \- HealthDamage)  
\- Examples:  
  (0,6) vs Armor(4, D=10) → Blocked 4, HP-2, D=6  
  (3,3) vs Armor(2, D=10) → Blocked 2, HP-(3+1)=4, D=8

### Ranged Tier 1

#### Crossbow

#### 

| Firing Rate | Low |
| :---- | :---- |
| Noise | None |
| Range | Long |
| Damage Per Projectile | Medium |
| Target | Single Target |
| Special | Armor Piercing |
| Heuristic |  |

#### Pistol

#### 

| Firing Rate | Medium |
| :---- | :---- |
| Noise | Medium |
| Range | Medium |
| Damage Per Projectile | Medium |
| Target | Single Target |
| Ammo | Normal |
| Special | None |
| Heuristic |  |

#### Rifle

| Firing Rate | Slow |
| :---- | :---- |
| Noise | Medium |
| Range | Long |
| Damage Per Projectile | High |
| Target | Single Target |
| Ammo | Armor Piercing |
| Special | Armor piercing |
| Heuristic | First armor piercing weapon |

#### Submachine Gun

#### 

| Firing Rate | High |
| :---- | :---- |
| Noise | High |
| Range | Medium |
| Damage Per Projectile | Medium |
| Target | Single Target |
| Ammo | Normal |
| Special | None |
| Heuristic | High damage from firing rate. Good to mow down meaty enemies |

#### Shotgun

| Firing Rate | Medium |
| :---- | :---- |
| Noise | High |
| Range | Short |
| Damage Per Projectile | Medium |
| Target | Cone |
| Ammo | Shotgun |
| Special | Pushback, multishot |
| Heuristic | Multiple projectiles in a cone. The closer the enemy the more hits they receive, which increases damage and pushback |

### Tier 2

#### Assault Rifle

| Firing Rate | High |
| :---- | :---- |
| Noise | High |
| Range | Long |
| Damage Per Projectile | High |
| Target | Single Target |
| Ammo | Armor piercing |
| Special | Armor Piercing |
| Heuristic | Armor Piercing \+ Rapid Fire, perfect against Bulwark Brute |

#### Goo Thrower

| Firing Rate | Continuous |
| :---- | :---- |
| Noise | Low |
| Range | Short |
| Damage Per Projectile | None |
| Target | Area |
| Ammo | Goo Tank |
| Special | Creates Slow Zone |
| Heuristic | Zone control to fight better |

#### Net Gun

| Firing Rate | Slow |
| :---- | :---- |
| Noise | Low |
| Range | Medium |
| Damage Per Projectile | None |
| Target | Single Target |
| Ammo | Nets |
| Special | Roots the target to the ground |

#### Flame Thrower

| Firing Rate | Continuous |
| :---- | :---- |
| Noise | Medium |
| Range | Short |
| Damage Per Projectile | DoT |
| Target | Area |
| Ammo | Fuel Tank |
| Special | Leaves fire zones |

**V1 (Demo) ENDS HERE**

## Tier 3 and beyond

* **Tech CC (Lab):** sonic pusher.  
* **High‑Caliber/AP:** Battle rifle, AP crossbow bolts, shaped charges.  
* **Sustained DPS:** LMG, belt‑shotgun, chem sprayer (fire/acid).  
* **Explosives:** Pipe/cluster bombs, remote charges, fuel toss.  
* **Silent:** suppressed pistols (Screamer duty).  
* **Utility:** Riot shield, smoke/flash, decoys, gas mask, med/antidote.

**Mods:** Flashlight mounts, noise dampeners, barbed shovel, damage amp paint (marks target).

**Loot gradient:** Higher tiers in inner rings; elite drops teach counters (AP mags from Armored elites, filters from Poison elites).

---

## 6\. Controls & Feel

* **Move:** WASD/left stick.  
* **Aim:** Mouse cursor/right stick.  
* **Fire/Alt:** LMB/RMB or triggers.  
* **Dash:** Shift/B.  
* **Use:** E / West Button.

**Feel cheats:** hit‑stop on melee, camera impulse on big hits, muzzle flashes, flashlight cone, screen vignette at high intensity.

**DEMO Version**

* **Move:** WASD/left stick.  
* **Aim:** Mouse cursor/right stick.  
* **Fire/Alt:** LMB / Trigger.  
* **Dash:** Shift / South button.  
* **Use/Revive:** E / West Button

**Feel cheats:** red vignette when player gets hurt, camera impulse on big hits, muzzle flashes, flashlight cone, screen vignette at high intensity.

---

## 7\. Art & Audio Direction

* **Sprites only**; No skeletal nor frame animations. “Animation” via sprite swap, color flash, scale squash/stretch, micro‑hops, particles, trails. Slower enemies Hop, faster enemies slide.   
  1. For example, super heavy enemies have big slow, heavy hops that can even be accompanied by camera shake. And faster enemies just move towards you.  
  2. Enemies have an idle sprite (when they haven't noticed you), an aggro sprite (when they notice you and start attacking) and some have a hurt sprite (when they get hit). These are just swapped on event triggers.  
  3. Melee attacks are just a slash visual effect  
* **Palette:** Fixed palette \+ gradient map post to unify AI‑generated art.  
* **Lighting:** URP 2D Lights (flashlight cones, neon, portal glow).  
* **UI:** Clean HUD with health, ammo, night timer, compass to center, join code overlay, ping markers.  
* **Audio:** Punchy SFX; music layers driven by Director Intensity.

---

## 8\. Systems (Design \+ Tech)

### 8.1 Simulation & Determinism

* **Authoritative ServerSim** at 50 Hz (IClock.FixedDelta \= 0.02).  
* Central **XorShift RNG** (seeded per run); snapshot as source of truth.  
* **InputCommand** (tick, move, aim, buttons) → server applies → **WorldSnapshot** (positions, HP, flags).

### 8.2 Networking

* **NGO \+ Unity Transport \+ Relay**: host authoritative; clients send inputs; server replicates state.  
* **Native:** DTLS/UDP; **WebGL:** WSS.  
* **Join‑in‑progress:** Host sends seed \+ nearby cells \+ actors; client spawns at outskirts with brief grace.  
* **Interest management:** per grid cell.

### 8.3 Spawning & Balance

`weight(type) = baseByTier[type,r] × timeMultiplier[n] × directorScale`

* Enemy type defines baseline; playtime increases spawn rate; Director modulates synergetic types spikes.

---

## 9\. Onboarding & Accessibility

Player will appear with the base gun. Shooting is intuitive, they will see an enemy and learn to kill it. When they find a gun on the floor they will see there's loot. Onboarding will just be natural play.

We won't add accessibility for the demo

---

## 10\. Telemetry (for tuning)

* Median **TTK per type**, 5th/95th percentiles.  
* **Counter usage** (% of kills where intended counter used).  
* **Time‑to‑first‑kill** per spike; **deaths heatmap**; run seeds and time reached.

---

## 11\. Milestones

* **v0.4.0 (SP slice):** Map, scoring, base gun, base enemy Reached  
* **v0.8.0 (MP):** Relay host/join, two players moving, snapshot basics, WebGL build. Reached  
* **v0.12.0 (AI):** Director, Tier 0 enemy, 6 Tier 1 enemies, 3 Tier 2 enemies, perf caps.  
* **v1.0.0 (Demo):** UX polish, telemetry, public WebGL page.

---

## 12\. Risks & Mitigations

* **Art inconsistency (AI assets):** enforce palette/gradient map pipeline.  
* **Relay latency (WebGL):** interpolation buffers, reduced spike frequency at high RTT.  
* **Cheating:** host authority, seed‑based loot, sanity checks.  
* **Scope creep:** single city theme; weapon families limited; 6 Tier 1 enemies \+ 3 Tier 2 enemies for 1.0 demo.

---

## 13\. Glossary

* **Director:** pacing system controlling spikes based on Intensity.  
* **TTK:** Time To Kill (target bands per enemy type).  
* **Snapshot:** serialized world state sent to clients.

Implementation notes live in `docs/architecture/overview.md` and `.cursorrules`. This GDD is the Product truth for Cursor and contributors.

# Hellscape 2 — Game Design Document (GDD)

Peer to peer matchmaking. One player hosts the game When you join, action starts right away.  
You are in a post apocalyptic world and monsters come from every direction.  
You and your friends start with nothing and just run, until you find your first weapon, which can just be a melee shovel.   
I'm thinking the map is a city and its surroundings. The further you are from the city's center, the weaker the monsters, but the objective is to get to the city center and destroy a portal or something.   
To add more pressure, there's a night count, every night stronger monsters spread outwards from the city center. Naturally, with some special cool RNG exceptions, weapons and armor are stronger the deeper you go into the city because it's where police precincts, military complexes and tech industries are. Very simple move and shoot. Find weapons in buildings. And I'm thinking about making it 2D top down view. That way I can ask for sprites from AI generators. And make it some style of art that allows me to not even animate it... somehow

| Enemies |  | Weapons |  |  |
| :---- | :---- | :---- | :---- | :---- |
| Fast | 1x Damage 3x Speed 1x Health 1x Units | Crowd Control | Mud, Roots, Pushback |  |
| Armored (Negates damage) | 1x Damage 1x Speed 0.5x Health 1x Units | Armor piercing | High Calibre |  |
| Meaty (High HP) | 1x Damage 1x Speed 3x Health 1x Units | Rapid Fire | Machine Guns |  |
| Ranged |  | Volley | Bows |  |
| Swarm | 1x Damage 1x Speed 0.5x Health 5x Units | Explosives |  |  |
| Poison |  | Silent | Bows, melee |  |
| Screamer |  |  |  |  |
| Jumpers (predict where they land) |  |  |  |  |
| Berserk (High Damage) | 3x Damage 1x Speed 1x Health 1x Units |  |  |  |
| Unstoppable |  |  |  |  |
| Explosive |  |  |  |  |
| All (Last Boss) |  |  |  |  |
| All (Last Boss) |  |  |  |  |

| Armor | Damage | Result |
| :---- | :---- | :---- |
| 4 | 6 | 2 |
| 4 | 3-3 | 3 |
| 3 | 6 | 3 |
| 3 | 3-3 | 3 |
| 6 | 6 | 0 |
| 6 | 3-3 | 3 |
| 1 | 6 | 5 |
| 1 | 3-3 | 5 |

**Version:** 0.1 (living doc) **Project:** Hellscape **Genre:** 2D top‑down co‑op survival‑run **Camera:** Orthographic (URP 2D), top‑down **Platforms:** PC, WebGL, iOS/Android (Unity 6, NGO \+ Relay) **Mode(s):** Single‑player (local host) & 2–16 player drop‑in co‑op

Drop-in co-op run into a burning city. Each night, a corruption ring expands outward. Stronger loot and enemies lie deeper toward the center. Survive, scavenge, push the ring back by destroying the portal—before it swallows the last safe blocks.

---

## 1\. Vision & Pillars

The first version of the game will include Tiers 0, 1 and 2 of enemies and weapons, with their corresponding city circles (Rural Fringe, Exurbs and Suburban Band)

**Elevator pitch:** Fight inward through a collapsing city. Each night, corruption expands outward; the best loot lies deeper toward the center. Scavenge, coordinate soft roles, and destroy the portal before the ring swallows the last safe blocks.

**Description:** Peer to peer multiplayer. One player hosts the game. You start on the outskirts of a postapocalyptic city. You are safe out here, because the further you are from the city's center, the weaker the monsters. But corruption has continued to spread, so you need to get back in there and try to destroy the portal from which the monsters are coming from.  
In the beginning, you and your friends only have shovels. With some special cool RNG exceptions, weapons and armor are stronger the deeper you go into the city because it's where police precincts, military complexes and tech industries are. 

Enemies have clear taxonomies. Normal enemies have one specialization, and stronger enemies start to combine them.  
In the beginning, the outskirts only have tier 0 enemies, the basic one. This first day is the chance for players to gather their first weapons and armor, and if they dare maybe they go in a little deeper to try their luck.  
They will also experience the first night in which they'll see how tier 1 monsters move out to the outskirts.  
So they have a set amount of days before the next tier of monsters reach this area.

Very simple move and shoot. Find weapons in buildings. 2D top down view, the camera sticks to the player.   
No skeletal nor frame animations. “Animation” via sprite swap, color flash, scale squash/stretch, micro‑hops, particles, trails. Slower enemies Hop, faster enemies slide.   
For example, super heavy enemies have big slow, heavy hops that can even be accompanied by camera shake. And faster enemies just move towards you.  
Enemies have an idle sprite (when they haven't noticed you), an aggro sprite (when they notice you and start attacking) and some have a hurt sprite (when they get hit)  
Melee attacks are just a slash visual effect

Peer to peer matchmaking. One player hosts the game.

Weapons have taxonomies that make them especially effective against some enemies

Rifles are armor piercing, so they are more effective against armored enemies. But they fire slowly so they aren't good against swarms.

Submachine guns have a very high fire rate, so they are very good at mowing down a single, big, unarmored high HP enemy. But they aren't very accurate and the damage per bullet isn't that high, so they aren't good with swarms either.

Shotguns deal damage in a cone and push back, so they are good both with fast enemies and swarms, because you don't have to be so precise. But they don't deal damage very fast nor are they armor piercing, so they don't perform against armored or high HP enemies.

Higher tier weapons combine these taxonomies.

Assault rifles are the combination of rifles and machine guns, so they are both rapid fire and armor piercing, which makes them good against high HP and armored enemies, but especially effective against a tier 2 enemy like the Bulwark Brute (armored \+ high hp)

Enemies flinch when hit (they stop walking). Only the Unstoppable enemies don't.

## Win Condition

The objective is to get to the city center and destroy a portal.  
**For the Demo**, players win when they try to move beyond Tier 2, the suburban band. 

## Lose Condition

Team wipe

## Dead & Respawn

When a player dies, their camera moves to a surviving player.  
They can be revived if at least one player returns to their base.

**Pillars**

6. **Radial Pressure:** A growing danger ring from city center; safety shrinks every night.  
7. **Risk‑Weighted Loot:** Better gear in inner rings (police, military, tech).  
8. **Toy‑First Combat:** Satisfying, readable “toys” (shovel, CC goo, AP rounds) with simple counters.  
9. **Drop‑in Co‑op:** Start/Join fast via Relay; short, intense runs.  
10. **Readable Minimalism:** Stylized sprites, no skeletal anim; juice via flashes, squash, particles, and 2D lights.

Success looks like: immediate pick‑up‑and‑play, clear risk/reward decisions, and memorable team “we pulled it off” moments.

---

## 2\. Core Loop

**Run → Scavenge → Fight (make noise) → Attract horde → Decide:** press deeper for better loot or peel out to survive the night.

**Objective:** Reach downtown and destroy the **Portal Node** before corruption overtakes the outskirts.

**Lose Conditions (tentative):**

* Squad wipe with no revives available.  
* Corruption reaches the safe perimeter (final outer ring) **and** portal remains active.

---

## 3\. World & Progression

### 3.1 City as Concentric Rings

* **Grid:** Seeded procedural grid (Chebyshev rings): Rural Fringe → Exurbs → Suburban Band → Industrial Belt → Midtown → Civic Core → Portal Keep  
* **Nights**: Each night increases CorruptionRadius. Newly corrupted rings start using the next tier’s enemy pool. Per-enemy stats do not scale with night.  
* **Radius Index r:** Higher \= farther from center; There's better loot closer to the center.  
* **Buildings:** Prefabbed blocks (residential, police, warehouse, lab). Simple interiors (line‑of‑sight via tile occlusion).

### 3.2 Night System

* **Day length:** \~5 minutes (configurable).  
* **NightCount** increments each full cycle.  
* **Corruption Radius** increases each night; corrupt cells spawn tougher enemies and hazards.  
* Visual: dark ring with emissive veins; gameplay: spawn multipliers, higher tier chance.

### 3.3 Director (Pacing)

* Maintains **Intensity** (0..1) from: recent damage, low ammo/meds, time since last spike.  
* Spawns **spikes** (mini‑hordes, specials) when intensity is low; cools down after peaks.

---

## 4\. Enemies

* *AP:* damage splits (True/Normal) to beat **Armored**.

* *CC:* immobilize/displace to beat **Fast/Jumper/Berserk**.

* *AoE/Spread:* cone/DoT to beat **Swarm/Poison**.

* *Sustain DPS:* time-to-damage against **Meaty/Ranged**.

* *Burst/Precision:* kill **Screamer/Ranged** quickly.

**Enemies flinch**: All except **Unstoppable** flinch on hit (brief move cancel).

### Tier 0

#### Basic (Damned)

| Color | Misty Rose |
| :---- | :---- |
| Health | Base |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | None |
| Heuristics | Basic enemy |

### 4.1 Tier 1

#### Fast

| Color | Yellow |
| :---- | :---- |
| Health | Base |
| Speed | More |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | None |
| Heuristics | Easy to kill. Roots/nets/sonic push |

#### Armored

| Color | Gray |
| :---- | :---- |
| Health | Base |
| Speed | Base |
| Size | Base |
| Armor | Base |
| Damage | Base |
| Special | None |
| Heuristics | Use armor piercing weapons to kill them faster |

#### Meaty

| Color | Red |
| :---- | :---- |
| Health | More |
| Speed | Base |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | None |
| Heuristics | Rapid fire weapons and DoT (like fire) |

#### Ranged

| Color | Cyan |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Dodgeable Ranged Attacks |
| Heuristics | Keep moving to avoid the attacks. |

#### Swarm

| Color | Tan |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Less |
| Armor | None |
| Damage | Base |
| Special | Multiple units at once |
| Heuristics | AoE weapons (grenades, launchers, fire) |

#### Poison

| Color | Green |
| :---- | :---- |
| Health | Less |
| Speed | Less |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | DoT ranged attacks |
| Heuristics |  |

#### Screamer

| Color | Magenta |
| :---- | :---- |
| Health | Less |
| Speed | Less |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Attracts aggro |
| Heuristics | Focus on them, they are squishy |

#### Jumper

| Color | Orange |
| :---- | :---- |
| Health | Base |
| Speed | None |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Moves by jumping randomly towards the player |
| Heuristics | Predict where they will land and shoot them between jumps |

#### Berserk

| Color | Brown |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | More |
| Special | None |
| Heuristics | Team effort to slow and burst them |

#### Unstoppable

| Color | Purple |
| :---- | :---- |
| Health | Base |
| Speed | Base |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | Immune to CC |
| Heuristics | Burst or they'll reach you |

#### Explosive

| Color | Blue |
| :---- | :---- |
| Health | Base |
| Speed | Less |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | Explodes when killed |
| Heuristics | Kill with ranged |

**TTK bands (solo baseline, mid gear):** Swarm 0.2–0.6s; Fast 0.5–1.0s; Screamer 0.5–0.8s; Ranged 0.8–1.2s (1.5–2.0s in cover); Poison 0.8–1.5s; Armored 2–3s (0.6–1.0s with AP); Meaty 3–5s (≈2s with combo); Jumper 1.0–1.8s; Berserk 1.5–2.5s (6–8s if uncountered).

### 4.2 Tier 2

#### Riot Runner

| Color | Yellow and Grey |
| :---- | :---- |
| Health | Base |
| Speed | More |
| Size | Base |
| Armor | Base |
| Damage | Base |
| Special | None |
| Heuristics | Root and Armor Piercing burst |

#### Bile Hulk

| Color | Red and Green |
| :---- | :---- |
| Health | More |
| Speed | Less |
| Size | More |
| Armor | None |
| Damage | Base |
| Special | DoT ranged attacks |
| Heuristics | Rapid fire weapons and DoT (like fire) |

#### Howler Marksman

| Color | Magenta and Cyan |
| :---- | :---- |
| Health | Less |
| Speed | Less |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Attracts aggro, Dodgeable Ranged Attacks |
| Heuristics | Focus on them, they are squishy |

#### Shock Pouncer

| Color | Brown and Orange |
| :---- | :---- |
| Health | Less |
| Speed | None |
| Size | Base |
| Armor | None |
| Damage | More |
| Special | Moves by jumping randomly towards the player |
| Heuristics | Team effort to focus them between jumps |

#### Brood Spitter

| Color | Cyan and Tan |
| :---- | :---- |
| Health | Less |
| Speed | Base |
| Size | Base |
| Armor | None |
| Damage | Base |
| Special | Ranged Dodgeable Attacks, Multiple Units Together |
| Heuristics | Use AoE and DoTs to wipe them |

#### Bulwark Brute

| Color | Red and Grey |
| :---- | :---- |
| Health | More |
| Speed | Base |
| Size | More |
| Armor | Base |
| Damage | Base |
| Special | None |
| Heuristics | Use assault rifles to mow it down |

**V1 (Demo) ENDS HERE**

**Spawn rules:** Early rings avoid synergy pairs (e.g., Screamer+Swarm) unless telegraphs are generous.

### Tier 4

### Tier 5 \- Elites

None of them have the screamer skill

### Tier 6 \- Bosses

6 taxonomies combined in each

#### Black Mother

| Color | Black |
| :---- | :---- |
| Health | A Lot |
| Speed | Base |
| Size | Huge |
| Armor | None |
| Damage | A Lot |
| Special | Dodgeable Ranged attacks, Poison Zone, Immune To CC, Attracts Aggro |
| Heuristics | You need to keep moving to dodge, and manage to hit her between the aggro. |

#### White Charger

| Color | White |
| :---- | :---- |
| Health | A Lot |
| Speed | Very Fast |
| Size | Huge |
| Armor | A Lot |
| Damage | A Lot |
| Special | Explodes when killed, Attracts Aggro |
| Heuristics | CC, DoT, chip him down and then kill with ranged |

---

## 5\. Weapons & Gear

### Effects

#### Armor & Armor Piercing

Armor is a second hp bar that absorbs a certain amount of damage. So if a creature has an armor of 4 and is hit by a weapon with damage of 6, the armor absorbs 4 of the 6 damage points and only 2 damage points reduce health. The armor's durability (the second hp bar) is reduced by the absorbed amount. Armor piercing damage is split between true damage and normal damage. So if a weapon deals 3 true damage and 3 normal damage, and the enemy has 2 armor, then the weapon deals its 3 true damage, and the armor blocks 2 of the 3 normal damage. In total the hit caused 4 damage. If the same weapon hits against an unarmored enemy, it would deal 6 damage. 

Armor model (ablative, per-hit):  
\- Armor \= (BlockPerHit, Durability).  
\- A damage payload \= (True, Normal).  
\- Per hit:  
  blocked \= min(BlockPerHit, Durability, Normal)  
  Durability \-= blocked  
  HealthDamage \= True \+ (Normal \- blocked)  
  HP \= max(0, HP \- HealthDamage)  
\- Examples:  
  (0,6) vs Armor(4, D=10) → Blocked 4, HP-2, D=6  
  (3,3) vs Armor(2, D=10) → Blocked 2, HP-(3+1)=4, D=8

### Melee Tier 0

#### Shovel

| Attack Rate | Low |
| :---- | :---- |
| Noise | None |
| Range | Melee |
| Damage Per Attack | Low |
| Target | Single Target |
| Ammo | None |
| Special | None |
| Heuristic |  |

### Melee Tier 1

#### Hammer

#### 

| Attack Rate | Low |
| :---- | :---- |
| Noise | None |
| Range | Melee |
| Damage Per Attack | Medium |
| Target | Single Target |
| Ammo | None |
| Special | Armor Piercing |
| Heuristic |  |

#### Spear

| Attack Rate | Medium |
| :---- | :---- |
| Noise | None |
| Range | Melee |
| Damage Per Attack | Medium |
| Target | Single Target |
| Ammo | None |
| Special | Double melee range |
| Heuristic |  |

#### Axe

#### 

| Attack Rate | Low |
| :---- | :---- |
| Noise | None |
| Range | Melee |
| Damage Per Attack | High |
| Target | Single Target |
| Ammo | None |
| Special | Armor Piercing |
| Heuristic |  |

#### Sword

| Attack Rate | Fast |
| :---- | :---- |
| Noise | None |
| Range | Melee |
| Damage Per Attack | High |
| Target | Single Target |
| Ammo | None |
| Special | None |
| Heuristic |  |

### Ranged Tier 1

#### Crossbow

#### 

| Firing Rate | Low |
| :---- | :---- |
| Noise | None |
| Range | Long |
| Damage Per Projectile | Medium |
| Target | Single Target |
| Special | Armor Piercing |
| Heuristic |  |

#### Pistol

#### 

| Firing Rate | Medium |
| :---- | :---- |
| Noise | Medium |
| Range | Medium |
| Damage Per Projectile | Medium |
| Target | Single Target |
| Ammo | Normal |
| Special | None |
| Heuristic |  |

#### Rifle

| Firing Rate | Slow |
| :---- | :---- |
| Noise | Medium |
| Range | Long |
| Damage Per Projectile | High |
| Target | Single Target |
| Ammo | Armor Piercing |
| Special | Armor piercing |
| Heuristic | First armor piercing weapon |

#### Submachine Gun

#### 

| Firing Rate | High |
| :---- | :---- |
| Noise | High |
| Range | Medium |
| Damage Per Projectile | Medium |
| Target | Single Target |
| Ammo | Normal |
| Special | None |
| Heuristic | High damage from firing rate. Good to mow down meaty enemies |

#### Shotgun

| Firing Rate | Medium |
| :---- | :---- |
| Noise | High |
| Range | Short |
| Damage Per Projectile | Medium |
| Target | Cone |
| Ammo | Shotgun |
| Special | Pushback, multishot |
| Heuristic | Multiple projectiles in a cone. The closer the enemy the more hits they receive, which increases damage and pushback |

### Tier 2

#### Assault Rifle

| Firing Rate | High |
| :---- | :---- |
| Noise | High |
| Range | Long |
| Damage Per Projectile | High |
| Target | Single Target |
| Ammo | Armor piercing |
| Special | Armor Piercing |
| Heuristic | Armor Piercing \+ Rapid Fire, perfect against Bulwark Brute |

#### Goo Thrower

| Firing Rate | Continuous |
| :---- | :---- |
| Noise | Low |
| Range | Short |
| Damage Per Projectile | None |
| Target | Area |
| Ammo | Goo Tank |
| Special | Creates Slow Zone |
| Heuristic | Zone control to fight better |

#### Net Gun

| Firing Rate | Slow |
| :---- | :---- |
| Noise | Low |
| Range | Medium |
| Damage Per Projectile | None |
| Target | Single Target |
| Ammo | Nets |
| Special | Roots the target to the ground |

#### Flame Thrower

| Firing Rate | Continuous |
| :---- | :---- |
| Noise | Medium |
| Range | Short |
| Damage Per Projectile | DoT |
| Target | Area |
| Ammo | Fuel Tank |
| Special | Leaves fire zones |

**V1 (Demo) ENDS HERE**

## Tier 3 and beyond

* **Tech CC (Lab):** sonic pusher.  
* **High‑Caliber/AP:** Battle rifle, AP crossbow bolts, shaped charges.  
* **Sustained DPS:** LMG, belt‑shotgun, chem sprayer (fire/acid).  
* **Explosives:** Pipe/cluster bombs, remote charges, fuel toss.  
* **Silent:** suppressed pistols (Screamer duty).  
* **Utility:** Riot shield, smoke/flash, decoys, gas mask, med/antidote.

**Mods:** Flashlight mounts, noise dampeners, barbed shovel, damage amp paint (marks target).

**Loot gradient:** Higher tiers in inner rings; elite drops teach counters (AP mags from Armored elites, filters from Poison elites).

---

## Buildings

For the Demo, all buildings will just be squares with loot and enemies inside.  
When the player kills all the enemies, they can choose to mark the place as their base.  
When a building is marked as a base, the previous base gets unmarked  
Enemies and loot stop spawning in bases.  
When a player comes back to their base, dead players respawn

### T0 — Rural Fringe (Tier 0\)

**Biome/Buildings:** farm lots, barns, trailers, tool sheds, lone gas stops  
**Weapons:** shovel (starter), farm melee (machete, hatchet and hammer), **lucky** finds of hunting rifle, pistol, pump shotgun in farmhouses  
**Armor/Utility:** Antiseptic Spray  
**Notes:** “learn to move” zone; sparse ammo. Learn to fight with melee and try out your first weapons.

### T1 — Exurbs (Tier 1\)

**Biome/Buildings:** bait & tackle, pawn shops, **sheriff substation**, roadside motels  
**Weapons:** pistols, pump shotguns, hunting rifles, submachine guns  
**Armor/Utility:** bandages, sports pads/helmet (low BlockPerHit)  
**Notes:** Start feeling the counters between enemies and weapons. 

### T2 — Suburban Band (Tier 2\)

**Biome/Buildings:** big-box sporting goods, strip malls, small police precinct, pharmacies, garages  
 **Weapons:** Assault Rifles, Goo Throwers, Net Guns, Flame Throwers  
 **Armor/Utility:** Police vests, med kits  
 **Notes:** Introduce CC specialized weapons and area control.

**V1 (Demo) ENDS HERE. For the demo players win when they try to go to the suburbs.**

### T3 — Industrial Belt (Tier 3\)

**Biome/Buildings:** warehouses, railyards, **chemical plant**, power substation, security posts  
 **Weapons:** LMGs (rare), carbines, craftables: **pipe bombs**, thermite, ANFO (gated)  
 **Armor/Utility:** **respirators/gas masks**, welding masks, improvised **plate carriers** (BlockPerHit 2, mid durability)  
 **Notes:** explosives & hazard gear show up; first steady AP trickle

### T4 — Midtown (Tier 4\)

**Biome/Buildings:** mid-rise offices, clinics, **private security** hubs, tech startups  
 **Weapons:** DMRs, suppressed carbines, **taser/sonic pusher** (tech CC), breaching charges (rare)  
 **Armor/Utility:** trauma kits, **ballistic plates** (BlockPerHit 3, higher durability), keycards  
 **Notes:** mobility \+ utility spike; CC toys start appearing

### T5 — Civic Core (Tier 5\)

**Biome/Buildings:** **police HQ/SWAT**, courthouse, central hospital, **university labs**, metro control  
 **Weapons:** AP battle rifles, grenade launcher (less-lethal \+ HE), **goo/net guns** (lab CC), shaped charges  
 **Armor/Utility:** riot armor, full gas masks, NV/IR optics, anti-toxin kits  
 **Notes:** this is where counters crystallize (AP vs Armored, CC vs Fast/Jumper)

### T6 — Portal Keep (Tier 6\)

**Biome/Buildings:** barricaded plaza, **military checkpoint**, arcology vault, occult machinery  
 **Weapons:** exotic prototypes (**phase lance**, **sigil disruptor**, anti-portal charges), limited ammo caches  
 **Armor/Utility:** heavy plates (BlockPerHit 4–6, low durability), boss-only mods  
 **Notes:** dual wardens drop unique items; loot is endgame-defining but scarce

## 6\. Controls & Feel

* **Move:** WASD/left stick.  
* **Aim:** Mouse cursor/right stick.  
* **Fire/Alt:** LMB/RMB or triggers.  
* **Dash:** Shift/B.  
* **Use:** E / West Button.

**Feel cheats:** hit‑stop on melee, camera impulse on big hits, muzzle flashes, flashlight cone, screen vignette at high intensity.

**DEMO Version**

* **Move:** WASD/left stick.  
* **Aim:** Mouse cursor/right stick.  
* **Fire/Alt:** LMB / Trigger.  
* **Dash:** Shift / South button.  
* **Use/Revive:** E / West Button

---

## 7\. Art & Audio Direction

* **Sprites only**; No skeletal nor frame animations. “Animation” via sprite swap, color flash, scale squash/stretch, micro‑hops, particles, trails. Slower enemies Hop, faster enemies slide.   
  1. For example, super heavy enemies have big slow, heavy hops that can even be accompanied by camera shake. And faster enemies just move towards you.  
  2. Enemies have an idle sprite (when they haven't noticed you), an aggro sprite (when they notice you and start attacking) and some have a hurt sprite (when they get hit). These are just swapped on event triggers.  
  3. Melee attacks are just a slash visual effect  
* **Palette:** Fixed palette \+ gradient map post to unify AI‑generated art.  
* **Lighting:** URP 2D Lights (flashlight cones, neon, portal glow).  
* **UI:** Clean HUD with health, ammo, night timer, compass to center, join code overlay, ping markers.  
* **Audio:** Punchy SFX; music layers driven by Director Intensity.

---

## 8\. Systems (Design \+ Tech)

### 8.1 Simulation & Determinism

* **Authoritative ServerSim** at 50 Hz (IClock.FixedDelta \= 0.02).  
* Central **XorShift RNG** (seeded per run); snapshot as source of truth.  
* **InputCommand** (tick, move, aim, buttons) → server applies → **WorldSnapshot** (positions, HP, flags).

### 8.2 Networking

* **NGO \+ Unity Transport \+ Relay**: host authoritative; clients send inputs; server replicates state.  
* **Native:** DTLS/UDP; **WebGL:** WSS.  
* **Join‑in‑progress:** Host sends seed \+ nearby cells \+ actors; client spawns at outskirts with brief grace.  
* **Interest management (later):** per grid cell.

### 8.3 Spawning & Balance

`weight(type) = baseByRing[type,r] × nightMultiplier[n] × directorScale`

* Ring defines baseline; Night increases elites/spawn rate; Director modulates spikes.

**Demo Version**  
Enemies spawn randomly within their allowed city circles.

### 8.4 Hazards & Noise/Light

* Weapons emit **noise**; higher noise pulls aggro across cells.  
* **Light** affects Screamer/Jumper ranges slightly; flashbangs reset.

**DEMO VERSION**  
There are no flashbangs

---

## 9\. Onboarding & Accessibility

* **Onboarding:** Diegetic tips (graffiti/signs), safe tutorial block with shovel \+ first Screamer encounter.  
* **Accessibility:** Remappable controls, aim assist slider, color‑blind‑safe palette, subtitles/telegraphs, screen shake toggle.

**Demo Version**  
Player will appear with the shovel. Swinging is intuitive, they will see an enemy and learn to kill it. When they find a building they will see there's loot. Onboarding will just be natural play.

We won't add accessibility for the demo

---

## 10\. Telemetry (for tuning)

* Median **TTK per type**, 5th/95th percentiles.  
* **Counter usage** (% of kills where intended counter used).  
* **Time‑to‑first‑kill** per spike; **deaths heatmap**; run seeds and ring reached.

---

## 11\. Milestones

* **v0.4.0 (SP slice):** Night/Ring, city grid, shovel melee, pistol ranged, 2 tier 1 enemies.  
* **v0.8.0 (MP):** Relay host/join, two players moving, snapshot basics, WebGL build.  
* **v0.12.0 (AI):** Director, 6 Tier 1 enemies, 3 Tier 2 enemies, perf caps.  
* **v1.0.0 (Demo):** UX polish, telemetry, public WebGL page.

---

## 12\. Risks & Mitigations

* **Art inconsistency (AI assets):** enforce palette/gradient map pipeline.  
* **Relay latency (WebGL):** interpolation buffers, reduced spike frequency at high RTT.  
* **Cheating:** host authority, seed‑based loot, sanity checks.  
* **Scope creep:** single city theme; weapon families limited; 6 Tier 1 enemies \+ 3 Tier 2 enemies for 1.0 demo.

---

## 13\. Glossary

* **Ring/Radius Index:** distance band from center (Chebyshev metric).  
* **Director:** pacing system controlling spikes based on Intensity.  
* **TTK:** Time To Kill (target bands per enemy type).  
* **Snapshot:** serialized world state sent to clients.
