using System.Collections;
using UnityEngine;
using Spine.Unity;
using Digit7s.Enemies;
using Digit7s.Characters;

namespace Digit7s.Gameplay
{
    [SelectionBase]
    public class EnemyInstance : MonoBehaviour
    {
        [Header("Runtime Rolled Stats")]
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private int roomLevel = 1;
        [SerializeField] private int levelBonus = 0;
        [SerializeField] private int currentHealth = 1;
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int damage = 1;

        [Header("Components")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private EnemyStatUI statUI;
        [SerializeField] private BoxCollider2D enemyCollider;

        private bool isAttacking = false;
        private bool isAttackable = true;
        private int lastAttackFrame = -1;

        public static event System.Action<EnemyInstance> OnEnemyKilled;

        public static void ClearStaticEvents()
        {
            OnEnemyKilled = null;
        }

        public static void RaiseEnemyKilled(EnemyInstance enemy)
        {
            if (OnEnemyKilled == null) return;

            var listeners = OnEnemyKilled.GetInvocationList();
            foreach (var d in listeners)
            {
                if (d.Target is UnityEngine.Object uObj && uObj == null)
                {
                    OnEnemyKilled -= (System.Action<EnemyInstance>)d;
                    continue;
                }
                try
                {
                    ((System.Action<EnemyInstance>)d).Invoke(enemy);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[EnemyInstance] OnEnemyKilled listener failed: {ex.Message}");
                }
            }
        }

        [Header("Stun State")]
        [SerializeField] private int stunRemainingClicks = 0;
        public bool IsStunned => stunRemainingClicks > 0 && !IsDead;
        public int StunRemainingClicks => stunRemainingClicks;

        private static readonly System.Collections.Generic.HashSet<EnemyInstance> activeEnemies = new System.Collections.Generic.HashSet<EnemyInstance>();
        public static bool LockBlocksDuringDeathAnimation = false;

        public static bool HasActiveEnemy
        {
            get
            {
                activeEnemies.RemoveWhere(e => e == null || !e.gameObject.activeInHierarchy || e.IsDead);
                return activeEnemies.Count > 0;
            }
        }

        public static int ActiveEnemyCount
        {
            get
            {
                activeEnemies.RemoveWhere(e => e == null || !e.gameObject.activeInHierarchy || e.IsDead);
                return activeEnemies.Count;
            }
        }

        public static int TotalActiveEnemyDamage
        {
            get
            {
                activeEnemies.RemoveWhere(e => e == null || !e.gameObject.activeInHierarchy || e.IsDead);
                int total = 0;
                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        total += enemy.Damage;
                    }
                }
                return total;
            }
        }

        public static void ClearAllActiveEnemies()
        {
            activeEnemies.Clear();
        }

        /// <summary>
        /// Checks if a block at the given position is locked because it is in the 8 directions (Up, Down, Left, Right, and 4 Diagonals)
        /// adjacent to any currently active enemy.
        /// </summary>
        public static bool IsBlockLocked(Vector3 blockPosition)
        {
            activeEnemies.RemoveWhere(e => e == null || !e.gameObject.activeInHierarchy || e.IsDead);
            if (activeEnemies.Count == 0) return false;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;

                Vector3 origin = enemy.SourceBlockPosition;
                float dx = Mathf.Abs(blockPosition.x - origin.x);
                float dy = Mathf.Abs(blockPosition.y - origin.y);

                // Check 8-direction adjacency (Up, Down, Left, Right, and 4 Diagonals) on the grid
                bool is8DirNeighbor = (dx <= 3.2f && dy <= 3.2f && (dx >= 1.4f || dy >= 1.4f));

                if (is8DirNeighbor)
                {
                    return true;
                }
            }

