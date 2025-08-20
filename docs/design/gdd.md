# Hellscape — Game Design Document (GDD)

**Version:** 0.1 (living doc)
**Project:** Hellscape
**Genre:** 2D top‑down co‑op survival‑run
**Camera:** Orthographic (URP 2D), fixed top‑down
**Platforms:** PC, WebGL, iOS/Android (Unity 6, NGO + Relay)
**Mode(s):** Single‑player (local host) & 2–4 player drop‑in co‑op

---

## 1. Vision & Pillars

**Elevator pitch:** Fight inward through a collapsing city. Each night, corruption expands outward; the best loot lies deeper toward the center. Scavenge, coordinate soft roles, and destroy the portal before the ring swallows the last safe blocks.

**Pillars**

1. **Radial Pressure:** A growing danger ring from city center; safety shrinks every night.
2. **Risk‑Weighted Loot:** Better gear in inner rings (police, military, tech).
3. **Toy‑First Combat:** Satisfying, readable “toys” (shovel, CC goo, AP rounds) with simple counters.
4. **Drop‑in Co‑op:** Start/Join fast via Relay; short, intense runs.
5. **Readable Minimalism:** Stylized sprites, no skeletal anim; juice via flashes, squash, particles, and 2D lights.

Success looks like: immediate pick‑up‑and‑play, clear risk/reward decisions, and memorable team “we pulled it off” moments.

---

## 2. Core Loop

**Run → Scavenge → Fight (make noise) → Attract horde → Decide:** press deeper for better loot or peel out to survive the night.

**Objective:** Reach downtown and destroy the **Portal Node** before corruption overtakes the outskirts.

**Lose Conditions (tentative):**

* Squad wipe with no revives available.
* Corruption reaches the safe perimeter (final outer ring) **and** portal remains active.

---

## 3. World & Progression

### 3.1 City as Concentric Rings

* **Grid:** Seeded procedural grid (Chebyshev rings): Suburbs → Industrial → Downtown.
* **Radius Index r:** Higher = farther from center; drives spawn difficulty and loot tier.
* **Buildings:** Prefabbed blocks (residential, police, warehouse, lab). Simple interiors (line‑of‑sight via tile occlusion).

### 3.2 Night System

* **Day length:** \~5 minutes (configurable).
* **NightCount** increments each full cycle.
* **Corruption Radius** increases each night; corrupt cells spawn tougher mixes and hazards.
* Visual: dark ring with emissive veins; gameplay: spawn multipliers, elite chance.

### 3.3 Director (Pacing)

* Maintains **Intensity** (0..1) from: recent damage, low ammo/meds, time since last spike.
* Spawns **spikes** (mini‑hordes, specials) when intensity is low; cools down after peaks.

---

## 4. Enemies

### 4.1 Common Types (single tag)

* **Fast (Y):** Rush, low HP. *Counter:* roots/nets/sonic push.
* **Armored (Gray):** DR vs small arms. *Counter:* AP rounds, shaped charges.
* **Meaty (Red):** High HP. *Counter:* sustained DPS, fire/DoT.
* **Ranged (Cyan):** Shoots from cover. *Counter:* smoke, suppression, shield push.
* **Swarm (Tan):** Many, fragile. *Counter:* explosives, flame/shrapnel.
* **Poison (Olive):** Clouds/DoT zones. *Counter:* filters, dispersal, range.
* **Screamer (Magenta):** Calls hordes. *Counter:* silence/stun, precision.
* **Jumper (Orange):** Leaps to predicted landings. *Counter:* spike/root at ring, shotgun punish.
* **Berserk (Brown):** Huge burst, short wind‑up. *Counter:* stagger/freeze, shield block.

**TTK bands (solo baseline, mid gear):**
Swarm 0.2–0.6s; Fast 0.5–1.0s; Screamer 0.5–0.8s; Ranged 0.8–1.2s (1.5–2.0s in cover); Poison 0.8–1.5s; Armored 2–3s (0.6–1.0s with AP); Meaty 3–5s (≈2s with combo); Jumper 1.0–1.8s; Berserk 1.5–2.5s (6–8s if uncountered).

### 4.2 Elites (dual tag)

* **Riot Runner (Fast+Armored):** Root → AP burst.
* **Bile Hulk (Meaty+Poison):** Filters → fire/DoT.
* **Howler Marksman (Screamer+Ranged):** Flash/silence → headshot.
* **Shock Pouncer (Jumper+Berserk):** Trap landing → shotgun punish.
* **Brood Spitter (Ranged+Swarm):** Explosives/volley.
* **Bulwark Brute (Armored+Meaty):** Charge/LMG, or environmental crush.

**Spawn rules:** Early rings avoid synergy pairs (e.g., Screamer+Swarm) unless telegraphs are generous.

