using UnityEngine;
using Hellscape.Domain;
using Vector2 = UnityEngine.Vector2;

namespace Hellscape.Presentation.Vfx
{
    public class ShotVfxPlayer : MonoBehaviour, IVfxEventBroadcaster
    {
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject tracerPrefab;
        [SerializeField] private GameObject impactSparkPrefab;
        [SerializeField] private float tracerSpeed = 50f;
        [SerializeField] private float vfxLifetime = 0.5f;
        
        private static ShotVfxPlayer _instance;
        
        public static ShotVfxPlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ShotVfxPlayer>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                // Register as VFX event broadcaster
                VfxEventSystem.SetBroadcaster(this);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void PlayMuzzleFlash(Vector2 pos, float angle)
        {
            if (muzzleFlashPrefab == null) return;
            
            var flash = Instantiate(muzzleFlashPrefab, pos, Quaternion.Euler(0, 0, angle));
            Destroy(flash, vfxLifetime);
        }

        public void PlayTracer(Vector2 from, Vector2 to)
        {
            if (tracerPrefab == null) return;
            
            var direction = (to - from).normalized;
            var distance = Vector2.Distance(from, to);
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            var tracer = Instantiate(tracerPrefab, from, Quaternion.Euler(0, 0, angle));
            
            // Scale tracer to match distance
            var scale = tracer.transform.localScale;
            scale.x = distance;
            tracer.transform.localScale = scale;
            
            Destroy(tracer, vfxLifetime);
        }

        public void PlayImpact(Vector2 pos, float angle)
        {
            if (impactSparkPrefab == null) return;
            
            var spark = Instantiate(impactSparkPrefab, pos, Quaternion.Euler(0, 0, angle));
            Destroy(spark, vfxLifetime);
        }

        public void PlayShotSequence(Vector2 from, Vector2 to)
        {
            var direction = (to - from).normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            PlayMuzzleFlash(from, angle);
            PlayTracer(from, to);
            PlayImpact(to, angle);
        }

        // IVfxEventBroadcaster implementation
        public void BroadcastVfxEvent(VfxEvent vfxEvent)
        {
            switch (vfxEvent.type)
            {
                case VfxEvent.Type.Shot:
                    Vector2 from = new Vector2(vfxEvent.from.x, vfxEvent.from.y);
                    Vector2 to = new Vector2(vfxEvent.to.x, vfxEvent.to.y);
                    PlayShotSequence(from, to);
                    break;
            }
        }
    }
}