            return false;
        }

        [Header("Source Block Origin")]
        [SerializeField] private Vector3 sourceBlockPosition = Vector3.zero;
        private bool hasExplicitSourceBlockPosition = false;

        public Vector3 SourceBlockPosition
        {
            get
            {
                if (hasExplicitSourceBlockPosition) return sourceBlockPosition;
                // Fallback: estimate from spawn position using enemy spawn offset
                Vector3 spawnOffset = enemyData != null ? enemyData.GetSpawnOffset() : new Vector3(0f, -2.04f, 0f);
                return transform.position - spawnOffset;
            }
            set
            {
                sourceBlockPosition = value;
                hasExplicitSourceBlockPosition = true;
            }
        }

        /// <summary>
        /// Centers this enemy's visual body (MeshRenderer bounds) to match its source block position.
        /// </summary>
        public void CenterToBlock()
        {
            if (!hasExplicitSourceBlockPosition) return;

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Vector3 delta = sourceBlockPosition - mr.bounds.center;
                delta.z = 0f;
                transform.position += delta;
            }
        }

        /// <summary>
        /// Gets the visual center position of this enemy for combat FX and projectile impacts.
        /// </summary>
        public Vector3 GetHitPosition()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                return new Vector3(mr.bounds.center.x, mr.bounds.center.y, transform.position.z - 0.2f);
            }
            return transform.position + new Vector3(0f, 0.4f, -0.2f);
        }

        public EnemyData Data => enemyData;
        public int RoomLevel => roomLevel;
        public int LevelBonus => levelBonus;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int Damage => damage;
        public bool IsDead => currentHealth <= 0;
        public bool IsAttackable => isAttackable && !IsDead && !isAttacking;
        public EnemyStatUI StatUI => statUI;

        private void OnEnable()
        {
            if (!IsDead)
            {
                activeEnemies.Add(this);
            }
        }

        private void OnDisable()
        {
            activeEnemies.Remove(this);
        }

        private void OnDestroy()
        {
            activeEnemies.Remove(this);
        }

        private void Awake()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            EnsureCollider();
            EnsureRendererSorting();
        }

        public void Initialize(RolledEnemyStats stats, float attackDelay = 0f)
        {
            enemyData = stats.enemyData;
            roomLevel = stats.roomLevel;
            levelBonus = stats.levelBonus;
            currentHealth = stats.health;
            maxHealth = Mathf.Max(1, stats.maxHealth);
            damage = stats.damage;

            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            CenterToBlock();
            EnsureCollider();
            EnsureRendererSorting();
            SetupStatUI();

            PlayAnimation(enemyData != null ? enemyData.idleAnimation : "Idle", true);

            if (!IsDead && gameObject.activeInHierarchy)
            {
                activeEnemies.Add(this);
            }

            if (attackDelay > 0f && Application.isPlaying)
            {
                isAttackable = false;
                if (enemyCollider != null) enemyCollider.enabled = false;
                if (statUI != null) statUI.SetVisible(false);
                StartCoroutine(EnableAttackAfterExplosionRoutine(attackDelay));
            }
            else
            {
                isAttackable = true;
                if (enemyCollider != null) enemyCollider.enabled = true;
                if (statUI != null) statUI.SetVisible(true);
            }
        }

        /// <summary>
        /// Called when this pre-spawned hidden enemy is revealed after breaking a block.
        /// </summary>
        public void OnRevealedFromBlock(float attackDelay = 0.65f)
        {
            gameObject.SetActive(true);
            CenterToBlock();
            if (!IsDead)
            {
                activeEnemies.Add(this);
            }
            EnsureCollider();
            EnsureRendererSorting();
            SetupStatUI();
            PlayAnimation(enemyData != null ? enemyData.idleAnimation : "Idle", true);

            if (attackDelay > 0f)
            {
                isAttackable = false;
                if (enemyCollider != null) enemyCollider.enabled = false;
                if (statUI != null) statUI.SetVisible(false);

                if (Application.isPlaying)
                {
                    StartCoroutine(EnableAttackAfterExplosionRoutine(attackDelay));
                }
            }
            else
            {
                isAttackable = true;
                if (enemyCollider != null) enemyCollider.enabled = true;
                if (statUI != null) statUI.SetVisible(true);
            }
        }

        private IEnumerator EnableAttackAfterExplosionRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!IsDead)
            {
                isAttackable = true;
                if (enemyCollider != null)
                {
                    enemyCollider.enabled = true;
                }
                if (statUI != null)
                {
                    statUI.SetVisible(true);
                    statUI.AlignPosition();
                }
            }
        }

        private void EnsureRendererSorting()
        {
            var sa = GetComponent<SkeletonAnimation>();
            if (sa != null && sa.Skeleton != null)
            {
                sa.Skeleton.SetColor(Color.white);
            }

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = 10;
                if (Application.isPlaying && mr.material != null)
                {
                    mr.material.color = Color.white;
                }
            }
        }

        private void EnsureCollider()
        {
            if (enemyCollider == null)
            {
                enemyCollider = GetComponent<BoxCollider2D>();
            }

            if (enemyCollider == null)
            {
                enemyCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            // Auto-size collider to match the enemy spine mesh
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                float localCenterY = mr.bounds.center.y - transform.position.y;
                float localCenterX = mr.bounds.center.x - transform.position.x;
                float width = Mathf.Max(1.2f, mr.bounds.size.x);
                float height = Mathf.Max(1.2f, mr.bounds.size.y);

                enemyCollider.offset = new Vector2(localCenterX, localCenterY);
                enemyCollider.size = new Vector2(width, height);
            }
            else
            {
                enemyCollider.offset = new Vector2(0f, 2.0f);
                enemyCollider.size = new Vector2(1.3f, 1.3f);
            }

            enemyCollider.enabled = true;
        }

        private void SetupStatUI()
        {
            if (statUI == null)
            {
                var uiObj = new GameObject("StatUI");
                uiObj.transform.SetParent(transform, false);
                statUI = uiObj.AddComponent<EnemyStatUI>();
            }

            statUI.Initialize(currentHealth, maxHealth, damage);
        }

        /// <summary>
        /// Called when the player clicks/taps on this enemy.
        /// Triggers attack animation on both player and enemy, exchanges damage.
        /// </summary>
        public void HandlePlayerAttack()
        {
            if (Time.frameCount == lastAttackFrame) return;
            lastAttackFrame = Time.frameCount;

            if (!IsAttackable) return;
            if (PlayerInstance.Instance == null || PlayerInstance.Instance.IsDead) return;

            StartCoroutine(CombatClashRoutine());
        }

        public void ApplyStun(int clicks = 3)
        {
            if (IsDead) return;
            stunRemainingClicks = Mathf.Max(stunRemainingClicks, clicks);
            Debug.Log($"<color=yellow>[EnemyInstance] {name} is STUNNED for {stunRemainingClicks} block clicks!</color>");
            if (statUI != null)
            {
                statUI.ShowStatusPopup($"STUNNED ({stunRemainingClicks})", Color.yellow);
                statUI.SetStunned(stunRemainingClicks);
            }
            if (gameObject.activeInHierarchy && Application.isPlaying)
            {
                StartCoroutine(StunTintRoutine());
            }
        }

        public void DecrementStun()
        {
            if (stunRemainingClicks <= 0 || IsDead) return;
            stunRemainingClicks--;
            Debug.Log($"<color=yellow>[EnemyInstance] {name} stun remaining clicks: {stunRemainingClicks}</color>");
            if (statUI != null)
            {
                if (stunRemainingClicks > 0)
                {
                    statUI.SetStunned(stunRemainingClicks);
                }
                else
                {
                    statUI.ClearStun(currentHealth, maxHealth, damage);
                }
            }
        }

        private IEnumerator StunTintRoutine()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
            {
                while (IsStunned)
                {
                    float pulse = 0.7f + Mathf.PingPong(Time.time * 3f, 0.3f);
                    mr.material.color = new Color(1f, 1f, 0.3f * pulse, 1f);
                    yield return null;
                }
                if (mr != null && mr.material != null)
                {
                    mr.material.color = Color.white;
                }
            }
        }

        private IEnumerator CombatClashRoutine()
        {
            isAttacking = true;

            // 1. Player calculates damage and powerups
            CatClassType catType = CatPowerUpController.Instance != null ? CatPowerUpController.Instance.CatType : CatClassType.Blaster;
            int playerDmg = PlayerInstance.Instance != null ? PlayerInstance.Instance.Damage : 3;
            bool isCrit = false;
            bool isStun = false;

            if (CatPowerUpController.Instance != null)
            {
                playerDmg = CatPowerUpController.Instance.ProcessPlayerAttack(this, playerDmg, out isCrit, out isStun);
            }

            NpcAllyInstance activeAlly = NpcAllyInstance.GetActiveAliveAlly();
            int npcDmg = activeAlly != null ? activeAlly.Damage : 0;
            int totalAttackDmg = playerDmg + npcDmg;

            Vector3 playerPos = PlayerInstance.Instance != null ? PlayerInstance.Instance.transform.position : new Vector3(-3.1f, -10.1f, 0f);
            Vector3 weaponPos = PlayerInstance.Instance != null
                ? PlayerInstance.Instance.GetWeaponMuzzlePosition()
                : playerPos + new Vector3(0.54f, 2.07f, -0.2f);
            Vector3 enemyHitPos = GetHitPosition();

            if (PlayerInstance.Instance != null)
            {
                PlayerInstance.Instance.PlayShootAnimation();
            }

            // If NPC Ally is active, play its attack animation and launch its projectile alongside the player's attack
            if (activeAlly != null)
            {
                activeAlly.PlayAttackAnimation();
                CatClassType npcFxType = activeAlly.AllyType == NpcAllyType.CatBoxing ? CatClassType.Brawler : CatClassType.Guardian;
                CombatFXController.Instance.PlayPlayerAttack(activeAlly.GetAttackOriginPosition(), enemyHitPos, npcFxType, false, null);
            }

            // Launch Player Attack Projectile & FX (deals Player Damage + NPC Cat Damage combined)
            bool attackLanded = false;
            CombatFXController.Instance.PlayPlayerAttack(weaponPos, enemyHitPos, catType, isCrit, () =>
            {
                attackLanded = true;
                TakeDamage(totalAttackDmg);
            });

            // Wait for attack projectile impact
            while (!attackLanded)
            {
                yield return null;
            }

            // 2. Enemy Counter-Attack (hits NPC Cat first if alive; once NPC Cat dies, hits Player)
            if (PlayerInstance.Instance != null && !IsDead)
            {
                string atkAnim = (enemyData != null && !string.IsNullOrEmpty(enemyData.attackAnimation)) ? enemyData.attackAnimation : "Attack";
                PlayAnimation(atkAnim, false);

                int counterDmg = damage;
                bool isBlocked = false;

                activeAlly = NpcAllyInstance.GetActiveAliveAlly();
                if (activeAlly != null)
                {
                    // NPC Cat takes damage first ("NPC Cat Damage အရင်ထိပြီးသူသေတော့ player ရှိ Damage ဝင်ပါမယ်")
                    isBlocked = activeAlly.ProcessIncomingEnemyAttack(counterDmg, out int actualNpcDmg);
                    counterDmg = actualNpcDmg;

                    bool counterLanded = false;
                    Vector3 npcHitPos = activeAlly.GetHitPosition();
                    CombatFXController.Instance.PlayEnemyCounterAttack(enemyHitPos, npcHitPos, isBlocked, () =>
                    {
                        counterLanded = true;
                        if (counterDmg > 0 && activeAlly != null)
                        {
                            activeAlly.TakeDamage(counterDmg);
                        }
                        else if (isBlocked && activeAlly != null && activeAlly.StatUI != null)
                        {
                            activeAlly.StatUI.ShowStatusPopup("BLOCKED!", new Color(0.3f, 0.85f, 1f));
                        }
                    });

                    while (!counterLanded)
                    {
                        yield return null;
                    }
                }
                else
                {
                    // NPC Cat is not present or has died -> damage enters Player
                    if (CatPowerUpController.Instance != null)
                    {
                        isBlocked = CatPowerUpController.Instance.ProcessEnemyCounterAttack(this, counterDmg, out int actualDamage);
                        counterDmg = actualDamage;
                    }

                    bool counterLanded = false;
                    Vector3 playerHitPos = playerPos + new Vector3(0f, 1.2f, -0.2f);
                    CombatFXController.Instance.PlayEnemyCounterAttack(enemyHitPos, playerHitPos, isBlocked, () =>
                    {
                        counterLanded = true;
                        if (counterDmg > 0)
                        {
                            PlayerInstance.Instance.TakeDamage(counterDmg);
                        }
                        else if (isBlocked && statUI != null)
                        {
                            statUI.ShowStatusPopup("BLOCKED!", new Color(0.3f, 0.85f, 1f));
                        }
                    });

                    while (!counterLanded)
                    {
                        yield return null;
                    }
                }
            }

            // Wait brief cooldown
            yield return new WaitForSeconds(0.25f);

            if (!IsDead)
            {
                string idleAnim = (enemyData != null && !string.IsNullOrEmpty(enemyData.idleAnimation)) ? enemyData.idleAnimation : "Idle";
                PlayAnimation(idleAnim, true);
            }

            isAttacking = false;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"[{name}] Took {amount} damage. Current HP: {currentHealth}/{maxHealth}");

            if (statUI != null)
            {
                statUI.UpdateStats(currentHealth, maxHealth, damage);
                statUI.ShowDamagePopup(amount);
            }

            CombatFXController.Instance.TriggerHitReaction(gameObject, true);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            currentHealth = 0;
            activeEnemies.Remove(this);
            RaiseEnemyKilled(this);

            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }

            if (statUI != null)
            {
                statUI.SetVisible(false);
            }

            // Immediately unlock and refresh all surrounding block visuals upon enemy death
            DestructibleBlock.UpdateAllBlocksVisuals();

            string anim = (enemyData != null && !string.IsNullOrEmpty(enemyData.deadAnimation)) ? enemyData.deadAnimation : "Dead";
            PlayAnimation(anim, false);
            Debug.Log($"[{name}] has died! Playing Dead animation, toggling MeshRenderer (0.15s blink) for 2s then destroying.");

            if (gameObject.activeInHierarchy && Application.isPlaying)
            {
                StartCoroutine(DeathBlinkAndDestroyRoutine());
            }
        }

        private IEnumerator DeathBlinkAndDestroyRoutine()
        {
            var mr = GetComponent<MeshRenderer>();
            float totalDuration = 2.0f;
            float blinkInterval = 0.15f;
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

        public void PlayAnimation(string animName, bool loop)
        {
            if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null || string.IsNullOrEmpty(animName)) return;

            if (skeletonAnimation.AnimationState == null)
            {
                skeletonAnimation.Initialize(false);
            }

            if (skeletonAnimation.AnimationState == null) return;

            var skelData = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
            if (skelData == null) return;

            var anim = skelData.FindAnimation(animName);
            if (anim != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, anim.Name, loop);
            }
        }

        private void OnMouseUpAsButton()
        {
            if (IsAttackable)
            {
                HandlePlayerAttack();
            }
        }
    }
}

