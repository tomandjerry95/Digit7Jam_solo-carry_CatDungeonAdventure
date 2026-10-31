using System;
using System.Collections;
using UnityEngine;
using Spine.Unity;

namespace Digit7s.Gameplay
{
    [SelectionBase]
    public class PlayerInstance : MonoBehaviour
    {
        public static PlayerInstance Instance { get; private set; }

        public static int SavedCurrentHealth { get; private set; } = 35;
        public static int SavedMaxHealth { get; private set; } = 35;
        public static int SavedDamage { get; private set; } = 2;
        public static int SavedAttackCounter { get; private set; } = 0;
        public static int SavedCritCharges { get; private set; } = 0;
        public static bool SavedGuaranteedCrit => SavedCritCharges > 0;
        public static int SavedCoins { get; private set; } = 0;
        public static int LastAppliedRoomBonusLevel { get; set; } = 0;
        public static bool HasSavedRunStats { get; private set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStaticState()
        {
            ResetRunStats(35, 2);
        }

        public static void SaveRunStats(int currentHp, int maxHp, int dmg, int attackCounter = 0, bool guaranteedCrit = false)
        {
            SaveRunStats(currentHp, maxHp, dmg, attackCounter, guaranteedCrit ? 1 : 0);
        }

        public static void SaveRunStats(int currentHp, int maxHp, int dmg, int attackCounter, int critCharges)
        {
            SavedCurrentHealth = Mathf.Max(0, currentHp);
            SavedMaxHealth = Mathf.Max(1, maxHp);
            SavedDamage = Mathf.Max(1, dmg);
            SavedAttackCounter = attackCounter;
            SavedCritCharges = Mathf.Max(0, critCharges);
            if (Instance != null)
            {
                SavedCoins = Instance.CurrentCoins;
            }
            HasSavedRunStats = true;
        }

        public static void ResetRunStats(int defaultHp = 35, int defaultDmg = 2)
        {
            SavedCurrentHealth = defaultHp;
            SavedMaxHealth = defaultHp;
            SavedDamage = defaultDmg;
            SavedAttackCounter = 0;
            SavedCritCharges = 0;
            SavedCoins = 0;
            LastAppliedRoomBonusLevel = 0;
            HasSavedRunStats = false;
            EnemySpawner.PendingBonusSlotSpin = false;
            EnemySpawner.HasLuckDropBonus = false;
            EnemySpawner.ResetNpcSpawnState();
            SetHasKey(false);
        }

        public static bool HasKey { get; private set; } = false;

        public static void SetHasKey(bool hasKey)
        {
            HasKey = hasKey;
            OnKeyChanged?.Invoke(HasKey);
        }

        public static event Action<int, int> OnHealthChanged;
        public static event Action<bool> OnKeyChanged;
        public static event Action<int> OnCoinsChanged;
        public static event Action OnPlayerDied;

        [Header("Stats")]
        [SerializeField] private int maxHealth = 35;
        [SerializeField] private int currentHealth = 35;
        [SerializeField] private int damage = 2;
        [SerializeField] private int currentCoins = 0;

        [Header("Components & Prefabs")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private GameObject shootFxPrefab;
        [SerializeField] private SkeletonDataAsset shootFxSkeletonData;

        private GameObject shootFxChildObj;
        private SkeletonAnimation shootFxChildAnim;
        private MeshRenderer shootFxChildRenderer;
        private Coroutine hideShootFxCoroutine;
        private bool isDead = false;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int Damage => damage;
        public int CurrentCoins => currentCoins;
        public bool IsDead => isDead;

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            currentCoins += amount;
            SavedCoins = currentCoins;
            Debug.Log($"<color=yellow>[PlayerInstance] Gained +{amount} coins! Total: {currentCoins}</color>");
            OnCoinsChanged?.Invoke(currentCoins);
        }

        public bool TryConsumeCoins(int amount)
        {
            if (currentCoins < amount) return false;
            currentCoins -= amount;
            SavedCoins = currentCoins;
            Debug.Log($"<color=yellow>[PlayerInstance] Consumed {amount} coins. Remaining: {currentCoins}</color>");
            OnCoinsChanged?.Invoke(currentCoins);
            return true;
        }

        private void Awake()
        {
            Instance = this;

            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            if (shootFxPrefab == null)
            {
                shootFxPrefab = Resources.Load<GameObject>("Fx2");
#if UNITY_EDITOR
                if (shootFxPrefab == null)
                {
                    shootFxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spine Characters/ShootFx/Fx2.prefab");
                }
#endif
            }

            if (shootFxSkeletonData == null)
            {
                shootFxSkeletonData = Resources.Load<SkeletonDataAsset>("Fx2_SkeletonData");
#if UNITY_EDITOR
                if (shootFxSkeletonData == null)
                {
                    shootFxSkeletonData = UnityEditor.AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Json Atlas/ShootFx/Fx2_SkeletonData.asset");
                }
#endif
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(int hp = 25, int dmg = 1, int curHp = -1)
        {
            Instance = this;
            maxHealth = Mathf.Max(1, hp);
            if (curHp > 0)
            {
                currentHealth = curHp;
                if (currentHealth > maxHealth) maxHealth = currentHealth;
            }
            else
            {
                currentHealth = maxHealth;
            }

            damage = Mathf.Max(1, dmg);
            currentCoins = SavedCoins;
            isDead = currentHealth <= 0;

            SaveRunStats(currentHealth, maxHealth, damage, SavedAttackCounter, SavedGuaranteedCrit);

            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }
            if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
            {
                skeletonAnimation.Skeleton.SetColor(Color.white);
            }

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = 10;
                if (mr.material != null)
                {
                    mr.material.color = Color.white;
                }
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnKeyChanged?.Invoke(HasKey);
            OnCoinsChanged?.Invoke(currentCoins);
            PlayIdleAnimation();
            EnsureShootFxAtGunMuzzle();
        }

        /// <summary>
        /// Calculates the exact local position of the front tip of the Cat's Gun barrel
        /// directly from the Spine "Gun" slot mesh vertices so it matches all 15 Cat skins.
        /// </summary>
        public Vector3 GetLocalWeaponMuzzlePosition()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
            {
                var mf = GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var verts = mf.sharedMesh.vertices;
                    int slotIdx = 0;
                    bool foundGun = false;
                    foreach (var slot in skeletonAnimation.Skeleton.Slots)
                    {
                        if (slot != null && slot.Data != null && slot.Data.Name == "Gun")
                        {
                            foundGun = true;
                            break;
                        }
                        slotIdx++;
                    }

                    int baseV = slotIdx * 4;
                    if (foundGun && verts != null && verts.Length >= baseV + 4)
                    {
                        float maxX = Mathf.Max(verts[baseV].x, verts[baseV + 1].x, verts[baseV + 2].x, verts[baseV + 3].x);
                        float minY = Mathf.Min(verts[baseV].y, verts[baseV + 1].y, verts[baseV + 2].y, verts[baseV + 3].y);
                        float maxY = Mathf.Max(verts[baseV].y, verts[baseV + 1].y, verts[baseV + 2].y, verts[baseV + 3].y);
                        float barrelY = Mathf.Lerp(minY, maxY, 0.62f);
                        return new Vector3(maxX + 0.02f, barrelY, -0.1f);
                    }
                }
            }

            return new Vector3(0.36f, 1.38f, -0.1f);
        }

