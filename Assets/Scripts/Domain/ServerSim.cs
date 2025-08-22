using System.Collections.Generic;
using Hellscape.Domain;
using Hellscape.Domain.Combat;

namespace Hellscape.Domain {
    public sealed class ServerSim {
        private readonly DeterministicRng rng;
        private int tick;

        // World
        private CityGrid grid;
        private NightSystem night;
        private Director director;

        // Playfield bounds
        private readonly Vector2 playfieldHalfExtents = new Vector2(25f, 14f);

        // Actors (toy implementation)
        private readonly Dictionary<int, Actor> actors = new();
        public bool RemoveActor(int actorId) => actors.Remove(actorId);

        private int nextId = 1;

        private readonly Dictionary<int, InputCommand> _latestByActor = new();
        
        // Event system
        private readonly Queue<DomainEvent> eventQueue = new();
        
        // Shot events for VFX
        private readonly List<ShotEvent> _shotEvents = new();
        
        // Lives system
        private LifeSystem life;

        public ServerSim(int seed) {
            this.rng = new DeterministicRng(seed);
        }

        public void Start() {
            grid = new CityGrid(64, 64, 32); // width, height, centerRadius
            night = new NightSystem();
            director = new Director();
            life = new LifeSystem(10f);
        }

        public void Tick(float deltaTime) {
            tick++;

            // 1) Apply inputs only if actor is alive
            foreach (var pair in _latestByActor)
            {
                if (actors.TryGetValue(pair.Key, out var a) && a.type == ActorType.Player && a.alive)
                {
                    a.ApplyInput(
                      pair.Value,
                      MovementConstants.PlayerSpeed,
                      MovementConstants.PlayerAcceleration,
                      MovementConstants.PlayerDeceleration,
                      MovementConstants.DashImpulse,
                      MovementConstants.DashCooldown,
                      deltaTime
                    );
                    
                    // Handle shooting for players
                    if (a.team == Team.Player &&
                        (pair.Value.buttons & MovementConstants.AttackButtonBit) != 0 &&
                        a.gunCooldownTicks == 0) {
                        var aimDir = new Vector2(pair.Value.aimX, pair.Value.aimY);
                        if (aimDir.x != 0 || aimDir.y != 0) {
                            ProcessShooting(a, aimDir);
                        }
                    }
                }
            }

            // 2) Enemy AI + contact damage
            foreach (var a in actors.Values) {
                a.Tick(deltaTime);
                if (a.type == ActorType.Enemy && a.alive) EnemyAttackPlayers(ref a, deltaTime);
            }

            // 3) Lives ticking
            int alivePlayers = CountAlivePlayers();
            life.Tick(deltaTime, alivePlayers);

            // 4) Respawns
            var rs = life.ConsumeRespawns();
            foreach (var id in rs) RespawnPlayer(id);

            // 5) cull dead enemies if hp<=0
            var toRemove = new List<int>();
            foreach (var a in actors.Values) {
                if (a.type == ActorType.Enemy && a.hp <= 0) {
                    toRemove.Add(a.id);
                }
            }
            foreach (var id in toRemove) {
                RemoveActor(id);
            }

            night.Tick(deltaTime);
            director.Tick();

            // Clamp all actors to playfield bounds
            foreach (var a in actors.Values) {
                a.pos = ClampToPlayfield(a.pos);
            }

            // TODO: spawn logic by ring & night; send snapshot deltas
            // For single-player we can skip serialization for now
        }

        public void ApplyForActor(int actorId, InputCommand cmd)
        {
            _latestByActor[actorId] = cmd;
        }

        public WorldSnapshot CreateSnapshot() {
            var actorStates = new ActorState[actors.Count];
            int i = 0;
            foreach (var actor in actors.Values) {
                actorStates[i] = actor.ToActorState();
                i++;
            }
            return new WorldSnapshot(tick, actorStates);
        }
        
        public int GetCurrentTick() => tick;

        public int SpawnPlayerAt(Vector2 pos)
        {
            var id = nextId++;
            actors[id] = new Actor(id, pos, ActorType.Player);
            return id;
        }

