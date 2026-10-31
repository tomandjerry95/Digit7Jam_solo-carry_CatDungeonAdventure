using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Digit7s.Gameplay
{
    public enum ItemPickupType
    {
        Heart,
        Damage,
        Key,
        Shield,
        Coin
    }

    /// <summary>
    /// Interactive collectible item (Heart, Damage, Key, Shield, Coin) hidden beneath destructible blocks.
    /// Automatically flies to the target (Ladder for Key, Player Character for Heart/Damage/Shield/Coin) and collects when revealed.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ItemPickupProp : MonoBehaviour
    {
        [Header("Item Configuration")]
        [SerializeField] private ItemPickupType itemType = ItemPickupType.Heart;
        [SerializeField] private int itemValue = 1;

        private bool isCollected = false;
        private bool isInteractable = true;
        private SpriteRenderer spriteRenderer;
        private Collider2D itemCollider;
        private Vector3 initialLocalPos;
        private float floatOffset;

        public bool IsCollected => isCollected;
        public bool IsInteractable => isInteractable && !isCollected;
        public ItemPickupType ItemType => itemType;
        public int Value => itemValue;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            itemCollider = GetComponent<Collider2D>();
            initialLocalPos = transform.localPosition;
            floatOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (isCollected || !isInteractable) return;

            // Gentle floating bob animation
            float bobY = Mathf.Sin((Time.time * 3.5f) + floatOffset) * 0.08f;
            transform.localPosition = new Vector3(initialLocalPos.x, initialLocalPos.y + bobY, initialLocalPos.z);
        }

        /// <summary>
        /// Called when revealed after breaking a block.
        ///- Gold Key: automatically flies to the ladder and unlocks it.
        ///- Heart, Shield, Damage: automatically flies to the front of the player character and auto-pickups.
        /// </summary>
        public void OnRevealedFromBlock(float delay = 0.25f)
        {
            isInteractable = false;
            initialLocalPos = transform.localPosition;

            if (itemCollider == null) itemCollider = GetComponent<Collider2D>();
            if (itemCollider != null) itemCollider.enabled = false;

            if (itemType == ItemPickupType.Key)
            {
                // Auto-fly to ladder and unlock it without requiring manual click
                if (Application.isPlaying)
                {
                    StartCoroutine(AutoFlyToLadderRoutine(delay));
                }
                else
                {
                    isInteractable = true;
                    Collect();
                }
                return;
            }

            // Heart, Shield, and Damage items automatically fly to the player character and auto-pickup!
            if (Application.isPlaying)
            {
                StartCoroutine(AutoFlyToPlayerRoutine(delay));
            }
            else
            {
                isInteractable = true;
                Collect();
            }
        }

        private IEnumerator AutoFlyToPlayerRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (isCollected) yield break;

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = "Default";
                spriteRenderer.sortingOrder = 30; // Fly above blocks and enemies
            }

            Transform badge = transform.Find("ValueBadge");
            if (badge != null)
            {
                var badgeCanvas = badge.GetComponent<Canvas>();
                if (badgeCanvas != null)
                {
                    badgeCanvas.sortingOrder = 31;
                }
            }

            Vector3 startPos = transform.position;
            Vector3 targetPos = GetPlayerPickupTargetPosition(startPos);

            float duration = 0.65f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 peakScale = startScale * 1.28f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                targetPos = GetPlayerPickupTargetPosition(targetPos);

                // Smooth arc toward the front of the player character
                float arcY = Mathf.Sin(t * Mathf.PI) * 1.35f;
                transform.position = Vector3.Lerp(startPos, targetPos, smoothT) + new Vector3(0f, arcY, 0f);

                if (t < 0.35f)
                {
                    transform.localScale = Vector3.Lerp(startScale, peakScale, t / 0.35f);
                }
                else
                {
                    transform.localScale = Vector3.Lerp(peakScale, startScale * 0.85f, (t - 0.35f) / 0.65f);
                }

                yield return null;
            }

            transform.position = targetPos;
            isInteractable = true;
            Collect();
        }

        private Vector3 GetPlayerPickupTargetPosition(Vector3 fallbackPos)
        {
            var player = PlayerInstance.Instance;
            if (player == null)
            {
                player = UnityEngine.Object.FindFirstObjectByType<PlayerInstance>();
            }

            if (player != null)
            {
                return player.GetPlayerCenterPosition();
            }

            return fallbackPos;
        }

        private IEnumerator AutoFlyToLadderRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            // Remove/destroy value badge so only the clean golden key flies
            Transform badge = transform.Find("ValueBadge");
            if (badge != null)
            {
                Destroy(badge.gameObject);
            }

            CatPowerUpController.TriggerNotification("Gold Key Found!", new Color(1f, 0.85f, 0.2f));

            LevelLadder ladder = LevelLadder.Instance != null ? LevelLadder.Instance : UnityEngine.Object.FindFirstObjectByType<LevelLadder>();
            Vector3 startPos = transform.position;
            Vector3 targetPos = ladder != null ? ladder.transform.position : startPos;

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = "Default";
                spriteRenderer.sortingOrder = 30; // Fly above blocks and enemies
            }

            float duration = 0.75f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 peakScale = startScale * 1.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (ladder != null)
                {
                    targetPos = ladder.transform.position;
                }

                // Parabolic curved arc
                float arcY = Mathf.Sin(t * Mathf.PI) * 1.6f;
                Vector3 curPos = Vector3.Lerp(startPos, targetPos, smoothT) + new Vector3(0f, arcY, 0f);
                transform.position = curPos;

                // Wobble tilt during flight
                float angle = Mathf.Sin(t * Mathf.PI * 4f) * 25f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);

                // Scale up then shrink slightly when nearing the lock
                if (t < 0.35f)
                {
                    transform.localScale = Vector3.Lerp(startScale, peakScale, t / 0.35f);
                }
                else
                {
                    transform.localScale = Vector3.Lerp(peakScale, startScale * 0.7f, (t - 0.35f) / 0.65f);
                }

                yield return null;
            }

            // Arrived at ladder! Unlock the lock
            if (ladder != null)
            {
                ladder.UnlockByIncomingKey();
            }
            else
            {
                PlayerInstance.SetHasKey(true);
            }

            isCollected = true;
            Destroy(gameObject);
        }

        public void Initialize(ItemPickupType type, int value, Sprite sprite)
        {
            itemType = type;
            itemValue = Mathf.Max(1, value);

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            BuildValueBadge();
        }

        private void BuildValueBadge()
        {
            // Create worldspace badge below icon to display e.g. +3, +1, +5
            var badgeObj = new GameObject("ValueBadge");
            badgeObj.transform.SetParent(transform, false);
            badgeObj.transform.localPosition = new Vector3(0f, -0.65f, -0.05f);

            var canvas = badgeObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "Default";
            canvas.sortingOrder = 15;

            var rt = badgeObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(54f, 24f);
            rt.localScale = new Vector3(0.015f, 0.015f, 1f);

            var bg = badgeObj.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);
            bg.raycastTarget = false;

            var outline = badgeObj.AddComponent<Outline>();
            Color outlineColor = itemType == ItemPickupType.Heart ? new Color(1f, 0.35f, 0.45f, 0.8f) :
                                 (itemType == ItemPickupType.Shield ? new Color(0.2f, 0.85f, 1f, 0.9f) :
                                 (itemType == ItemPickupType.Coin ? new Color(1f, 0.82f, 0.2f, 0.95f) :
                                 (itemType == ItemPickupType.Damage ? new Color(1f, 0.8f, 0.2f, 0.8f) :
                                                                      new Color(1f, 0.85f, 0.2f, 0.9f))));
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, -1f);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(badgeObj.transform, false);
            var txtRT = txtObj.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            var txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            if (itemType == ItemPickupType.Key)
            {
                txt.text = "<color=#FFD700><b>KEY</b></color>";
            }
            else if (itemType == ItemPickupType.Shield)
            {
                rt.sizeDelta = new Vector2(68f, 24f);
                txt.fontSize = 16;
                txt.text = "<color=#00E5FF><b>SHIELD</b></color>";
            }
            else if (itemType == ItemPickupType.Coin)
            {
                rt.sizeDelta = new Vector2(50f, 24f);
                txt.fontSize = 17;
                txt.text = $"<color=#FFD700><b>+{itemValue}</b></color>";
            }
            else
            {
                string colorHex = itemType == ItemPickupType.Heart ? "#55FF88" : "#FFD700";
                txt.text = $"<color={colorHex}><b>+{itemValue}</b></color>";
            }

            var shadow = txtObj.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        public void Collect()
        {
            if (!isInteractable || isCollected) return;

            // If collected while still sitting away from the player, auto-fly to the player first!
            Vector3 playerTarget = GetPlayerPickupTargetPosition(transform.position);
            if (Application.isPlaying && Vector3.Distance(transform.position, playerTarget) > 1.2f)
            {
                isInteractable = false;
                StartCoroutine(AutoFlyToPlayerRoutine(0f));
                return;
            }

            isCollected = true;
            if (itemCollider != null) itemCollider.enabled = false;

            // Apply stat to player
            ApplyItemEffect();

            // Collect animation & destroy
            if (Application.isPlaying)
            {
                StartCoroutine(CollectAnimationRoutine());
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void ApplyItemEffect()
        {
            var player = PlayerInstance.Instance;
            string noticeMsg = "";
            Color noticeColor = Color.white;

            switch (itemType)
            {
                case ItemPickupType.Heart:
                    Digit7s.UI.UISoundManager.PlayHealthPickup();
                    if (player != null)
                    {
                        player.Heal(itemValue);
                    }
                    noticeMsg = $"+{itemValue} HP";
                    noticeColor = new Color(0.2f, 1f, 0.45f);
                    Debug.Log($"<color=green>[ItemPickup] Collected Heart! Healed +{itemValue} HP.</color>");
                    break;

                case ItemPickupType.Damage:
                    Digit7s.UI.UISoundManager.PlaySwordPickup();
                    if (player != null)
                    {
                        player.IncreaseDamage(itemValue);
                    }
                    noticeMsg = $"+{itemValue} DMG";
                    noticeColor = new Color(1f, 0.85f, 0.25f);
                    Debug.Log($"<color=yellow>[ItemPickup] Collected Damage! Increased DMG by +{itemValue}.</color>");
                    break;

                case ItemPickupType.Key:
                    PlayerInstance.SetHasKey(true);
                    noticeMsg = "GOLD KEY COLLECTED!";
                    noticeColor = new Color(1f, 0.85f, 0.2f);
                    Debug.Log("<color=yellow>[ItemPickup] Collected Gold Key! Ladder can now be unlocked.</color>");
                    break;

                case ItemPickupType.Shield:
                    var powerUp = CatPowerUpController.Instance;
                    if (powerUp == null && player != null)
                    {
                        powerUp = player.GetComponent<CatPowerUpController>();
                    }
                    if (powerUp != null)
                    {
                        powerUp.GrantOneTimeShield(itemValue);
                        noticeMsg = $"SHIELD READY! ({powerUp.ShieldCharges}/{CatPowerUpController.MaxShieldCharges})";
                    }
                    else
                    {
                        noticeMsg = "SHIELD READY! (1x BLOCK)";
                    }
                    noticeColor = new Color(0.3f, 0.85f, 1f);
                    Debug.Log($"<color=cyan>[ItemPickup] Collected Shield! Shield charges: {powerUp?.ShieldCharges ?? 1}/{CatPowerUpController.MaxShieldCharges}.</color>");
                    break;

                case ItemPickupType.Coin:
                    Digit7s.UI.UISoundManager.PlayCoinPickup();
                    if (player != null)
                    {
                        player.AddCoins(itemValue);
                        noticeMsg = $"+{itemValue} COIN ({player.CurrentCoins}/5)";
                    }
                    else
                    {
                        noticeMsg = $"+{itemValue} COIN";
                    }
                    noticeColor = new Color(1f, 0.85f, 0.2f);
                    Debug.Log($"<color=yellow>[ItemPickup] Collected Coin (+{itemValue})! Total coins: {player?.CurrentCoins ?? itemValue}</color>");
                    break;
            }

            CatPowerUpController.TriggerNotification(noticeMsg, noticeColor);
        }

        private IEnumerator CollectAnimationRoutine()
        {
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = startScale * 1.35f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + new Vector3(0f, 0.6f, 0f);

            float elapsed = 0f;
            float duration = 0.16f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
                if (spriteRenderer != null)
                {
                    Color c = spriteRenderer.color;
                    spriteRenderer.color = new Color(c.r, c.g, c.b, 1f - t);
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnMouseUpAsButton()
        {
            if (!isInteractable) return;
            Collect();
        }
    }
}
