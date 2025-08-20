using System.Collections.Generic;
using Hellscape.Domain;

namespace Hellscape.Domain {
    public sealed class ServerSim {
        private readonly DeterministicRng rng;
        private int tick;

        // World
        private CityGrid grid;
        private NightSystem night;
        private Director director;

        // Actors (toy implementation)
        private readonly Dictionary<int, Actor> actors = new();
        private int nextId = 1;
        private Actor playerActor;

        public ServerSim(int seed) {
            this.rng = new DeterministicRng(seed);
        }

        public void Start() {
            grid = new CityGrid(64, 64, 32); // width, height, centerRadius
            night = new NightSystem();
            director = new Director();
            SpawnPlayer(new Vector2(grid.CenterX - 10, grid.CenterY + 6));
            // Spawn a few enemies to prove life
            for (int i = 0; i < 8; i++) {
                SpawnEnemy(RandomEdgePos());
            }
        }

        public void Tick(float deltaTime) {
            tick++;
            night.Tick(deltaTime);
            director.Tick();

            // Advance simple actors (server-authoritative)
            foreach (var a in actors.Values) a.Tick(deltaTime);

            // TODO: spawn logic by ring & night; send snapshot deltas
            // For single-player we can skip serialization for now
        }

        public void Apply(InputCommand command) {
            if (playerActor != null && command.tick == tick) {
                playerActor.ApplyInput(command, MovementConstants.PlayerSpeed, MovementConstants.PlayerAcceleration, 
                    MovementConstants.PlayerDeceleration, MovementConstants.DashImpulse, MovementConstants.DashCooldown);
            }
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

        private void SpawnPlayer(Vector2 pos) {
            var id = nextId++;
            playerActor = new Actor(id, pos, ActorType.Player);
            actors[id] = playerActor;
        }

        private void SpawnEnemy(Vector2 pos) {
            var id = nextId++;
            actors[id] = new Actor(id, pos, ActorType.Enemy);
        }

        private Vector2 RandomEdgePos() {
            // Spawn on outskirts
            var x = rng.Range(2, grid.Width - 2);
            var y = rng.Range(2, grid.Height - 2);
            return new Vector2(x, y);
        }

        // Minimal inner Actor for the server
        private enum ActorType { Player = 0, Enemy = 1 }
        private sealed class Actor {
            public int id;
            public Vector2 pos;
            public Vector2 vel;
            public short hp = 100;
            public ActorType type;
            
            // Dash state
            private float dashCooldownRemaining;
            
            public Actor(int id, Vector2 pos, ActorType type) {
                this.id = id;
                this.pos = pos;
                this.type = type;
            }
            
            public void Tick(float deltaTime) {
                pos += vel * deltaTime; /* TODO: AI/physics */
                
                // Update dash cooldown
                if (dashCooldownRemaining > 0) {
                    dashCooldownRemaining -= deltaTime;
                }
            }
            
            public void ApplyInput(InputCommand command, float speed, float acceleration, float deceleration, 
                float dashImpulse, float dashCooldown) {
                
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
                
                // Normal movement
                var targetVel = new Vector2(command.moveX, command.moveY);
                if (targetVel.x != 0 || targetVel.y != 0) {
                    targetVel = Normalize(targetVel) * speed;
                    vel = Lerp(vel, targetVel, acceleration * 0.02f); // Using fixed delta
                } else {
                    vel = Lerp(vel, Vector2.zero, deceleration * 0.02f);
                }
            }
            
            public ActorState ToActorState() {
                return new ActorState(id, pos.x, pos.y, vel.x, vel.y, hp, (byte)type);
            }
            
            private static Vector2 Normalize(Vector2 v) {
                var length = (float)System.Math.Sqrt(v.x * v.x + v.y * v.y);
                if (length > 0) {
                    return new Vector2(v.x / length, v.y / length);
                }
                return Vector2.zero;
            }
            
            private static Vector2 Lerp(Vector2 a, Vector2 b, float t) {
                return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
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
}