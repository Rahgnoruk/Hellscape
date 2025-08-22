using NUnit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hellscape.Net
{
    public sealed class ShotVfx : MonoBehaviour
    {
        [SerializeField] GameObject tracerPrefab;   // thin line sprite or LineRenderer
        [SerializeField] GameObject muzzleFlashPrefab; // small sprite
        [SerializeField] float tracerLifetime = 0.06f; // seconds

        public List<GameObject> tracerInstances;

        public void PlayTracer(Vector2 start, Vector2 end)
        {
            if (tracerPrefab == null) return;
            GameObject tracerInstance = null;
            foreach(var instance in tracerInstances)
            {
                if (!instance.activeSelf)
                {
                    tracerInstance = instance;
                    break;
                }
            }
            if (tracerInstance == null)
            {
                tracerInstance = Instantiate(tracerPrefab);
                tracerInstances.Add(tracerInstance);
            }
            PlayTracer(start, end, tracerInstance);
        }
        private void PlayTracer(Vector2 start, Vector2 end, GameObject tracerInstance)
        {
            tracerInstance.SetActive(true);
            var dir = end - start;
            var len = dir.magnitude;
            if (len < 0.01f)
            {
                tracerInstance.SetActive(false);
                return;
            }

            tracerInstance.transform.right = dir.normalized;
            tracerInstance.transform.localScale = new Vector3(len, tracerInstance.transform.localScale.y, 1f);
            tracerInstance.transform.position = start + dir * 0.5f; // center it on the line
            StartCoroutine(DeactivateTracerInSeconds(tracerInstance, 0.06f)); // very brief
        }
        private IEnumerator DeactivateTracerInSeconds(GameObject tracerInstance, float duration)
        {
            yield return new WaitForSeconds(duration);
            tracerInstance.SetActive(false);
        }
        public void PlayMuzzle(Vector2 at)
        {
            if (muzzleFlashPrefab == null) return;
            var go = Instantiate(muzzleFlashPrefab, at, Quaternion.identity);
            Destroy(go, 0.05f);
        }
    }
}