        public bool TryGetActorState(int id, out ActorState state)
        {
            if (actors.TryGetValue(id, out var a))
            {
                state = a.ToActorState();
                return true;
            }
            state = default;
            return false;
        }
        
        public int SpawnEnemyAt(Vector2 pos)
        {
            var id = nextId++;
            actors[id] = new Actor(id, pos, ActorType.Enemy);
            return id;
        }
        
        public void SetActorHp(int actorId, short hp)
        {
            if (actors.TryGetValue(actorId, out var actor))
            {
                actor.hp = hp;
            }
        }
        
        public void EnqueueEvent(DomainEvent e)
        {
            eventQueue.Enqueue(e);
        }
        
        public bool TryDequeueEvent(out DomainEvent e)
        {
            return eventQueue.TryDequeue(out e);
        }
        
        // Expose a safe consumer for shot events
        public IReadOnlyList<ShotEvent> ConsumeShotEvents()
        {
            var list = _shotEvents.ToArray();
            _shotEvents.Clear();
            return list;
        }
        
        private void ProcessShooting(Actor shooter, Vector2 aimDir) {
            // Normalize aim direction
            var normalizedAim = ServerSimHelpers.Normalize(aimDir);
            
            // Compute ray start and end
            var rayStart = shooter.pos;
            var rayEnd = shooter.pos + normalizedAim * CombatConstants.PistolRange;
            
            Actor firstHitEnemy = null;
            float closestImpact = float.MaxValue;
            bool hit = false;
            Vector2 shotEnd = rayEnd;

            // Test against all enemies using robust hitscan
            foreach (var actor in actors.Values) {
                if (actor.team == Team.Enemy) {
                    if (Hitscan.SegmentCircle(rayStart, rayEnd, actor.pos, actor.radius, out float linePointClosestToCenter)) {
                        if (linePointClosestToCenter < closestImpact) {
                            closestImpact = linePointClosestToCenter;
                            firstHitEnemy = actor;
                            shotEnd = rayStart + normalizedAim * linePointClosestToCenter * CombatConstants.PistolRange;
                        }
                    }
                }
            }
            
            if (firstHitEnemy != null) {
                hit = true;
                // Apply damage
                firstHitEnemy.hp -= (short)CombatConstants.PistolDamage;
                
                // Apply flinch timer
                firstHitEnemy.flinchTimer = 0.1f; // 0.1s flinch
                
                // Emit hit event
                EnqueueEvent(DomainEvent.HitLanded(shooter.id, firstHitEnemy.id, CombatConstants.PistolDamage));
                
                // Check for death
                if (firstHitEnemy.hp <= 0) {
                    EnqueueEvent(DomainEvent.ActorDied(firstHitEnemy.id));
                    RemoveActor(firstHitEnemy.id);
                }
            }
            
            // Enqueue shot event for VFX
            _shotEvents.Add(new ShotEvent(rayStart, shotEnd, hit));
            
            // Set cooldown
            shooter.gunCooldownTicks = CombatConstants.PistolCooldownTicks;
        }
        
        private void UpdateEnemyAI(Actor enemy, float deltaTime) {
            // Handle flinch timer
            if (enemy.flinchTimer > 0) {
                enemy.flinchTimer -= deltaTime;
                return; // Don't move while flinching
            }
            
            // Find nearest player
            Actor nearestPlayer = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var actor in actors.Values) {
                if (actor.team == Team.Player) {
                    var distance = ServerSimHelpers.Distance(enemy.pos, actor.pos);
                    if (distance < nearestDistance && distance <= CombatConstants.EnemySenseRange) {
                        nearestDistance = distance;
                        nearestPlayer = actor;
                    }
                }
            }
            
            if (nearestPlayer != null) {
                // Move toward player
                var direction = ServerSimHelpers.Normalize(nearestPlayer.pos - enemy.pos);
                var targetVel = direction * enemy.moveSpeed;
                
                float t = ServerSimHelpers.Clamp01(MovementConstants.PlayerAcceleration * deltaTime);
                enemy.vel = ServerSimHelpers.Lerp(enemy.vel, targetVel, t);
            } else {
                // Slow to stop
                float t = ServerSimHelpers.Clamp01(MovementConstants.PlayerDeceleration * deltaTime);
                enemy.vel = ServerSimHelpers.Lerp(enemy.vel, Vector2.zero, t);
            }
        }

