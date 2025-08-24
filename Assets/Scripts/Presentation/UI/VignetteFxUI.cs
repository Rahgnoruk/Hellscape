using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace Hellscape.Presentation.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class VignetteFxUI : MonoBehaviour
    {
        private UIDocument document;
        private VisualElement damageVignette;
        private Coroutine flashCoroutine;
        
        private void Awake()
        {
            document = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            SetupUI();
        }
        
        private void SetupUI()
        {
            var root = document.rootVisualElement;
            damageVignette = root.Q<VisualElement>("DamageVignette");
            
            if (damageVignette != null)
            {
                // Ensure vignette starts hidden
                damageVignette.style.opacity = 0f;
            }
        }
        
        public void Flash(float intensity = 1f, float hold = 0.06f, float fade = 0.25f)
        {
            if (damageVignette == null) return;
            
            // Stop any existing flash
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            
            flashCoroutine = StartCoroutine(FlashCoroutine(intensity, hold, fade));
        }
        
        private IEnumerator FlashCoroutine(float intensity, float hold, float fade)
        {
            if (damageVignette == null) yield break;
            
            // Set initial opacity
            float targetOpacity = Mathf.Clamp01(intensity);
            damageVignette.style.opacity = targetOpacity;
            
            // Hold at full intensity
            yield return new WaitForSeconds(hold);
            
            // Fade to zero
            float elapsed = 0f;
            float startOpacity = targetOpacity;
            
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fade;
                float currentOpacity = Mathf.Lerp(startOpacity, 0f, t);
                damageVignette.style.opacity = currentOpacity;
                yield return null;
            }
            
            // Ensure opacity is exactly 0
            damageVignette.style.opacity = 0f;
            flashCoroutine = null;
        }
        
        public void OnLocalPlayerDamaged(int amount)
        {
            // Calculate intensity based on damage amount
            float intensity = Mathf.Clamp01(amount / 100f); // Normalize to 0-1 range
            Flash(intensity);
        }
        
        private void OnDestroy()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
        }
    }
}
