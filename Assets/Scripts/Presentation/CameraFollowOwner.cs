using UnityEngine;
using Unity.Netcode;

namespace Hellscape.Presentation
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class CameraFollowOwner : NetworkBehaviour
    {
        [SerializeField] float smoothTime = 0.12f;
        [SerializeField] Vector3 offset = new Vector3(0, 0, -10);

        Vector3 vel;

        void LateUpdate()
        {
            if (!IsOwner) return;
            var cam = Camera.main;
            if (!cam) return;

            var target = transform.position + offset;
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, target, ref vel, smoothTime);
        }
    }
}
