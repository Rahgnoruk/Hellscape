You are Cursor working on the Hellscape repo with .cursorrules loaded. Use strict TDD and hexagonal boundaries.

### Feature
Authoritative player movement driven by InputCommands + snapshot codec (SP-ready, MP-friendly).

### Scope
- DOMAIN ONLY for rules; Unity adapters in Platform/App as needed.
- Single Player path first (LocalTransport). No visuals beyond existing placeholder.

### Requirements
1) **DTOs (Domain)**
   - `InputCommand { int tick; float moveX, moveY; float aimX, aimY; byte buttons }`
   - `ActorId` (int), `ActorState { ActorId id; float x,y; float vx,vy; short hp; byte type }`
   - `WorldSnapshot { int tick; ActorState[] actors }`
2) **ServerSim (Domain)**
   - Track a single local player actor (id=1).
   - `Apply(InputCommand)` each tick.
   - Kinematic movement: speed param, acceleration, deceleration. No physics engine.
   - Optional dash: on button bit (e.g., 0x04), apply short impulse with cooldown.
3) **Snapshot codec (Domain)**
   - `SnapshotCodec.Encode(WorldSnapshot) -> byte[]`
   - `Decode(byte[]) -> WorldSnapshot`
   - Delta encoding is optional now; do full snapshot first. Keep structs blittable-friendly.
4) **Ports**
   - Use `IClock.FixedDelta`. Use `IRng` if needed; avoid Unity refs.
5) **Adapters (App/Platform)**
   - Wire SP: `UnityClock : IClock`.
   - Hook existing `LocalTransport` so a `PlayerInputHandler` can send commands to the sim (loopback).

### TDD Steps (write tests first in Assets/Hellscape/Scripts/Tests/)
- `Movement_ZeroInput_NoDrift`: with zero commands over N ticks, position remains constant.
- `Movement_Forward_AdvancesPredictably`: hold move=(1,0) for T ticks → expected x within epsilon.
- `Movement_Dash_ImpulseAndCooldown`: pressing dash sets higher displacement for a few ticks, then respects cooldown.
- `Snapshot_Roundtrip_Equal`: encode→decode returns identical actor data (use deterministic ordering).
- `Determinism_SameCommandsSameSeed_SameFinalState`: same seed + same command stream produce same snapshot hash.

### Contracts / acceptance
- Fixed tick from `IClock.FixedDelta` (use 0.02f in tests).
- Speed/accel/decay constants defined in Domain and exposed for tests.
- Snapshot byte order is little-endian; no allocations in hot path beyond arrays creation.
- Domain compiles with NO UnityEngine (enforced by asmdef).

### Files & placement
- Domain: `Assets/Hellscape/Scripts/Domain/` (DTOs, ServerSim, SnapshotCodec, ports).
- Tests (Edit Mode): `Assets/Hellscape/Scripts/Tests/`
- Platform/App: `UnityClock.cs` + minimal bootstrap to call `Apply` each FixedUpdate in SP.

### Definition of Done
- All new tests green.
- No UnityEngine in Domain.
- Provide a short diff summary + suggested Conventional Commit messages.