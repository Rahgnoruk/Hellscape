using System;
using System.Collections.Generic;
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

        // Actors
        private readonly Dictionary<int, Actor> playerActors = new();
        private readonly Dictionary<int, Actor> enemyActors = new();
        public bool RemovePlayerActor(int actorId) => playerActors.Remove(actorId);
        public bool RemoveEnemyActor(int actorId) => enemyActors.Remove(actorId);

        private int nextId = 1;

        private readonly Dictionary<int, InputCommand> _latestByActor = new();
        
        // Event system
        private readonly Queue<DomainEvent> eventQueue = new();
        
        // Shot events for VFX
        private readonly List<ShotEvent> _shotEvents = new();
        
        // Lives system
        private LifeSystem lifeSystem;
        
        // Team score
        private int teamScore;

        public ServerSim(int seed) {
            this.rng = new DeterministicRng(seed);
        }

        public void Start() {
            grid = new CityGrid(64, 64, 32); // width, height, centerRadius
            night = new NightSystem();
            director = new Director();
            lifeSystem = new LifeSystem(10f);
        }

        public void Tick(float deltaTime) {
            tick++;

            // 1) Apply inputs only if actor is alive
            foreach (var pair in _latestByActor)
            {
                if (playerActors.TryGetValue(pair.Key, out var playerActor) &&
                    playerActor.alive)
                {
                    playerActor.ApplyInput(
                      pair.Value,
                      MovementConstants.PlayerSpeed,
                      MovementConstants.PlayerAcceleration,
                      MovementConstants.PlayerDeceleration,
                      MovementConstants.DashImpulse,
                      MovementConstants.DashCooldown,
                      deltaTime
                    );
                    
                    // Handle shooting for players
                    if (playerActor.team == Team.Player &&
                        (pair.Value.buttons & MovementConstants.AttackButtonBit) != 0 &&
                        playerActor.gunCooldownTicks == 0) {
                        var aimDir = new Vector2(pair.Value.aimX, pair.Value.aimY);
                        if (aimDir.x != 0 || aimDir.y != 0) {
                            ProcessShooting(playerActor, aimDir, deltaTime);
                        }
                    }
                }
            }
            var deadEnemies = new List<int>();
            // 2) Update all enemies
            foreach (var enemyActor in enemyActors.Values) {
                // 2.1) Tick actors
                enemyActor.Tick(deltaTime);
                // 2.2) Clamp all actors to playfield bounds
                enemyActor.pos = ClampToPlayfield(enemyActor.pos);
                // 2.3) Enemy AI + contact damage
                if (enemyActor.alive)
                {
                    UpdateEnemyAI(enemyActor, deltaTime);
                }
                // 2.4) Mark enemies for removal if hp<=0
                if (enemyActor.hp <= 0)
                {
                    deadEnemies.Add(enemyActor.id);
                }
            }
            // 3) Remove dead enemies and increment score
            foreach (var id in deadEnemies)
            {
                EnqueueEvent(DomainEvent.ActorDied(id));
                RemoveEnemyActor(id);
                teamScore += 1; // Increment score for each enemy killed
            }
            // 4) Update all player actors
            foreach (var playerActor in playerActors.Values)
            {
                // 4.1) Tick actors
                playerActor.Tick(deltaTime);
                // 4.2) Clamp all actors to playfield bounds
                playerActor.pos = ClampToPlayfield(playerActor.pos);
            }
            // 5) Tick respawn system
            int alivePlayers = CountAlivePlayers();
            lifeSystem.Tick(deltaTime, alivePlayers);

            // 6) Respawn Players
            var toRespawn = lifeSystem.ConsumeRespawns();
            foreach (var id in toRespawn)
            {
                RespawnPlayer(id);
            }


            night.Tick(deltaTime);
            director.Tick();

            // TODO: spawn logic by ring & night; send snapshot deltas
            // For single-player we can skip serialization for now
        }

        public void ApplyForActor(int actorId, InputCommand cmd)
        {
            _latestByActor[actorId] = cmd;
        }

        public WorldSnapshot CreateSnapshot() {
            var actorStates = new ActorState[enemyActors.Count + playerActors.Count];
            int i = 0;
            foreach (var actor in enemyActors.Values) {
                actorStates[i] = actor.ToActorState();
                i++;
            }
            foreach (var actor in playerActors.Values) {
                actorStates[i] = actor.ToActorState();
                i++;
            }
            return new WorldSnapshot(tick, actorStates);
        }
        
        public int GetCurrentTick() => tick;

        public int SpawnPlayerAt(Vector2 pos)
        {
            var id = nextId++;
            playerActors[id] = new Actor(id, pos, ActorType.Player);
            return id;
        }

        public bool TryGetActorState(int id, out ActorState state)
        {
            if (enemyActors.TryGetValue(id, out var actor))
            {
                state = actor.ToActorState();
                return true;
            }
            if (playerActors.TryGetValue(id, out actor))
            {
                state = actor.ToActorState();
                return true;
            }
            state = default;
            return false;
        }
        
        public int SpawnEnemyAt(Vector2 pos)
        {
            var id = nextId++;
            enemyActors[id] = new Actor(id, pos, ActorType.Enemy);
            return id;
        }
        
        public void SetActorHp(int actorId, short hp)
        {
            if (enemyActors.TryGetValue(actorId, out var actor))
            {
                actor.hp = hp;
                return;
            }
            if (playerActors.TryGetValue(actorId, out actor))
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
        
        private void ProcessShooting(Actor shooter, Vector2 aimDir, float deltaTime) {
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
            foreach (var enemyActor in enemyActors.Values) {
                if (Hitscan.SegmentCircle(rayStart, rayEnd, enemyActor.pos, enemyActor.radius, out float linePointClosestToCenter)) {
                    if (linePointClosestToCenter < closestImpact) {
                        closestImpact = linePointClosestToCenter;
                        firstHitEnemy = enemyActor;
                        shotEnd = rayStart + normalizedAim * linePointClosestToCenter * CombatConstants.PistolRange;
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
            }
            
            // Enqueue shot event for VFX
            _shotEvents.Add(new ShotEvent(rayStart, shotEnd, hit));
            
            // Set cooldown
            shooter.gunCooldownTicks = (int)Math.Round(CombatConstants.PistolCooldownSeconds / deltaTime);
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
            
            foreach (var playerActor in playerActors.Values) {
                var distance = ServerSimHelpers.Distance(enemy.pos, playerActor.pos);
                if (distance < nearestDistance && distance <= CombatConstants.EnemySenseRange) {
                    nearestDistance = distance;
                    nearestPlayer = playerActor;
                }
            }
            
            if (nearestPlayer != null) {
                // Move toward player
                var direction = ServerSimHelpers.Normalize(nearestPlayer.pos - enemy.pos);
                var targetVel = direction * enemy.moveSpeed;
                
                float t = ServerSimHelpers.Clamp01(MovementConstants.PlayerAcceleration * deltaTime);
                enemy.vel = ServerSimHelpers.Lerp(enemy.vel, targetVel, t);
                EnemyAttackPlayers(ref enemy, deltaTime);
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

        public float GetReviveSecondsRemaining() => lifeSystem.ReviveSecondsRemaining;
        
        public int GetTeamScore() => teamScore;
        
        public int GetDeadPlayerCount() {
            int dead = 0;
            foreach (var actor in playerActors.Values) {
                if (actor.type == ActorType.Player && !actor.alive) dead++;
            }
            return dead;
        }
        
        public bool AreThereAlivePlayers() {
            foreach (var actor in playerActors.Values) {
                if (actor.alive)
                {
                    return true;
                }
            }
            return false;
        }

        private int CountAlivePlayers() {
            int alivePlayers = 0;
            foreach (var playerActor in playerActors.Values)
            {
                if (playerActor.alive)
                {
                    alivePlayers++;
                }
            }
            return alivePlayers; 
        }

        private void EnemyAttackPlayers(ref Actor enemy, float deltaTime) {
            enemy.attackCd -= deltaTime;
            if (enemy.attackCd > 0f) return;
            
            const float attackRange = 0.75f;
            const short damage = 10;
            
            // find nearest alive player within range
            int targetId = -1;
            float bestDistSq = 999999f;
            foreach (var idToPlayer in playerActors) {
                var playerActor = idToPlayer.Value;
                if (!playerActor.alive) continue;
                
                var distanceX = playerActor.pos.x - enemy.pos.x; 
                var distanceY = playerActor.pos.y - enemy.pos.y;
                var distanceSq = distanceX*distanceX + distanceY*distanceY;
                
                if (distanceSq < bestDistSq) { 
                    bestDistSq = distanceSq; 
                    targetId = idToPlayer.Key; 
                }
            }
            
            if (targetId >= 0 && bestDistSq <= attackRange*attackRange) {
                var player = playerActors[targetId];
                if (player.alive) {
                    // simple normal damage (no armor for players yet)
                    player.hp = (short)Math.Max(0, player.hp - damage);
                    if (player.hp <= 0) {
                        player.alive = false;
                        lifeSystem.MarkDead(player.id);
                        player.vel = Vector2.zero;
                    }
                    playerActors[targetId] = player;
                    enemy.attackCd = 0.8f;
                }
            }
        }

        private void RespawnPlayer(int actorId) {
            if (!playerActors.TryGetValue(actorId, out var olayer)) return;
            
            olayer.alive = true;
            olayer.hp = 80; // respawn health
            olayer.pos = SafeRespawnPosition();
            olayer.vel = Vector2.zero;
            // clear dash cd etc. if needed
            playerActors[actorId] = olayer;
        }

        private Vector2 SafeRespawnPosition() {
            foreach(var actor in playerActors.Values) {
                if (actor.alive) {
                    return new Vector2(actor.pos.x + 2f, actor.pos.y + 2f);
                }
            }
            return new Vector2(0,0);
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