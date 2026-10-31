using System;
using System.Collections;
using UnityEngine;
using Spine.Unity;
using Digit7s.Enemies;

namespace Digit7s.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class DestructibleBlock : MonoBehaviour
    {
        [Header("Effects & Prefabs")]
        [Tooltip("Explosion or particle effect spawned when block is destroyed")]
        [SerializeField] private GameObject breakEffectPrefab;

        [Header("Destruction Settings")]
        [Tooltip("Delay before destroying GameObject")]
        [SerializeField] private float destroyDelay = 0f;

        [Tooltip("Smooth shrink animation before deletion")]
        [SerializeField] private bool animateShrinkOnBreak = true;

        [Tooltip("Score awarded when this block is cut/destroyed")]
        [SerializeField] private int scoreValue = 10;

        [Header("Enemy Spawning on Break")]
        [Tooltip("Offset applied when an assigned enemy spawns from this block")]
        [SerializeField] private Vector3 enemySpawnOffset = Vector3.zero;

        public static event Action<DestructibleBlock> OnAnyBlockDestroyed;
        public static event Action<DestructibleBlock, EnemyInstance> OnBlockSpawnedEnemy;

        private bool isDestroyed = false;
        private Collider2D blockCollider;
        private SpriteRenderer spriteRenderer;

        private bool hasAssignedEnemy = false;
        private RolledEnemyStats assignedEnemyStats;

        [Header("Explosion FX Tuning")]
        [Tooltip("Scale factor for the explosion burst.")]
        [SerializeField] private float explosionScale = 0.7f;

        [Tooltip("Additional manual offset to tweak explosion position (X, Y) if desired.")]
        [SerializeField] private Vector2 explosionOffset = Vector2.zero;

        // Internal Spine skeleton center offset in local units
        private const float SpineExplosionInternalCenterY = 2.21f;

        [Header("Hidden Content")]
        [Tooltip("Pre-spawned object hidden beneath this block (Ladder, Enemy, Wooden Prop)")]
        [SerializeField] private GameObject hiddenContent;

        [Header("Lock Visuals")]
        [Tooltip("Color tint applied when this block is locked by an active enemy in 8 directions")]
        [SerializeField] private Color enemyLockedColor = new Color(0.60f, 0.28f, 0.28f, 1f);

        [Tooltip("Color tint applied when this block is not yet reachable (not adjacent to any cleared space)")]
        [SerializeField] private Color unreachableColor = new Color(0.38f, 0.40f, 0.48f, 0.95f);

        [Tooltip("Normal default color when this block is reachable and clickable")]
        [SerializeField] private Color defaultColor = Color.white;

        private static readonly System.Collections.Generic.List<Vector3> clearedPositions = new System.Collections.Generic.List<Vector3>();

        public static void ResetClearedPositions()
        {
            clearedPositions.Clear();
        }

        public static void RegisterClearedPosition(Vector3 pos)
        {
            for (int i = 0; i < clearedPositions.Count; i++)
            {
                if (Vector3.Distance(clearedPositions[i], pos) < 0.5f) return;
            }
            clearedPositions.Add(pos);
            UpdateAllBlocksVisuals();
        }

        public static bool IsAdjacentToClearedSpace(Vector3 pos)
        {
            for (int i = 0; i < clearedPositions.Count; i++)
            {
                Vector3 cPos = clearedPositions[i];
                float dx = Mathf.Abs(pos.x - cPos.x);
                float dy = Mathf.Abs(pos.y - cPos.y);

                // 4-direction adjacency (Up, Down, Left, Right)
                bool isHorizontalNeighbor = (dx >= 1.4f && dx <= 3.2f && dy <= 1.0f);
                bool isVerticalNeighbor = (dy >= 1.4f && dy <= 3.2f && dx <= 1.0f);

                if (isHorizontalNeighbor || isVerticalNeighbor)
                {
                    return true;
                }
            }
            return false;
        }

        public static void UpdateAllBlocksVisuals()
        {
            var blocks = FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None);
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] != null && !blocks[i].isDestroyed)
                {
                    blocks[i].UpdateVisualState();
                }
            }
        }

        public bool IsClickable()
        {
            if (isDestroyed) return false;
            if (!IsAdjacentToClearedSpace(transform.position)) return false;
            if (EnemyInstance.IsBlockLocked(transform.position)) return false;
            return true;
        }

        private bool isVisuallyLocked = false;
        private GameObject enemyScoutMarker;

        public bool IsDestroyed => isDestroyed;
        public int ScoreValue => scoreValue;
        public bool IsVisuallyLocked => isVisuallyLocked;
        public GameObject HiddenContent => hiddenContent;
        public bool HasAssignedEnemy => hasAssignedEnemy;
        public RolledEnemyStats AssignedEnemyStats => assignedEnemyStats;

        public bool HasEnemyInside
        {
            get
            {
                if (isDestroyed) return false;
                if (hasAssignedEnemy && assignedEnemyStats.enemyData != null) return true;
                if (hiddenContent != null && hiddenContent.GetComponent<EnemyInstance>() != null) return true;
                return false;
            }
        }

        public void RevealEnemyIndicator()
        {
            if (isDestroyed || !HasEnemyInside || enemyScoutMarker != null) return;

            enemyScoutMarker = new GameObject("EnemyScoutMarker");
            enemyScoutMarker.transform.SetParent(transform, false);
            enemyScoutMarker.transform.localPosition = new Vector3(0f, 0f, -0.15f);

            var canvas = enemyScoutMarker.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "Default";
            canvas.sortingOrder = 25;

            var rt = enemyScoutMarker.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(36f, 36f);
            rt.localScale = new Vector3(0.013f, 0.013f, 1f);

            // Circular alert badge container
            var badgeBg = enemyScoutMarker.AddComponent<UnityEngine.UI.Image>();
            badgeBg.color = new Color(0.12f, 0.08f, 0.14f, 0.90f);
            badgeBg.raycastTarget = false;

            var badgeOutline = enemyScoutMarker.AddComponent<UnityEngine.UI.Outline>();
            badgeOutline.effectColor = new Color(1f, 0.78f, 0.2f, 0.95f); // Vibrant Gold
            badgeOutline.effectDistance = new Vector2(1.2f, -1.2f);

            // Clean simple "!" exclamation mark
            var txtObj = new GameObject("AlertText");
            txtObj.transform.SetParent(enemyScoutMarker.transform, false);
            var txtRT = txtObj.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            var txt = txtObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = "<color=#FFCC00><b>!</b></color>";
            txt.raycastTarget = false;

            var shadow = txtObj.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1f, -1f);

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                StartCoroutine(PulseScoutMarkerRoutine(enemyScoutMarker.transform));
            }

            Debug.Log($"<color=yellow>[DestructibleBlock] '{name}' has an enemy inside and was SCOUTED with '!' indicator!</color>");
        }

        private IEnumerator PulseScoutMarkerRoutine(Transform marker)
        {
            if (marker == null) yield break;
            Vector3 baseScale = marker.localScale;
            while (marker != null)
            {
                float t = Mathf.PingPong(Time.time * 2.8f, 1f);
                float scaleMult = Mathf.Lerp(1.0f, 1.12f, t);
                marker.localScale = baseScale * scaleMult;
                yield return null;
            }
        }

        public static System.Collections.Generic.List<DestructibleBlock> GetAdjacentBlocks(Vector3 originPos)
        {
            var result = new System.Collections.Generic.List<DestructibleBlock>();
            var allBlocks = UnityEngine.Object.FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None);
            foreach (var b in allBlocks)
            {
                if (b == null || b.IsDestroyed) continue;
                float dx = Mathf.Abs(b.transform.position.x - originPos.x);
                float dy = Mathf.Abs(b.transform.position.y - originPos.y);

                bool isHorizontal = (dx >= 1.2f && dx <= 3.2f && dy <= 0.9f);
                bool isVertical = (dy >= 1.2f && dy <= 3.2f && dx <= 0.9f);
                if (isHorizontal || isVertical)
                {
                    result.Add(b);
                }
            }
            return result;
        }

        private void Awake()
        {
            blockCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }

            // Auto-load default explosion prefab if none assigned
            if (breakEffectPrefab == null)
            {
                breakEffectPrefab = Resources.Load<GameObject>("ExplosionFx");
#if UNITY_EDITOR
                if (breakEffectPrefab == null)
                {
                    breakEffectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spine Characters/Explosion/ExplosionFx.prefab");
                }
#endif
            }
        }

        private void Start()
        {
            UpdateVisualState();
        }

        private void OnEnable()
        {
            UpdateVisualState();
        }

        private void Update()
        {
            if (isDestroyed || !Application.isPlaying) return;

            UpdateVisualState();
        }

        public void UpdateVisualState()
        {
            if (spriteRenderer == null || isDestroyed) return;

            if (EnemyInstance.IsBlockLocked(transform.position))
            {
                spriteRenderer.color = enemyLockedColor;
            }
            else if (!IsAdjacentToClearedSpace(transform.position))
            {
                spriteRenderer.color = unreachableColor;
            }
            else
            {
                spriteRenderer.color = defaultColor;
            }
        }

        /// <summary>
        /// Breaks and removes the block with visual feedback.
        /// </summary>
        public void BreakBlock(bool force = false)
        {
            if (isDestroyed) return;
            if (!force && Application.isPlaying)
            {
                if (EnemyInstance.IsBlockLocked(transform.position))
                {
                    Debug.Log($"[DestructibleBlock] '{name}' click ignored: Cannot break block because it is locked by an active enemy in 8 directions!");
                    CatPowerUpController.TriggerNotification("Defeat nearby enemy first!", new Color(1f, 0.4f, 0.4f));
                    return;
                }

                if (!IsAdjacentToClearedSpace(transform.position))
                {
                    Debug.Log($"[DestructibleBlock] '{name}' click ignored: Cannot break block because it is not adjacent to any cleared space!");
                    CatPowerUpController.TriggerNotification("Break adjacent blocks first!", new Color(0.7f, 0.7f, 0.7f));
                    return;
                }
            }
            isDestroyed = true;
            RegisterClearedPosition(transform.position);

            if (enemyScoutMarker != null)
            {
                Destroy(enemyScoutMarker);
                enemyScoutMarker = null;
            }

            // Disable collider immediately so it cannot be hit again
            if (blockCollider != null)
            {
                blockCollider.enabled = false;
            }

            // Spawn explosion visual effect aligned with the block center
            if (breakEffectPrefab != null)
            {
                Vector3 spawnPos = transform.position + new Vector3(
                    explosionOffset.x,
                    explosionOffset.y - (SpineExplosionInternalCenterY * explosionScale),
                    -0.2f
                );

                var fx = Instantiate(breakEffectPrefab, spawnPos, Quaternion.identity);
                fx.transform.localScale = new Vector3(explosionScale, explosionScale, 1f);

                var fxRenderer = fx.GetComponent<Renderer>();
                if (fxRenderer != null && spriteRenderer != null)
                {
                    fxRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                    fxRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;
                }

                // Ensure explosion animation does NOT loop and destroys exactly when animation finishes
                var sa = fx.GetComponent<SkeletonAnimation>();
                if (sa != null)
                {
                    sa.loop = false;
                    if (sa.AnimationState != null)
                    {
                        var track = sa.AnimationState.SetAnimation(0, "Explossion", false);
                        if (track != null)
                        {
                            track.Loop = false;
                        }

                        sa.AnimationState.Complete += (entry) =>
                        {
                            if (fx != null)
                            {
                                Destroy(fx);
                            }
                        };
                    }
                }

                // Fallback destroy in case animation events do not fire
                if (Application.isPlaying)
                {
                    Destroy(fx, 0.7f);
                }
                else
                {
                    DestroyImmediate(fx);
                }
            }

            // Play contextual reveal sound based on revealed contents (Hp, Damage, Shield, Enemy, Key, Empty)
            PlayContextualRevealSound();

            // Reveal pre-spawned hidden object (Ladder, Enemy, Wooden Prop) if one was assigned
            if (hiddenContent != null)
            {
                hiddenContent.SetActive(true);
                var enemy = hiddenContent.GetComponent<EnemyInstance>();
                if (enemy != null)
                {
                    enemy.CenterToBlock();
                    enemy.OnRevealedFromBlock(0.65f);
                    OnBlockSpawnedEnemy?.Invoke(this, enemy);
                }

                var ladder = hiddenContent.GetComponent<LevelLadder>();
                if (ladder != null)
                {
                    ladder.OnRevealedFromBlock(0.65f);
                }

                var prop = hiddenContent.GetComponent<WoodenBreakableProp>();
                if (prop != null)
                {
                    prop.OnRevealedFromBlock(0.65f);
                }

                var itemPickup = hiddenContent.GetComponent<ItemPickupProp>();
                if (itemPickup != null)
                {
                    itemPickup.OnRevealedFromBlock(0.25f);
                }

                var npcAlly = hiddenContent.GetComponent<NpcAllyInstance>();
                if (npcAlly != null)
                {
                    npcAlly.OnRevealedFromBlock(0.45f);
                }
            }
            // Fallback for dynamically rolled enemies
            else if (hasAssignedEnemy && assignedEnemyStats.enemyData != null)
            {
                SpawnAssignedEnemy();
            }

            OnAnyBlockDestroyed?.Invoke(this);

            if (Application.isPlaying)
            {
                if (animateShrinkOnBreak && gameObject.activeInHierarchy)
                {
                    StartCoroutine(ShrinkAndDestroyRoutine());
                }
                else
                {
                    Destroy(gameObject, destroyDelay);
                }
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public void SetHiddenContent(GameObject content)
        {
            hiddenContent = content;
            if (hiddenContent != null)
            {
                hiddenContent.SetActive(false);
            }
        }

        public void ClearHiddenContent()
        {
            if (hiddenContent != null)
            {
                if (Application.isPlaying) Destroy(hiddenContent);
                else DestroyImmediate(hiddenContent);
                hiddenContent = null;
            }
        }

        private GameObject SpawnAssignedEnemy()
        {
            if (assignedEnemyStats.enemyData == null || assignedEnemyStats.enemyData.enemyPrefab == null)
            {
                return null;
            }

            Vector3 spawnOffset = enemySpawnOffset != Vector3.zero
                ? enemySpawnOffset
                : assignedEnemyStats.enemyData.GetSpawnOffset();

            Vector3 spawnPos = transform.position + spawnOffset;
            var enemyObj = Instantiate(assignedEnemyStats.enemyData.enemyPrefab, spawnPos, Quaternion.identity);
            enemyObj.name = $"{assignedEnemyStats.enemyData.enemyName}_Lv{assignedEnemyStats.roomLevel}_HP{assignedEnemyStats.health}_DMG{assignedEnemyStats.damage}";

            var instance = enemyObj.GetComponent<EnemyInstance>();
            if (instance == null)
            {
                instance = enemyObj.AddComponent<EnemyInstance>();
            }
            instance.SourceBlockPosition = transform.position;
            instance.Initialize(assignedEnemyStats, 0.65f);
            instance.CenterToBlock();

            OnBlockSpawnedEnemy?.Invoke(this, instance);
            Debug.Log($"[DestructibleBlock] Spawned enemy '{enemyObj.name}' from block at {spawnPos} (attack unlocked in 0.65s after explosion)");
            return enemyObj;
        }

        public void SetAssignedEnemy(RolledEnemyStats stats)
        {
            assignedEnemyStats = stats;
            hasAssignedEnemy = true;
        }

        public void ClearAssignedEnemy()
        {
            assignedEnemyStats = default;
            hasAssignedEnemy = false;
        }

        private void PlayContextualRevealSound()
        {
            if (hiddenContent != null)
            {
                var itemPickup = hiddenContent.GetComponent<ItemPickupProp>();
                if (itemPickup != null)
                {
                    switch (itemPickup.ItemType)
                    {
                        case ItemPickupType.Heart:
                            Digit7s.UI.UISoundManager.PlayBlockRevealHeart();
                            break;
                        case ItemPickupType.Damage:
                            Digit7s.UI.UISoundManager.PlayBlockRevealDamage();
                            break;
                        case ItemPickupType.Shield:
                            Digit7s.UI.UISoundManager.PlayBlockRevealShield();
                            break;
                        case ItemPickupType.Key:
                            Digit7s.UI.UISoundManager.PlayBlockRevealKey();
                            break;
                        default:
                            Digit7s.UI.UISoundManager.PlayBlockRevealEmpty();
                            break;
                    }
                    return;
                }

                if (hiddenContent.GetComponent<EnemyInstance>() != null)
                {
                    Digit7s.UI.UISoundManager.PlayBlockRevealEnemy();
                    return;
                }

                if (hiddenContent.GetComponent<LevelLadder>() != null)
                {
                    Digit7s.UI.UISoundManager.PlayBlockRevealKey();
                    return;
                }

                if (hiddenContent.GetComponent<NpcAllyInstance>() != null)
                {
                    Digit7s.UI.UISoundManager.PlayBlockRevealShield();
                    return;
                }

                // Wooden prop or other empty content
                Digit7s.UI.UISoundManager.PlayBlockRevealEmpty();
            }
            else if (hasAssignedEnemy && assignedEnemyStats.enemyData != null)
            {
                Digit7s.UI.UISoundManager.PlayBlockRevealEnemy();
            }
            else
            {
                Digit7s.UI.UISoundManager.PlayBlockRevealEmpty();
            }
        }

        private IEnumerator ShrinkAndDestroyRoutine()
        {
            float elapsed = 0f;
            Vector3 origScale = transform.localScale;
            while (elapsed < destroyDelay)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / destroyDelay);
                transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
