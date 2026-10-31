using System;
using System.Collections;
using UnityEngine;

namespace Digit7s.Gameplay
{
    /// <summary>
    /// Interactive ladder hidden under a destructible block.
    /// Has a lock (Icon_Lock) in front. Only when the player has the Gold Key
    /// will the lock break/vanish, allowing the player to enter the ladder and advance.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LevelLadder : MonoBehaviour
    {
        public static LevelLadder Instance { get; private set; }
        public static event Action OnLadderUnlocked;

        [Header("Animation Settings")]
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseScale = 0.06f;

        [Header("Lock Settings")]
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private bool isLocked = true;

        private Vector3 baseScale;
        private bool isTransitioning = false;
        private bool isInteractable = true;
        private SpriteRenderer spriteRenderer;
        private Collider2D ladderCollider;

        private GameObject lockObject;
        private SpriteRenderer lockRenderer;
        private Coroutine lockShakeRoutine;
        private Vector3 initialLockLocalPos = new Vector3(0f, 0f, -0.1f);

        public bool IsTransitioning => isTransitioning;
        public bool IsInteractable => isInteractable && !isTransitioning;
        public bool IsLocked => isLocked;

        private void Awake()
        {
            Instance = this;
            baseScale = transform.localScale;
            spriteRenderer = GetComponent<SpriteRenderer>();
            ladderCollider = GetComponent<Collider2D>();
            EnsureLockSprite();
            EnsureLockObject();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = new Vector3(2.2f, 2.2f, 1f);
            }
            EnsureLockObject();
            StartCoroutine(PopInRoutine());
        }

        public void Initialize(Sprite lockSp)
        {
            if (lockSp != null)
            {
                lockSprite = lockSp;
            }
            EnsureLockSprite();
            EnsureLockObject();
        }

        public void EnsureLockSprite()
        {
            if (lockSprite == null)
            {
                lockSprite = Resources.Load<Sprite>("Icons/Icon_Lock");
            }
            if (lockSprite == null)
            {
                lockSprite = Resources.Load<Sprite>("Icon_Lock");
            }
            if (lockSprite == null)
            {
                lockSprite = Resources.Load<Sprite>("Icons/Icon_Lock_Gold");
            }
#if UNITY_EDITOR
            if (lockSprite == null)
            {
                lockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Lock.png");
            }
            if (lockSprite == null)
            {
                lockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Lock.png");
            }
#endif
        }

        public void EnsureLockObject()
        {
            if (!isLocked)
            {
                if (lockObject != null)
                {
                    Destroy(lockObject);
                    lockObject = null;
                }
                return;
            }

            if (lockObject == null)
            {
                lockObject = new GameObject("Ladder_Lock");
                lockObject.transform.SetParent(transform, false);
                lockObject.transform.localPosition = initialLockLocalPos;
                lockObject.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

                lockRenderer = lockObject.AddComponent<SpriteRenderer>();
                EnsureLockSprite();
                lockRenderer.sprite = lockSprite;
                lockRenderer.sortingLayerName = "Default";
                lockRenderer.sortingOrder = 5; // Ladder is 1, so lock sits cleanly in front
            }
            else
            {
                if (lockRenderer == null) lockRenderer = lockObject.GetComponent<SpriteRenderer>();
                if (lockRenderer != null && lockRenderer.sprite == null)
                {
                    EnsureLockSprite();
                    lockRenderer.sprite = lockSprite;
                }
                if (lockRenderer != null)
                {
                    lockRenderer.sortingLayerName = "Default";
                    lockRenderer.sortingOrder = 5;
                }
            }
        }

        /// <summary>
        /// Called when revealed after breaking a block. Disables collider & click interaction
        /// for the duration of the explosion animation so it cannot be accidentally clicked.
        /// </summary>
        public void OnRevealedFromBlock(float delay = 0.65f)
        {
            isInteractable = false;
            if (ladderCollider == null) ladderCollider = GetComponent<Collider2D>();
            if (ladderCollider != null) ladderCollider.enabled = false;

            if (delay > 0f && Application.isPlaying)
            {
                StartCoroutine(EnableInteractionAfterExplosionRoutine(delay));
            }
            else if (delay <= 0f)
            {
                isInteractable = true;
                if (ladderCollider != null) ladderCollider.enabled = true;
            }
        }

        private IEnumerator EnableInteractionAfterExplosionRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            isInteractable = true;
            if (ladderCollider != null)
            {
                ladderCollider.enabled = true;
            }
        }

        private void Update()
        {
            if (isTransitioning) return;

            // Subtle pulsing breathing animation so ladder is immediately noticeable
            float scaleMod = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            transform.localScale = baseScale * scaleMod;
        }

        private IEnumerator PopInRoutine()
        {
            float elapsed = 0f;
            float duration = 0.12f;
            transform.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(Vector3.zero, baseScale, t);
                yield return null;
            }
            transform.localScale = baseScale;
        }

        public void OnLadderClicked()
        {
            if (!isInteractable || isTransitioning) return;

            if (isLocked)
            {
                // Locked without key
                Debug.Log("[LevelLadder] Ladder is locked! You need a Key to enter.");
                PlayLockedShake();
                Digit7s.UI.UISoundManager.PlayLadderLocked();
                CatPowerUpController.TriggerNotification("Ladder is Locked! Find the Gold Key!", new Color(1f, 0.85f, 0.2f));
                return;
            }

            isTransitioning = true;
            Debug.Log($"<color=green>[LevelLadder] Ladder clicked! Advancing to Room Level {EnemySpawner.CurrentRoomLevel + 1}...</color>");
            StartCoroutine(TransitionRoutine());
        }

        private void PlayLockedShake()
        {
            if (lockObject == null) return;
            if (lockShakeRoutine != null) StopCoroutine(lockShakeRoutine);
            lockShakeRoutine = StartCoroutine(LockShakeRoutine());
        }

        private IEnumerator LockShakeRoutine()
        {
            if (lockObject == null) yield break;

            float elapsed = 0f;
            float duration = 0.35f;
            Color origColor = Color.white;
            if (lockRenderer != null) lockRenderer.color = new Color(1f, 0.35f, 0.35f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * 45f) * 0.12f;
                if (lockObject != null)
                {
                    lockObject.transform.localPosition = initialLockLocalPos + new Vector3(xOffset, 0f, 0f);
                }
                yield return null;
            }

            if (lockObject != null)
            {
                lockObject.transform.localPosition = initialLockLocalPos;
                if (lockRenderer != null) lockRenderer.color = origColor;
            }
            lockShakeRoutine = null;
        }

        /// <summary>
        /// Called when the flying Gold Key reaches the ladder.
        /// Shatters and removes the lock icon, leaving the ladder open for the player to click and advance.
        /// </summary>
        public void UnlockByIncomingKey()
        {
            if (!isLocked) return;
            isLocked = false;
            PlayerInstance.SetHasKey(true);
            Digit7s.UI.UISoundManager.PlayKeyUnlock();

            CatPowerUpController.TriggerNotification("Ladder Unlocked! Click ladder to advance.", new Color(0.2f, 1f, 0.5f));
            Debug.Log("<color=green>[LevelLadder] Key unlocked ladder! Click ladder to advance to next room level.</color>");

            if (lockShakeRoutine != null)
            {
                StopCoroutine(lockShakeRoutine);
                lockShakeRoutine = null;
            }

            if (lockObject != null)
            {
                StartCoroutine(ShatterLockRoutine());
            }

            StartCoroutine(UnlockPunchRoutine());
            OnLadderUnlocked?.Invoke();
        }

        private IEnumerator UnlockPunchRoutine()
        {
            Vector3 punchScale = baseScale * 1.25f;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = Mathf.Sin(t * Mathf.PI);
                transform.localScale = Vector3.Lerp(baseScale, punchScale, s);
                yield return null;
            }
            transform.localScale = baseScale;
        }

        private IEnumerator ShatterLockRoutine()
        {
            if (lockObject == null) yield break;

            Vector3 startScale = lockObject.transform.localScale;
            Vector3 punchScale = startScale * 1.6f;
            Color startColor = lockRenderer != null ? lockRenderer.color : Color.white;
            float elapsed = 0f;
            float duration = 0.38f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (lockObject != null)
                {
                    lockObject.transform.localScale = Vector3.Lerp(startScale, punchScale, t);
                    lockObject.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 28f);
                    if (lockRenderer != null)
                    {
                        lockRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                    }
                }
                yield return null;
            }

            if (lockObject != null)
            {
                Destroy(lockObject);
                lockObject = null;
            }
        }

        private IEnumerator TransitionRoutine()
        {
            // Quick punch-scale effect on click before loading
            Vector3 startScale = transform.localScale;
            Vector3 punchScale = startScale * 1.25f;
            float elapsed = 0f;
            float duration = 0.12f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, punchScale, t);
                yield return null;
            }

            // Auto-collect any remaining revealed uncollected items
            var uncollectedItems = UnityEngine.Object.FindObjectsByType<ItemPickupProp>(FindObjectsSortMode.None);
            foreach (var item in uncollectedItems)
            {
                if (item != null && item.gameObject.activeInHierarchy && !item.IsCollected)
                {
                    item.Collect();
                }
            }

            // Convert any remaining Shield charges to Coins before advancing to next room
            // ("Cat အားလုံ; Shield အခန်းအဆုံးမှာ ladder နောက်တခန်းရောက်သွားရင် Shield တခုကိုယ် = Coin 1 ပြန်ပြီးလဲပေးမယ် (coin exchange animation and sfx) ထည့်ပေးပါ")
            var powerUp = CatPowerUpController.Instance;
            if (powerUp != null && powerUp.ShieldCharges > 0)
            {
                yield return StartCoroutine(powerUp.ConvertShieldsToCoinsRoutine());
            }

            EnemySpawner.AdvanceToNextLevel();
        }

        private void OnMouseUpAsButton()
        {
            if (!isInteractable) return;
            OnLadderClicked();
        }
    }
}
