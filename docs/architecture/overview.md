# Hellscape Architecture Overview


**Goal:** A single authoritative simulation (ServerSim) that runs identically in SP and MP, with transport swapped via adapters. Unity hosts composition and presentation only.


## Hexagonal Modules
- Domain: NightSystem, CityGrid, Director, CombatResolver, EnemyType/SpawnTable DTOs, Snapshot/Codec, Ports.
- Net: NGO/Relay adapter implementing ITransport; LocalTransport for SP.
- Platform: UnityClock, XorShiftRng; (optional) Pathfinding service.
- Persistence: IConfig JSON/SO mappers, IStorage for saves.
- Presentation: Input, camera, VFX/SFX/UI; listeners for Domain events.


## Data flow
Input → InputCommand → ServerSim.Tick → DomainEvents + Snapshot → Presentation/Net replicate → Render.


## Join-in-progress
Client connects → requests snapshot → host sends seed + nearby cells + actors → client interpolates.


## Determinism
- One RNG instance.
- Fixed tick.
- Snapshot is the truth; clients interpolate.