        /// <summary>
        /// Returns the exact world position right in front of the Player Cat's Gun barrel.
        /// </summary>
        public Vector3 GetWeaponMuzzlePosition()
        {
            return transform.TransformPoint(GetLocalWeaponMuzzlePosition());
        }

        /// <summary>
        /// Returns the exact center position of the player character (in world space),
        /// dynamically computed from the Spine mesh renderer bounds.
        /// </summary>
        public Vector3 GetPlayerCenterPosition()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null && mr.bounds.size.y > 0.1f)
            {
                Vector3 center = mr.bounds.center;
                center.z = transform.position.z - 0.2f;
                return center;
            }

            return transform.position + new Vector3(0f, 2.36f, -0.2f);
        }

        /// <summary>
        /// Ensures Fx2 (with Fx2_SkeletonData) is placed right in front of the Player Cat's Gun
        /// at the exact same size/scale as the Player Cat.
        /// </summary>
        private void EnsureShootFxAtGunMuzzle()
        {
            if (shootFxPrefab == null)
            {
                shootFxPrefab = Resources.Load<GameObject>("Fx2");
#if UNITY_EDITOR
                if (shootFxPrefab == null)
                {
                    shootFxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spine Characters/ShootFx/Fx2.prefab");
                }
#endif
            }

            if (shootFxSkeletonData == null)
            {
                shootFxSkeletonData = Resources.Load<SkeletonDataAsset>("Fx2_SkeletonData");
#if UNITY_EDITOR
                if (shootFxSkeletonData == null)
                {
                    shootFxSkeletonData = UnityEditor.AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Json Atlas/ShootFx/Fx2_SkeletonData.asset");
                }
#endif
            }

            if (shootFxChildObj == null)
            {
                Transform existing = transform.Find("Fx2");
                if (existing != null)
                {
                    shootFxChildObj = existing.gameObject;
                }
                else if (shootFxPrefab != null)
                {
                    shootFxChildObj = Instantiate(shootFxPrefab, transform);
                    shootFxChildObj.name = "Fx2";
                }
                else if (shootFxSkeletonData != null)
                {
                    var fxGo = new GameObject("Fx2");
                    fxGo.transform.SetParent(transform, false);
                    var sr = fxGo.AddComponent<SkeletonRenderer>();
                    sr.skeletonDataAsset = shootFxSkeletonData;
                    shootFxChildAnim = fxGo.AddComponent<SkeletonAnimation>();
                    shootFxChildObj = fxGo;
                }
            }

            if (shootFxChildObj == null) return;

            shootFxChildObj.transform.localPosition = GetLocalWeaponMuzzlePosition();
            shootFxChildObj.transform.localRotation = Quaternion.identity;
            shootFxChildObj.transform.localScale = Vector3.one; // Inherits Player Cat's exact 1.5x scale ("size အတူတူ")

            if (shootFxChildAnim == null)
            {
                shootFxChildAnim = shootFxChildObj.GetComponent<SkeletonAnimation>();
            }
            if (shootFxChildAnim != null)
            {
                if (shootFxSkeletonData != null && shootFxChildAnim.SkeletonDataAsset == null)
                {
                    shootFxChildAnim.skeletonDataAsset = shootFxSkeletonData;
                }
                shootFxChildAnim.loop = false;
                shootFxChildAnim.Initialize(false);
            }

            if (shootFxChildRenderer == null)
            {
                shootFxChildRenderer = shootFxChildObj.GetComponent<MeshRenderer>();
            }
            if (shootFxChildRenderer != null)
            {
                shootFxChildRenderer.sortingLayerName = "Default";
                shootFxChildRenderer.sortingOrder = 25;
                shootFxChildRenderer.enabled = false;
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || isDead) return;
            currentHealth += amount;
            if (currentHealth > maxHealth)
            {
                maxHealth = currentHealth;
            }
            Debug.Log($"<color=green>[Player] Healed +{amount} HP! Current HP: {currentHealth}/{maxHealth}</color>");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            SaveRunStats(currentHealth, maxHealth, damage, SavedAttackCounter, SavedGuaranteedCrit);
        }

        public void IncreaseMaxAndCurrentHealth(int amount)
        {
            if (amount <= 0 || isDead) return;
            maxHealth += amount;
            currentHealth += amount;
            Debug.Log($"<color=green>[Player] Max & Current HP increased by +{amount}! Current HP: {currentHealth}/{maxHealth}</color>");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            SaveRunStats(currentHealth, maxHealth, damage, SavedAttackCounter, SavedGuaranteedCrit);
        }

        public void IncreaseDamage(int amount)
        {
            if (amount <= 0) return;
            damage += amount;
            Debug.Log($"<color=yellow>[Player] Damage permanently increased by +{amount}! Current DMG: {damage}</color>");
            SaveRunStats(currentHealth, maxHealth, damage, SavedAttackCounter, SavedGuaranteedCrit);
        }

        public void PlayShootAnimation()
        {
            if (isDead || skeletonAnimation == null) return;

            if (skeletonAnimation.AnimationState == null)
            {
                skeletonAnimation.Initialize(false);
            }

            if (skeletonAnimation.AnimationState == null) return;

            var skelData = skeletonAnimation.SkeletonDataAsset != null ? skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false) : null;
            if (skelData == null) return;

            var shootAnim = skelData.FindAnimation("Shoot") ?? skelData.FindAnimation("shoot") ?? skelData.FindAnimation("Attack");
            if (shootAnim != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, shootAnim.Name, false);
                skeletonAnimation.AnimationState.AddAnimation(0, "Idle", true, 0f);
            }

            // Trigger Fx2_SkeletonData right in front of the Player Cat's Gun with matching scale
            EnsureShootFxAtGunMuzzle();
            if (shootFxChildObj != null && shootFxChildAnim != null)
            {
                shootFxChildObj.transform.localPosition = GetLocalWeaponMuzzlePosition();
                shootFxChildObj.transform.localScale = Vector3.one;

                if (shootFxChildRenderer != null)
                {
                    shootFxChildRenderer.sortingLayerName = "Default";
                    shootFxChildRenderer.sortingOrder = 25;
                    shootFxChildRenderer.enabled = true;
                }

                if (shootFxChildAnim.AnimationState == null)
                {
                    shootFxChildAnim.Initialize(false);
                }

                if (shootFxChildAnim.AnimationState != null)
                {
                    var fxSkel = shootFxChildAnim.SkeletonDataAsset != null ? shootFxChildAnim.SkeletonDataAsset.GetSkeletonData(false) : null;
                    string fxAnimName = (fxSkel != null && fxSkel.FindAnimation("animation") != null) ? "animation" : "animation";
                    var track = shootFxChildAnim.AnimationState.SetAnimation(0, fxAnimName, false);
                    if (track != null)
                    {
                        track.Loop = false;
                    }
                }

                if (hideShootFxCoroutine != null)
                {
                    StopCoroutine(hideShootFxCoroutine);
                }
                hideShootFxCoroutine = StartCoroutine(HideShootFxAfterDelay(0.5f));
            }
        }

        private IEnumerator HideShootFxAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (shootFxChildRenderer != null)
            {
                shootFxChildRenderer.enabled = false;
            }
            hideShootFxCoroutine = null;
        }

        public void PlayIdleAnimation()
        {
            if (skeletonAnimation == null) return;

            if (skeletonAnimation.AnimationState == null)
            {
                skeletonAnimation.Initialize(false);
            }

            if (skeletonAnimation.AnimationState == null) return;

            var skelData = skeletonAnimation.SkeletonDataAsset != null ? skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false) : null;
            if (skelData == null) return;

            var idleAnim = skelData.FindAnimation("Idle") ?? skelData.FindAnimation("idle");
            if (idleAnim != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, idleAnim.Name, true);
            }
        }

        public void TakeDamage(int amount)
        {
            if (isDead || amount <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"<color=orange>[Player] Took {amount} damage to HP! Current HP: {currentHealth}/{maxHealth}</color>");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Red flash feedback
            if (gameObject.activeInHierarchy && Application.isPlaying)
            {
                StartCoroutine(DamageFlashRoutine());
            }

            if (currentHealth <= 0)
            {
                Die();
            }

            SaveRunStats(currentHealth, maxHealth, damage, SavedAttackCounter, SavedGuaranteedCrit);
        }

        private IEnumerator DamageFlashRoutine()
        {
            if (CombatFXController.Instance != null)
            {
                CombatFXController.Instance.TriggerHitReaction(gameObject, false);
            }
            else
            {
                var sa = GetComponent<SkeletonAnimation>();
                if (sa != null && sa.Skeleton != null)
                {
                    sa.Skeleton.SetColor(new Color(2.0f, 0.35f, 0.35f, 1.0f));
                    yield return new WaitForSeconds(0.08f);
                    if (sa != null && sa.Skeleton != null)
                    {
                        sa.Skeleton.SetColor(Color.white);
                    }
                }
            }
            yield break;
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;
            currentHealth = 0;
            SaveRunStats(0, maxHealth, damage, SavedAttackCounter, SavedGuaranteedCrit);
            Debug.Log("<color=red>[Player] Player HP is 0! Player died. Triggering Game Over...</color>");

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnPlayerDied?.Invoke();
        }
    }
}