---

## 5. Weapons & Gear

**Starter:** Shovel (quiet melee).
**Families:**

* **Tech CC (Lab):** Goo launcher (slow/root), net gun, sonic pusher.
* **High‑Caliber/AP:** Battle rifle, AP crossbow bolts, shaped charges.
* **Sustained DPS:** LMG, belt‑shotgun, chem sprayer (fire/acid).
* **Explosives:** Pipe/cluster bombs, remote charges, fuel toss.
* **Silent:** Bows, suppressed pistols (Screamer duty).
* **Utility:** Riot shield, smoke/flash, decoys, gas mask, med/antidote.

**Mods:** Flashlight mounts, noise dampeners, barbed shovel, damage amp paint (marks target).

**Loot gradient:** Higher tiers in inner rings; elite drops teach counters (AP mags from Armored elites, filters from Poison elites).

---

## 6. Controls & Feel

* **Move:** WASD/left stick.
* **Aim:** Mouse cursor/right stick.
* **Fire/Alt:** LMB/RMB or triggers.
* **Dash:** Shift/B.
* **Use/Revive:** E/X.

**Feel cheats:** hit‑stop on melee, camera impulse on big hits, muzzle flashes, flashlight cone, screen vignette at high intensity.

---

## 7. Art & Audio Direction

* **Sprites only**; no skeletal animation. “Animation” via: color flash, scale squash/stretch, micro‑hops, particles, trails.
* **Palette:** Fixed palette + gradient map post to unify AI‑generated art.
* **Lighting:** URP 2D Lights (flashlight cones, neon, portal glow).
* **UI:** Clean HUD with health, ammo, night timer, compass to center, join code overlay, ping markers.
* **Audio:** Punchy SFX; music layers driven by Director Intensity.

---

## 8. Systems (Design + Tech)

### 8.1 Simulation & Determinism

* **Authoritative ServerSim** at 50 Hz (IClock.FixedDelta = 0.02).
* Central **XorShift RNG** (seeded per run); snapshot as source of truth.
* **InputCommand** (tick, move, aim, buttons) → server applies → **WorldSnapshot** (positions, HP, flags).

### 8.2 Networking

* **NGO + Unity Transport + Relay**: host authoritative; clients send inputs; server replicates state.
* **Native:** DTLS/UDP; **WebGL:** WSS.
* **Join‑in‑progress:** Host sends seed + nearby cells + actors; client spawns at outskirts with brief grace.
* **Interest management (later):** per grid cell.

### 8.3 Spawning & Balance

`weight(type) = baseByRing[type,r] × nightMultiplier[n] × directorScale`

* Ring defines baseline; Night increases elites/spawn rate; Director modulates spikes.

### 8.4 Hazards & Noise/Light

* Weapons emit **noise**; higher noise pulls aggro across cells.
* **Light** affects Screamer/Jumper ranges slightly; flashbangs reset.

---

## 9. Onboarding & Accessibility

* **Onboarding:** Diegetic tips (graffiti/signs), safe tutorial block with shovel + first Screamer encounter.
* **Accessibility:** Remappable controls, aim assist slider, color‑blind‑safe palette, subtitles/telegraphs, screen shake toggle.

---

## 10. Telemetry (for tuning)

* Median **TTK per type**, 5th/95th percentiles.
* **Counter usage** (% of kills where intended counter used).
* **Time‑to‑first‑kill** per spike; **deaths heatmap**; run seeds and ring reached.

---

## 11. Milestones

* **v0.4.0 (SP slice):** Night/Ring, city grid, shovel melee, pistol ranged, 2 enemy types, 8 tests.
* **v0.8.0 (MP):** Relay host/join, two players moving, snapshot basics, WebGL build.
* **v0.12.0 (AI):** Director, 6 commons, 3 elites, perf caps.
* **v1.0.0 (Demo):** UX polish, telemetry, public WebGL page.

---

## 12. Risks & Mitigations

* **Art inconsistency (AI assets):** enforce palette/gradient map pipeline.
* **Relay latency (WebGL):** interpolation buffers, reduced spike frequency at high RTT.
* **Cheating:** host authority, seed‑based loot, sanity checks.
* **Scope creep:** single city theme; weapon families limited; 6 commons + 3 elites for 1.0 demo.

---

## 13. Glossary

* **Ring/Radius Index:** distance band from center (Chebyshev metric).
* **Director:** pacing system controlling spikes based on Intensity.
* **TTK:** Time To Kill (target bands per enemy type).
* **Snapshot:** serialized world state sent to clients.

> Implementation notes live in `docs/architecture/overview.md` and `.cursorrules`. This GDD is the Product truth for Cursor and contributors.
