namespace Hellscape.Domain
{
    // Tracks dead players and a shared survive-to-revive countdown.
    public sealed class LifeSystem
    {
        private readonly System.Collections.Generic.HashSet<int> dead = new();
        private float reviveCountdown = -1f; // <0 means idle
        private readonly float reviveDuration;

        // buffer for respawns after Tick
        private readonly System.Collections.Generic.List<int> toRespawn = new();

        public LifeSystem(float reviveDurationSeconds = 10f) { reviveDuration = reviveDurationSeconds; }

        public bool IsDead(int actorId) => dead.Contains(actorId);
        public void MarkDead(int actorId)
        {
            if (dead.Add(actorId))
                reviveCountdown = reviveDuration; // (re)start countdown when the first death happens or when a new one dies
        }

        public void MarkAlive(int actorId) { dead.Remove(actorId); if (dead.Count == 0) reviveCountdown = -1f; }

        public void Tick(float deltaTime, int alivePlayerCount)
        {
            toRespawn.Clear();
            if (alivePlayerCount <= 0) { reviveCountdown = -1f; return; } // team wipe; handled by ServerSim
            if (dead.Count == 0) { reviveCountdown = -1f; return; }
            if (reviveCountdown < 0f) reviveCountdown = reviveDuration;

            reviveCountdown -= deltaTime;
            if (reviveCountdown <= 0f)
            {
                // Everyone who is dead comes back now
                foreach (var id in dead) toRespawn.Add(id);
                dead.Clear();
                reviveCountdown = -1f;
            }
        }

        public System.Collections.Generic.IReadOnlyList<int> ConsumeRespawns()
        {
            return toRespawn.ToArray();
        }

        public float ReviveSecondsRemaining => reviveCountdown < 0f ? 0f : reviveCountdown;
        public int DeadCount => dead.Count;
    }
}
