using UnityEngine;


namespace Hellscape.Presentation {
    public sealed class CameraRig2D : MonoBehaviour {
        public Transform follow;
        public float smooth = 10f;
        void LateUpdate(){
            if (!follow){
                return;
            } 
            transform.position = Vector3.Lerp(
                transform.position,
                new Vector3(follow.position.x, follow.position.y, transform.position.z),
                Time.deltaTime*smooth
            );
        }
    }
}