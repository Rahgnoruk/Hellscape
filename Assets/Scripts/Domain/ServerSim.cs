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

        private void SpawnPlayer(Vector2 pos) {
            var id = nextId++;
            actors[id] = new Actor(id, pos, ActorType.Player);
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
            
            public Actor(int id, Vector2 pos, ActorType type) {
                this.id = id;
                this.pos = pos;
                this.type = type;
            }
            
            public void Tick(float deltaTime) {
                pos += vel * deltaTime; /* TODO: AI/physics */
            }
        }
    }

    // Simple Vector2 struct for Domain (no UnityEngine dependency)
    public struct Vector2 {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.x * b, a.y * b);
    }
}