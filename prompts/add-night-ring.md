You are Cursor working on the Hellscape repo with the .cursorrules loaded.

Implement the first feature using strict TDD and the hexagonal architecture.

### Feature
Authoritative Night/Ring progression with deterministic RNG:
- NightSystem: ticks the day/night cycle (TimeOfDay 0..1, NightCount++ on wrap) and exposes CorruptionRadius that grows per night.
- CityGrid: radial indexing (radiusIndex) from center; tags tiles (Downtown/Industrial/Suburbs) by radius.
- DeterministicRng: single seeded RNG (XorShift32) exposed via IRng port.
- No UnityEngine references in Domain.

### Module & files
- Create/extend assembly definitions as needed.
- Place Domain code in: `Assets/Hellscape/Scripts/Domain/`
- Place ports in Domain: `IClock`, `IRng`.
- Place adapter stubs in Platform (but don’t implement yet): `Assets/Hellscape/Scripts/Platform/UnityClock.cs` (will return Time.fixedDelta later).

### TDD steps (do these explicitly)
1) Write failing **Edit Mode** tests in `Assets/Hellscape/Scripts/Tests/` (NUnit):
   - `NightSystemAdvancesNight`: advancing fixed ticks past one day increments NightCount and wraps TimeOfDay to [0,1).
   - `CorruptionRadiusGrows`: CorruptionRadius(night n+1) > CorruptionRadius(night n).
   - `DeterminismRng`: same seed → same sequence; different seed → different.
   - `CityGridRadiusIndex`: points further from center never have a smaller radius index than closer points.
2) Implement minimal Domain code to make tests pass (no UnityEngine usage).
3) Keep Domain pure: add `.asmdef` for Hellscape.Domain with **Override References = ON** so UnityEngine cannot be used.
4) Provide a short summary of diffs and ensure tests are green.

### Contracts / acceptance criteria
- Day length is parameterizable (float DayLengthSeconds; default 300s). TimeOfDay ∈ [0,1).
- CorruptionRadius = f(NightCount) is strictly monotonic (e.g., base + k * NightCount).
- DeterministicRng: XorShift32; float Next01 in [0,1).
- CityGrid: `RadiusIndex(int x,int y)` uses Chebyshev or max-abs distance; must be consistent with tests.
- Domain must compile with NO UnityEngine references.

### Definition of Done
- All new tests pass (green) in Edit Mode.
- No UnityEngine in Domain (enforced by asmdef).
- Clear, small diffs; Conventional Commit message suggestions.

After completing, output:
- The created/modified file list.
- The test results summary.
- Any follow-up TODOs (e.g., hook UnityClock in App layer).