        public Vector2 ClampToPlayfield(Vector2 p) {
            return new Vector2(
                ServerSimHelpers.Clamp(p.x, -playfieldHalfExtents.x, playfieldHalfExtents.x),
                ServerSimHelpers.Clamp(p.y, -playfieldHalfExtents.y, playfieldHalfExtents.y)
            );
        }

        public void SpawnEnemiesAtEdges(int count, float inset) {
            for (int i = 0; i < count; i++) {
                var spawnPos = GetRandomEdgePosition(inset);
                SpawnEnemyAt(spawnPos);
            }
        }

        private Vector2 GetRandomEdgePosition(float inset) {
            var edge = rng.Range(0, 4); // 0=top, 1=right, 2=bottom, 3=left
            var t = rng.Next01(); // 0 to 1 along the edge
            
            switch (edge) {
                case 0: // top edge
                    return new Vector2(
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.x + inset, playfieldHalfExtents.x - inset, t),
                        playfieldHalfExtents.y - inset
                    );
                case 1: // right edge
                    return new Vector2(
                        playfieldHalfExtents.x - inset,
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.y + inset, playfieldHalfExtents.y - inset, t)
                    );
                case 2: // bottom edge
                    return new Vector2(
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.x + inset, playfieldHalfExtents.x - inset, t),
                        -playfieldHalfExtents.y + inset
                    );
                case 3: // left edge
                    return new Vector2(
                        -playfieldHalfExtents.x + inset,
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.y + inset, playfieldHalfExtents.y - inset, t)
                    );
                default:
                    return Vector2.zero;
            }
        }

        // PUBLIC: app/bridge access (keeps one source of truth)
        public Vector2 GetRandomEdgePositionForBridge(float inset) => GetRandomEdgePosition(inset);

        public float GetReviveSecondsRemaining() => life.ReviveSecondsRemaining;
        
        private int CountAlivePlayers() { 
            int c=0; 
            foreach (var a in actors.Values) 
                if (a.type==ActorType.Player && a.alive) c++; 
            return c; 
        }

        private void EnemyAttackPlayers(ref Actor enemy, float dt) {
            enemy.attackCd -= dt;
            if (enemy.attackCd > 0f) return;
            
            const float attackRange = 0.75f;
            const short damage = 10;
            
            // find nearest alive player within range
            int targetId = -1; 
            float bestDistSq = 999999f;
            foreach (var kv in actors) {
                var p = kv.Value;
                if (p.type != ActorType.Player || !p.alive) continue;
                
                var dx = p.pos.x - enemy.pos.x; 
                var dy = p.pos.y - enemy.pos.y;
                var dsq = dx*dx + dy*dy;
                
                if (dsq < bestDistSq) { 
                    bestDistSq = dsq; 
                    targetId = kv.Key; 
                }
            }
            
            if (targetId >= 0 && bestDistSq <= attackRange*attackRange) {
                var player = actors[targetId];
                if (player.alive) {
                    // simple normal damage (no armor for players yet)
                    player.hp = (short)System.Math.Max(0, player.hp - damage);
                    if (player.hp <= 0) {
                        player.alive = false;
                        life.MarkDead(player.id);
                        player.vel = Vector2.zero;
                    }
                    actors[targetId] = player;
                    enemy.attackCd = 0.8f;
                }
            }
        }

        private void RespawnPlayer(int actorId) {
            if (!actors.TryGetValue(actorId, out var p)) return;
            
            p.alive = true;
            p.hp = 80; // respawn health
            p.pos = SafeRespawnPosition();
            p.vel = Vector2.zero;
            // clear dash cd etc. if needed
            actors[actorId] = p;
        }

        private Vector2 SafeRespawnPosition() {
            // reuse outskirts or near edge helper if present; otherwise pick (-10, 6)
            return new Vector2(-10f, 6f);
        }
        
        // Minimal inner Actor for the server
        private enum ActorType { Player = 0, Enemy = 1 }
        private sealed class Actor {
            public int id;
            public Vector2 pos;
            public Vector2 vel;
            public short hp = 100;
            public ActorType type;
            public Team team;
            public float radius;
            public float moveSpeed;
            public float flinchTimer;
            public bool alive = true;
            public float attackCd; // for enemies
            
            // Dash state
            private float dashCooldownRemaining;
            
            // Combat state
            public int gunCooldownTicks;
            
            public Actor(int id, Vector2 pos, ActorType type) {
                this.id = id;
                this.pos = pos;
                this.type = type;
                this.team = type == ActorType.Player ? Team.Player : Team.Enemy;
                this.radius = type == ActorType.Player ? CombatConstants.PlayerRadius : CombatConstants.EnemyRadius;
                this.moveSpeed = type == ActorType.Player ? MovementConstants.PlayerSpeed : MovementConstants.PlayerSpeed * 0.8f; // Damned speed
                this.hp = type == ActorType.Player ? (short)100 : (short)60; // Damned HP
            }
            
            public void Tick(float deltaTime) {
                pos += vel * deltaTime;
                
                // Update dash cooldown
                if (dashCooldownRemaining > 0) {
                    dashCooldownRemaining -= deltaTime;
                }
                
                // Update gun cooldown
                if (gunCooldownTicks > 0) {
                    gunCooldownTicks--;
                }
            }
            
            public void ApplyInput(InputCommand command, float speed, float acceleration, float deceleration, 
                float dashImpulse, float dashCooldown, float deltaTime) {
                
                // Handle dash
                if ((command.buttons & MovementConstants.DashButtonBit) != 0 && dashCooldownRemaining <= 0) {
                    var dashDir = new Vector2(command.moveX, command.moveY);
                    if (dashDir.x != 0 || dashDir.y != 0) {
                        var normalized = Normalize(dashDir);
                        vel = normalized * dashImpulse;
                        dashCooldownRemaining = dashCooldown;
                        return;
                    }
                }
                
                // Handle shooting
                if ((command.buttons & MovementConstants.AttackButtonBit) != 0 && gunCooldownTicks == 0) {
                    var aimDir = new Vector2(command.aimX, command.aimY);
                    if (aimDir.x != 0 || aimDir.y != 0) {
                        TryShoot(aimDir);
                    }
                }
                
                // Normal movement
                var targetVel = new Vector2(command.moveX, command.moveY);
                if (targetVel.x != 0 || targetVel.y != 0) {
                    targetVel = Normalize(targetVel) * speed;
                    float t = Clamp01(acceleration * deltaTime);
                    vel = Lerp(vel, targetVel, t);
                } else
                {
                    float t = Clamp01(deceleration * deltaTime);
                    vel = Lerp(vel, Vector2.zero, t);
                }
            }

            public ActorState ToActorState() {
                return new ActorState(id, pos.x, pos.y, vel.x, vel.y, hp, (byte)type, team, radius, alive);
            }
            
            private void TryShoot(Vector2 aimDir) {
                // This will be handled by the ServerSim
            }
            
            private static Vector2 Normalize(Vector2 v) {
                return ServerSimHelpers.Normalize(v);
            }
            
            private static Vector2 Lerp(Vector2 a, Vector2 b, float t) {
                return ServerSimHelpers.Lerp(a, b, t);
            }
            private static float Clamp01(float x)
            {
                return ServerSimHelpers.Clamp01(x);
            }
        }
    }

    // Simple Vector2 struct for Domain (no UnityEngine dependency)
    public struct Vector2 {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.x * b, a.y * b);
        public static Vector2 operator *(float a, Vector2 b) => new Vector2(a * b.x, a * b.y);
        public static Vector2 zero => new Vector2(0, 0);
    }
    
    // Helper methods for ServerSim
    public static class ServerSimHelpers {
        public static Vector2 Normalize(Vector2 v) {
            var length = (float)System.Math.Sqrt(v.x * v.x + v.y * v.y);
            if (length > 0) {
                return new Vector2(v.x / length, v.y / length);
            }
            return Vector2.zero;
        }
        
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) {
            return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float Clamp01(float x) {
            if (x < 0f) return 0f;
            if (x > 1f) return 1f;
            return x;
        }
        
        public static float Distance(Vector2 a, Vector2 b) {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }
        
        public static float Clamp(float value, float min, float max) {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}