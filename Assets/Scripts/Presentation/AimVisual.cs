using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Hellscape.Presentation
{
    public sealed class AimVisual : NetworkBehaviour
    {
        [SerializeField] Transform gun;   // assign child "Gun"
        [SerializeField] float minAimLen = 0.001f;

        Vector2 lookStick; // optional cache from NetPlayer via events if you prefer

        void Reset()
        {
            if (!gun && transform.childCount > 0) gun = transform.GetChild(0);
        }

        void Update()
        {
            if (!IsOwner || !gun) return;

            Vector2 aimDir = ComputeAimDir();
            if (aimDir.sqrMagnitude < minAimLen) return;

            float ang = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            gun.rotation = Quaternion.Euler(0, 0, ang);

            // Optional flip (if your gun sprite points right by default)
            var sr = gun.GetComponent<SpriteRenderer>();
            if (sr) sr.flipY = aimDir.x < 0f;
        }

        static Vector2 ReadPointerWorld(Vector3 actorPos)
        {
            if (Mouse.current != null)
            {
                var mp = Mouse.current.position.ReadValue();
                var cam = Camera.main;
                if (cam)
                {
                    var wp = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 0f));
                    return (Vector2)wp - (Vector2)actorPos;
                }
            }
#if UNITY_ANDROID || UNITY_IOS
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed)
            {
                var tp = ts.primaryTouch.position.ReadValue();
                var cam = Camera.main;
                if (cam)
                {
                    var wp = cam.ScreenToWorldPoint(new Vector3(tp.x, tp.y, 0f));
                    return (Vector2)wp - (Vector2)actorPos;
                }
            }
#endif
            return Vector2.zero;
        }

        Vector2 ComputeAimDir()
        {
            // Prefer pointer/touch position if present
            var dir = ReadPointerWorld(transform.position);
            if (dir.sqrMagnitude > 0.0001f) return dir.normalized;

            // Fallback to right stick (if you also pipe it in here later)
            var gp = Gamepad.current;
            if (gp != null)
            {
                var ls = gp.rightStick.ReadValue();
                if (ls.sqrMagnitude > 0.16f) return ls.normalized;
            }
            return Vector2.right;
        }
    }
}
