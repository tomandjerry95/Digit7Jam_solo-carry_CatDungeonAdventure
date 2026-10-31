using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Digit7s.UI;

namespace Digit7s.Gameplay
{
    public enum NpcAllyType
    {
        CatBoxing = 0,
        CatGuardian = 1
    }

    /// <summary>
    /// Represents a friendly NPC Cat ally (CatBoxing or CatGuardian) hidden beneath a '?' block.
    /// - Maximum 1 NPC per room level.
    /// - Spawns with 10% base probability; increases by +10% each level it doesn't appear, and resets to 10% when it appears.
    /// - CatBoxing: Base DMG 2-5, HP 10-20. Every 3 levels: DMG +3, HP +6 (min & max). Gains +1 DMG whenever an enemy dies.
    /// - CatGuardian: Base DMG 1-3, HP 6-12. Every 3 levels: DMG +2, HP +5 (min & max). Starts with 2 Shields, gains +1 Shield whenever an enemy dies.
    /// - Combat: Adds NPC Damage to Player Damage when clicking an enemy. Takes Enemy counter-attack damage first until defeated, after which damage goes to the Player.
    /// </summary>
    [SelectionBase]
    public class NpcAllyInstance : MonoBehaviour
    {
        public static NpcAllyInstance ActiveAlly { get; private set; }

        public static NpcAllyInstance GetActiveAliveAlly()
        {
            if (ActiveAlly != null && ActiveAlly.gameObject != null && ActiveAlly.gameObject.activeInHierarchy && ActiveAlly.isRevealed && !ActiveAlly.IsDead)
            {
                return ActiveAlly;
            }
            return null;
        }

        [Header("NPC Identity & Rolled Stats")]
        [SerializeField] private NpcAllyType allyType = NpcAllyType.CatBoxing;
        [SerializeField] private int roomLevel = 1;
        [SerializeField] private int currentHealth = 10;
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int damage = 2;
        [SerializeField] private int shieldCharges = 0;
        [SerializeField] private bool isRevealed = false;

        [Header("Components")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private EnemyStatUI statUI;

        private GameObject shieldIndicatorObj;
        private readonly List<SpriteRenderer> stackedShieldRenderers = new List<SpriteRenderer>();
        private UnityEngine.UI.Text shieldCountBadgeText;
        private GameObject shieldCountBadgeObj;
        private Vector3 shieldBaseLocalPos = new Vector3(0.85f, 1.45f, -0.15f);
        private Vector3 shieldBaseScale = new Vector3(0.65f, 0.65f, 1f);
        private const int MaxVisualShieldLayers = 3;

        public NpcAllyType AllyType => allyType;
        public string DisplayName => allyType == NpcAllyType.CatBoxing ? "Cat Boxing" : "Cat Guardian";
        public int RoomLevel => roomLevel;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int Damage => damage;
        public int ShieldCharges => shieldCharges;
        public bool IsRevealed => isRevealed;
        public bool IsDead => currentHealth <= 0;
        public EnemyStatUI StatUI => statUI;

        /// <summary>
        /// Calculates the scaled HP and Damage ranges for the given NPC type and room level.
        /// Every 3 levels completed (step = (roomLevel - 1) / 3):
        /// - CatBoxing (Base DMG 2-3, HP 5-7): DMG +3, HP +3 (min & max)
        /// - CatGuardian (Base DMG 1-2, HP 2-5): DMG +2, HP +2 (min & max), 1 starting shield
        /// </summary>
        public static void GetScaledStatRanges(NpcAllyType type, int targetRoomLevel, out int minHp, out int maxHp, out int minDmg, out int maxDmg, out int initialShields)
        {
            int step = Mathf.Max(0, (Mathf.Max(1, targetRoomLevel) - 1) / 3);

            if (type == NpcAllyType.CatBoxing)
            {
                int hpBonus = step * 3;
                int dmgBonus = step * 3;
                minHp = 5 + hpBonus;
                maxHp = 7 + hpBonus;
                minDmg = 2 + dmgBonus;
                maxDmg = 3 + dmgBonus;
                initialShields = 0;
            }
            else // CatGuardian
            {
                int hpBonus = step * 2;
                int dmgBonus = step * 2;
                minHp = 2 + hpBonus;
                maxHp = 5 + hpBonus;
                minDmg = 1 + dmgBonus;
                maxDmg = 2 + dmgBonus;
                initialShields = 1;
            }
        }

        private void Awake()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }
            EnsureRendererSorting();
        }

