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