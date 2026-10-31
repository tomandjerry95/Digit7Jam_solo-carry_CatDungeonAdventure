using System.Collections;
using UnityEngine;

namespace Digit7s.Gameplay
{
    /// <summary>
    /// Interactive breakable wooden prop (Barrel or Box) hidden underneath blocks.
    /// Can be clicked to break with score feedback.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WoodenBreakableProp : MonoBehaviour
    {
        public enum WoodenPropType
        {
            Barrel,
            Box
        }

        [Header("Prop Settings")]
        [SerializeField] private WoodenPropType propType;
        [SerializeField] private int scoreValue = 5;

        private bool isBroken = false;
        private bool isInteractable = true;
        private SpriteRenderer spriteRenderer;
        private Collider2D propCollider;

        public bool IsBroken => isBroken;
        public bool IsInteractable => isInteractable && !isBroken;
        public WoodenPropType PropType => propType;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            propCollider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// Called when revealed after breaking a block. Disables collider & click interaction
        /// for the duration of the explosion animation so it cannot be accidentally clicked.
        /// </summary>
        public void OnRevealedFromBlock(float delay = 0.65f)
        {
            isInteractable = false;
            if (propCollider == null) propCollider = GetComponent<Collider2D>();
            if (propCollider != null) propCollider.enabled = false;

            if (delay > 0f && Application.isPlaying)
            {
                StartCoroutine(EnableInteractionAfterExplosionRoutine(delay));
            }
            else if (delay <= 0f)
            {
                isInteractable = true;
                if (propCollider != null) propCollider.enabled = true;
            }
        }

        private IEnumerator EnableInteractionAfterExplosionRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            isInteractable = true;
            if (propCollider != null && !isBroken)
            {
                propCollider.enabled = true;
            }
        }

        public void Initialize(WoodenPropType type, Sprite sprite)
        {
            propType = type;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        public void BreakProp()
        {
            if (!isInteractable || isBroken) return;

            if (EnemyInstance.IsBlockLocked(transform.position))
            {
                Debug.Log($"[WoodenBreakableProp] Click ignored: {propType} is locked by an adjacent active enemy!");
                return;
            }

            isBroken = true;
            if (propCollider != null) propCollider.enabled = false;

            Debug.Log($"[WoodenBreakableProp] Broke {propType} at {transform.position}! (+{scoreValue} pts)");
            StartCoroutine(ShrinkAndDestroyRoutine());
        }

        private IEnumerator ShrinkAndDestroyRoutine()
        {
            Vector3 startScale = transform.localScale;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float elapsed = 0f;
            float duration = 0.08f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnMouseUpAsButton()
        {
            if (!isInteractable) return;
            BreakProp();
        }
    }
}
