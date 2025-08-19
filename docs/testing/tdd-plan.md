# TDD Plan


## Acceptance (outside-in)
1) Night/ring progression works.
2) Spawn weights vary by ring/night.
3) Time To Kill bands enforce counters (Armored/AP, etc.).
4) Snapshot roundtrip produces same hash.


## Unit tests
- Combat math (EHP, DPS, stagger windows).
- Director intensity up/down.
- RNG determinism.
- Elite combos remain solvable.


## Golden masters
- Snapshot binary -> hash file per version.


Use NUnit in Edit Mode. Domain tests run with no UnityEngine.