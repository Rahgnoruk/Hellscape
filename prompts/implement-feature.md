You are Cursor acting on the Hellscape repo with .cursorrules. Implement the following feature using TDD and the hexagonal boundaries. Produce minimal diffs.


### Feature
<WRITE FEATURE HERE, e.g.: "Armored enemy type with AP counter and tests for TTK bands">


### Steps
1) Write failing tests in Hellscape.Tests (unit + an integration if needed).
2) Implement Domain code to satisfy tests (no UnityEngine).
3) Add/adjust DTOs and keep snapshot codec roundtrip tests green.
4) If Presentation needed, add adapters/MonoBehaviours without rules.
5) Run all tests; ensure no warnings.


### Definition of Done
- Tests pass.
- Domain untouched by Unity refs.
- Snapshot roundtrip unchanged (or golden updated intentionally with review).
- Documentation: update docs/architecture or docs/testing if contracts changed.