        private void OnEnable()
        {
            EnemyInstance.OnEnemyKilled -= HandleEnemyKilled;
            EnemyInstance.OnEnemyKilled += HandleEnemyKilled;

            if (isRevealed && !IsDead)
            {
                ActiveAlly = this;
            }
        }

        private void OnDisable()
        {
            EnemyInstance.OnEnemyKilled -= HandleEnemyKilled;
            if (ActiveAlly == this)
            {
                ActiveAlly = null;
            }
        }

        private void OnDestroy()
        {
            EnemyInstance.OnEnemyKilled -= HandleEnemyKilled;
            if (ActiveAlly == this)
            {
                ActiveAlly = null;
            }
        }

        private void Update()
        {
            if (shieldIndicatorObj != null && shieldIndicatorObj.activeSelf)
            {
                float bobY = Mathf.Sin(Time.time * 3.5f) * 0.04f;
                float pulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.05f;
                shieldIndicatorObj.transform.localPosition = shieldBaseLocalPos + new Vector3(0f, bobY, 0f);
                shieldIndicatorObj.transform.localScale = shieldBaseScale * pulse;
            }
        }

        public void Initialize(NpcAllyType type, int targetRoomLevel)
        {
            allyType = type;
            roomLevel = Mathf.Max(1, targetRoomLevel);

            GetScaledStatRanges(allyType, roomLevel, out int minHp, out int maxHp, out int minDmg, out int maxDmg, out int startShields);

            maxHealth = Random.Range(minHp, maxHp + 1);
            currentHealth = maxHealth;
            damage = Random.Range(minDmg, maxDmg + 1);
            shieldCharges = startShields;
            isRevealed = false;

            // Remove any leftover collider so the NPC does not block clicks or get attacked by player
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            EnsureRendererSorting();
            SetupStatUI();
            if (statUI != null)
            {
                statUI.SetVisible(false);
            }
            UpdateShieldVisual();
            if (shieldIndicatorObj != null)
            {
                shieldIndicatorObj.SetActive(false);
            }

            PlayIdleAnimation();
        }

        public void OnRevealedFromBlock(float revealDelay = 0.45f)
        {
            gameObject.SetActive(true);
            isRevealed = true;
            ActiveAlly = this;

            EnemyInstance.OnEnemyKilled -= HandleEnemyKilled;
            EnemyInstance.OnEnemyKilled += HandleEnemyKilled;

            EnsureRendererSorting();
            SetupStatUI();
            PlayIdleAnimation();

            string notif = allyType == NpcAllyType.CatBoxing
                ? $"ALLY JOINED: CAT BOXING! (HP:{currentHealth} DMG:{damage})"
                : $"ALLY JOINED: CAT GUARDIAN! (HP:{currentHealth} DMG:{damage} SHIELD:x{shieldCharges})";
            CatPowerUpController.TriggerNotification(notif, new Color(0.25f, 1f, 0.65f));

            if (revealDelay > 0f && Application.isPlaying && gameObject.activeInHierarchy)
            {
                if (statUI != null) statUI.SetVisible(false);
                if (shieldIndicatorObj != null) shieldIndicatorObj.SetActive(false);
                StartCoroutine(ShowUIAfterExplosionRoutine(revealDelay));
            }
            else
            {
                if (statUI != null)
                {
                    statUI.SetVisible(true);
                    statUI.AlignPosition();
                }
                UpdateShieldVisual();
            }

            Debug.Log($"<color=green>[NpcAllyInstance] Revealed '{DisplayName}' at Lv{roomLevel} -> HP: {currentHealth}/{maxHealth}, DMG: {damage}, Shields: {shieldCharges}</color>");
        }

        private IEnumerator ShowUIAfterExplosionRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!IsDead)
            {
                if (statUI != null)
                {
                    statUI.SetVisible(true);
                    statUI.AlignPosition();
                }
                UpdateShieldVisual();
            }
        }

        private void EnsureRendererSorting()
        {
            if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
            {
                skeletonAnimation.Skeleton.SetColor(Color.white);
            }

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = 10;
                if (mr.sharedMaterial != null && Application.isPlaying && mr.material != null)
                {
                    mr.material.color = Color.white;
                }
            }
        }

        private void SetupStatUI()
        {
            if (statUI == null)
            {
                var uiObj = new GameObject("NpcStatUI");
                uiObj.transform.SetParent(transform, false);
                statUI = uiObj.AddComponent<EnemyStatUI>();
            }

            statUI.Initialize(currentHealth, maxHealth, damage);
            statUI.SetAllyTheme();
        }

        /// <summary>
        /// Passive triggered whenever an enemy is killed in the room while this NPC ally is revealed and alive:
        /// - CatBoxing: +1 Damage per enemy killed ("enemy တကောင်သေးတိုင်း +1 damage တိုးပေးမယ်")
        /// - CatGuardian: +1 Shield per enemy killed ("enemy တကောင်သေးတိုင်း +1 shield သူရှိမှာတိုးပေးမယ်")
        /// </summary>
        private void HandleEnemyKilled(EnemyInstance killedEnemy)
        {
            if (this == null || !gameObject || !isRevealed || IsDead)
            {
                EnemyInstance.OnEnemyKilled -= HandleEnemyKilled;
                return;
            }

            if (allyType == NpcAllyType.CatBoxing)
            {
                damage += 1;
                if (statUI != null)
                {
                    statUI.UpdateStats(currentHealth, maxHealth, damage);
                    statUI.ShowStatusPopup("+1 DMG!", new Color(1f, 0.85f, 0.25f));
                }
                UISoundManager.PlayCatBrawlerEffect();
                Debug.Log($"<color=yellow>[NpcAllyInstance] CatBoxing passive triggered on enemy kill: +1 DMG -> Current DMG: {damage}</color>");
            }
            else if (allyType == NpcAllyType.CatGuardian)
            {
                shieldCharges += 1;
                UpdateShieldVisual();
                if (statUI != null)
                {
                    statUI.ShowStatusPopup($"+1 SHIELD!", new Color(0.3f, 0.9f, 1f));
                }
                UISoundManager.PlayCatGuardianEffect();
                Debug.Log($"<color=cyan>[NpcAllyInstance] CatGuardian passive triggered on enemy kill: +1 Shield -> Current Shields: {shieldCharges}</color>");
            }
        }

        /// <summary>
        /// Evaluates an incoming enemy counter-attack against this NPC ally.
        /// If the NPC has shield charges (CatGuardian), consumes 1 shield and blocks the damage (0 damage taken).
        /// </summary>
        public bool ProcessIncomingEnemyAttack(int enemyDamage, out int actualDamageToNpc)
        {
            if (shieldCharges > 0)
            {
                shieldCharges = Mathf.Max(0, shieldCharges - 1);
                UpdateShieldVisual();
                actualDamageToNpc = 0;
                UISoundManager.PlayCatGuardianEffect();
                Debug.Log($"<color=cyan>[NpcAllyInstance] {DisplayName} consumed 1 Shield to block {enemyDamage} enemy damage! Remaining shields: {shieldCharges}</color>");
                return true;
            }

            actualDamageToNpc = enemyDamage;
            return false;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"<color=orange>[NpcAllyInstance] {DisplayName} took {amount} damage! Current HP: {currentHealth}/{maxHealth}</color>");

            if (statUI != null)
            {
                statUI.UpdateStats(currentHealth, maxHealth, damage);
                statUI.ShowDamagePopup(amount);
            }

            if (CombatFXController.Instance != null)
            {
                CombatFXController.Instance.TriggerHitReaction(gameObject, false);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            currentHealth = 0;
            EnemyInstance.OnEnemyKilled -= HandleEnemyKilled;
            if (ActiveAlly == this)
            {
                ActiveAlly = null;
            }

            if (statUI != null)
            {
                statUI.SetVisible(false);
            }

            if (shieldIndicatorObj != null)
            {
                shieldIndicatorObj.SetActive(false);
            }

            CatPowerUpController.TriggerNotification($"{DisplayName.ToUpper()} FALLEN!", new Color(1f, 0.45f, 0.45f));
            Debug.Log($"<color=red>[NpcAllyInstance] {DisplayName} has died! Subsequent enemy damage will now hit the Player.</color>");

            if (gameObject.activeInHierarchy && Application.isPlaying)
            {
                StartCoroutine(DeathBlinkAndDestroyRoutine());
            }
            else if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator DeathBlinkAndDestroyRoutine()
        {
            var mr = GetComponent<MeshRenderer>();
            float totalDuration = 1.2f;
            float blinkInterval = 0.12f;
            float elapsed = 0f;
            bool isMeshVisible = true;

            while (elapsed < totalDuration)
            {
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
                isMeshVisible = !isMeshVisible;

                if (mr != null)
                {
                    mr.enabled = isMeshVisible;
                }
            }

            Destroy(gameObject);
        }

        public Vector3 GetAttackOriginPosition()
        {
            return transform.position + new Vector3(0.25f, 1.5f, -0.2f);
        }

        public Vector3 GetHitPosition()
        {
            return transform.position + new Vector3(0f, 1.5f, -0.2f);
        }

        public void PlayAttackAnimation()
        {
            if (IsDead || skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null) return;

            if (skeletonAnimation.AnimationState == null)
            {
                skeletonAnimation.Initialize(false);
            }
            if (skeletonAnimation.AnimationState == null) return;

            var skelData = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
            if (skelData == null) return;

            // CatBoxing uses "Fight", CatGuardian uses "Attack"
            var atkAnim = skelData.FindAnimation("Fight")
                       ?? skelData.FindAnimation("Attack")
                       ?? skelData.FindAnimation("Shoot");

            if (atkAnim != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, atkAnim.Name, false);
                var idleAnim = skelData.FindAnimation("Idle") ?? skelData.FindAnimation("idle");
                if (idleAnim != null)
                {
                    skeletonAnimation.AnimationState.AddAnimation(0, idleAnim.Name, true, 0f);
                }
            }
        }

        public void PlayIdleAnimation()
        {
            if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null) return;

            if (skeletonAnimation.AnimationState == null)
            {
                skeletonAnimation.Initialize(false);
            }
            if (skeletonAnimation.AnimationState == null) return;

            var skelData = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
            if (skelData == null) return;

            var idleAnim = skelData.FindAnimation("Idle") ?? skelData.FindAnimation("idle");
            if (idleAnim != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, idleAnim.Name, true);
            }
        }

        private void UpdateShieldVisual()
        {
            if (this == null || !gameObject || !isRevealed || IsDead || shieldCharges <= 0)
            {
                if (shieldIndicatorObj != null)
                {
                    shieldIndicatorObj.SetActive(false);
                }
                return;
            }

            Sprite shieldSprite = Resources.Load<Sprite>("Icons/Icon_Shield");
#if UNITY_EDITOR
            if (shieldSprite == null)
            {
                shieldSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Shield.png");
            }
#endif

            if (shieldIndicatorObj == null)
            {
                shieldIndicatorObj = new GameObject("NpcShieldIndicator");
                shieldIndicatorObj.transform.SetParent(transform, false);

                var mr = GetComponent<MeshRenderer>();
                if (mr != null && mr.bounds.size.y > 0.1f)
                {
                    float localMidY = (mr.bounds.center.y - transform.position.y);
                    float localSideX = (mr.bounds.max.x - transform.position.x) + 0.32f;
                    shieldBaseLocalPos = new Vector3(Mathf.Max(0.75f, localSideX), Mathf.Clamp(localMidY, 1.1f, 1.7f), -0.15f);
                }
                else
                {
                    shieldBaseLocalPos = new Vector3(0.85f, 1.45f, -0.15f);
                }

                shieldIndicatorObj.transform.localPosition = shieldBaseLocalPos;
                shieldIndicatorObj.transform.localScale = shieldBaseScale;

                stackedShieldRenderers.Clear();
                for (int i = 0; i < MaxVisualShieldLayers; i++)
                {
                    var layerObj = new GameObject($"ShieldLayer_{i + 1}");
                    layerObj.transform.SetParent(shieldIndicatorObj.transform, false);
                    layerObj.transform.localPosition = new Vector3(i * 0.16f, 0.45f + (i * 0.08f), -i * 0.02f);
                    layerObj.transform.localScale = Vector3.one;

                    var sr = layerObj.AddComponent<SpriteRenderer>();
                    sr.sprite = shieldSprite;
                    sr.sortingLayerName = "Default";
                    sr.sortingOrder = 25 + i;
                    stackedShieldRenderers.Add(sr);
                }

                shieldCountBadgeObj = new GameObject("ShieldCountBadge");
                shieldCountBadgeObj.transform.SetParent(shieldIndicatorObj.transform, false);
                shieldCountBadgeObj.transform.localPosition = new Vector3(0.10f, -0.05f, -0.08f);

                var canvas = shieldCountBadgeObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingLayerName = "Default";
                canvas.sortingOrder = 30;

                var rt = shieldCountBadgeObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(48f, 22f);
                rt.localScale = new Vector3(0.015f, 0.015f, 1f);

                var bg = shieldCountBadgeObj.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0.05f, 0.1f, 0.18f, 0.92f);
                bg.raycastTarget = false;

                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(shieldCountBadgeObj.transform, false);
                var txtRT = txtObj.AddComponent<RectTransform>();
                txtRT.anchorMin = Vector2.zero;
                txtRT.anchorMax = Vector2.one;
                txtRT.offsetMin = Vector2.zero;
                txtRT.offsetMax = Vector2.zero;

                shieldCountBadgeText = txtObj.AddComponent<UnityEngine.UI.Text>();
                shieldCountBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                shieldCountBadgeText.fontSize = 18;
                shieldCountBadgeText.fontStyle = FontStyle.Bold;
                shieldCountBadgeText.alignment = TextAnchor.MiddleCenter;
                shieldCountBadgeText.color = new Color(0.2f, 0.9f, 1f);
                shieldCountBadgeText.raycastTarget = false;
            }

            int visibleLayers = Mathf.Clamp(shieldCharges, 1, MaxVisualShieldLayers);
            for (int i = 0; i < stackedShieldRenderers.Count; i++)
            {
                if (stackedShieldRenderers[i] != null)
                {
                    stackedShieldRenderers[i].gameObject.SetActive(i < visibleLayers);
                }
            }

            if (shieldCountBadgeObj != null && shieldCountBadgeText != null)
            {
                shieldCountBadgeObj.SetActive(shieldCharges >= 1);
                shieldCountBadgeText.text = $"x{shieldCharges}";
            }

            shieldIndicatorObj.SetActive(true);
        }
    }
}
