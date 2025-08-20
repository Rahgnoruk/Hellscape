using UnityEngine;
using Hellscape.Domain;

namespace Hellscape.Platform {
    public class UnityClock : IClock {
        public float FixedDelta => Time.fixedDeltaTime;
    }
}
