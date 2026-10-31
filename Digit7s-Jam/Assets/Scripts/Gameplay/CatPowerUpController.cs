using System;
using System.Collections;
using UnityEngine;
using Digit7s.Characters;

namespace Digit7s.Gameplay
{
    /// <summary>
    /// Handles character-specific combat abilities and HP scaling formulas for:
    /// - Cat Blaster (HP: 35, DMG: 2):
    ///   • 4 attacks -> 5th attack deals Damage x2 + heals HP based on Player's dealt damage (0-10 -> 1hp, 11-20 -> 2hp, 21-30 -> 3hp...) + grants Shield
    ///   • On Enemy Kill: Grants 1-time Damage x1.5 effect for next attack (does NOT reset attack count value! No Damage x2, no HP heal, no Shield)
    /// - Cat Brawler (HP: 30, DMG: 4):
    ///   • On Enemy Kill: Heals HP based on Enemy's Total HP (0-10 -> 1hp, 11-20 -> 2hp, 21-30 -> 3hp...)
    ///   • 5 attacks -> 6th attack heals HP based on Player's dealt damage (0-10 -> 1hp, 11-20 -> 2hp, 21-30 -> 3hp...) + grants Shield
    /// - Cat Guardian (HP: 40, DMG: 3):
    ///   • 6 attacks -> 7th attack blocks Enemy Damage (0 DMG taken) + heals HP based on Enemy's Damage (0-10 -> 1hp, 11-20 -> 2hp, 21-30 -> 3hp...)
    ///   • On Enemy Kill: Grants 1-time Damage Block shield (does NOT reset block count value!), displays a Shield icon in front of the player, and resets Shield on entering a new room.
    /// </summary>
    public class CatPowerUpController : MonoBehaviour
    {
        public static CatPowerUpController Instance { get; private set; }

        [Header("Character Data Reference")]
        [SerializeField] private CharacterData characterData;

        [Header("Class Configuration")]
        [SerializeField] private CatClassType catType = CatClassType.Blaster;

        [Header("Attack Counter State")]
        [Tooltip("Tracks current regular attacks done toward the next special trigger")]
        [SerializeField] private int currentAttackCount = 0;
        [SerializeField] private int attacksRequired = 4;
        [SerializeField] private bool guaranteedNextAttackCrit = false;
        [SerializeField] private int blasterFastComboCharges = 0;
        [SerializeField] private bool hasOneTimeShieldBlock = false;
        [SerializeField] private int shieldCharges = 0;

        public const int MaxShieldCharges = 3;
        public const int MaxFastComboCharges = 5;

        private bool isBlasterCritReady = false;
        private bool pendingGuardianBlock = false;
        private PlayerInstance playerInstance;
        private GameObject shieldIndicatorObj;
        private readonly System.Collections.Generic.List<SpriteRenderer> stackedShieldRenderers = new System.Collections.Generic.List<SpriteRenderer>();
        private UnityEngine.UI.Text shieldCountBadgeText;
        private GameObject shieldCountBadgeObj;
        private Vector3 shieldBaseLocalPos = new Vector3(-1.05f, 1.05f, -0.15f);
        private Vector3 shieldBaseScale = new Vector3(0.85f, 0.85f, 1f);

        public CharacterData Data => characterData;
        public CatClassType CatType => catType;
        public int CurrentAttackCount => currentAttackCount;
        public int AttacksRequired => attacksRequired;
        public int BlasterFastComboCharges => blasterFastComboCharges;
        public int BlasterBonusComboChances => blasterFastComboCharges;
        public int BlasterComboCharges => blasterFastComboCharges;
        public bool IsBlasterCritReady => isBlasterCritReady;
        public bool HasGuaranteedNextCrit => (catType == CatClassType.Blaster) && (isBlasterCritReady || currentAttackCount >= attacksRequired);
        public bool HasOneTimeShieldBlock => hasOneTimeShieldBlock || shieldCharges > 0;
        public int ShieldCharges => shieldCharges;

        public static event Action<string, Color> OnPowerUpNotification;

        public static void TriggerNotification(string message, Color color)
        {
            OnPowerUpNotification?.Invoke(message, color);
        }

        private void Awake()
        {
            Instance = this;
            playerInstance = GetComponent<PlayerInstance>();
        }

        private void OnEnable()
        {
            EnemyInstance.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            EnemyInstance.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (shieldIndicatorObj != null && shieldIndicatorObj.activeSelf)
            {
                float bobY = Mathf.Sin(Time.time * 3.5f) * 0.05f;
                float pulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.05f;
                shieldIndicatorObj.transform.localPosition = shieldBaseLocalPos + new Vector3(0f, bobY, 0f);
                shieldIndicatorObj.transform.localScale = shieldBaseScale * pulse;
            }
        }

        /// <summary>
        /// Formula helper:
        /// If stepSize == 10: 0-10 -> 1, 11-20 -> 2, 21-30 -> 3, etc. (Cat Blaster)
        /// If stepSize == 20: 0-20 -> 1, 21-40 -> 2, 41-60 -> 3, etc. (Cat Brawler, Cat Guardian)
        /// </summary>
        public static int GetBucketStep(int value, int stepSize = 20)
        {
            if (value <= 0) return 1;
            float step = stepSize > 0 ? (float)stepSize : 20f;
            return Mathf.Max(1, Mathf.CeilToInt(value / step));
        }

        public void Initialize(CharacterData data, CatClassType type, int savedAttackCounter = 0, int savedCritCharges = 0)
        {
            Instance = this;
            characterData = data;
            catType = type;
            playerInstance = GetComponent<PlayerInstance>();

            switch (catType)
            {
                case CatClassType.Blaster:
                    attacksRequired = (characterData != null && characterData.attacksRequiredForSpecial > 0)
                        ? characterData.attacksRequiredForSpecial
                        : 4;
                    break;
                case CatClassType.Brawler:
                    attacksRequired = (characterData != null && characterData.attacksRequiredForSpecial > 0)
                        ? characterData.attacksRequiredForSpecial
                        : 5;
                    break;
                case CatClassType.Guardian:
                    attacksRequired = (characterData != null && characterData.attacksRequiredForSpecial > 0)
                        ? characterData.attacksRequiredForSpecial
                        : 5;
                    break;
            }

            currentAttackCount = Mathf.Max(0, savedAttackCounter);
            blasterFastComboCharges = (catType == CatClassType.Blaster) ? Mathf.Max(0, savedCritCharges) : 0;
            guaranteedNextAttackCrit = false;
            // Shield resets on entering a new room as requested ("အသစ်အခန်းရောက်သွားရင် Shield Rest ချပါမယ်")
            shieldCharges = 0;
            hasOneTimeShieldBlock = false;
            pendingGuardianBlock = false;
            UpdateShieldVisual(false);
            EvaluateBlasterRandomCrit();

            Debug.Log($"<color=cyan>[CatPowerUp] Initialized {catType} with Attacks Required: {attacksRequired}, Current Counter: {currentAttackCount}, BlasterFastComboCharges: {blasterFastComboCharges}</color>");
        }

        /// <summary>
        /// Evaluates Cat Blaster's Random Damage x2 Fast Combo chance based on currentAttackCount:
        /// - 4 or 5 attacks: 100% (Damage x2)
        /// - 3 attacks: 1 in 2 (50%)
        /// - 2 attacks: 1 in 3 (33.3%)
        /// - 0 or 1 attacks: 1 in 4 (25%)
        /// If roll hits: isBlasterCritReady becomes true, and UI displays (Damage x2).
        /// </summary>
        public void EvaluateBlasterRandomCrit()
        {
            if (catType != CatClassType.Blaster)
            {
                isBlasterCritReady = false;
                return;
            }

            if (currentAttackCount >= 4)
            {
                // 4 or 5 attacks done -> 100% (Damage x2)
                isBlasterCritReady = true;
            }
            else if (blasterFastComboCharges > 0)
            {
                int maxExclusive = currentAttackCount switch
                {
                    3 => 2,
                    2 => 3,
                    _ => 4
                };
                isBlasterCritReady = (UnityEngine.Random.Range(0, maxExclusive) == 0);
            }
            else
            {
                isBlasterCritReady = false;
            }
        }

        public void Initialize(CharacterData data, CatClassType type, int savedAttackCounter, bool savedGuaranteedCrit)
        {
            Initialize(data, type, savedAttackCounter, savedGuaranteedCrit ? 1 : 0);
        }

        /// <summary>
        /// Grants 1-time Damage Block shield(s) (from Shield item pickup, Bonus Slot Machine, or Guardian on-kill passive)
        /// up to MaxShieldCharges (3) and displays the stacked Shield icon(s) in front of the player character.
        /// </summary>
        public void GrantOneTimeShield(int count = 1)
        {
            shieldCharges = Mathf.Clamp(Mathf.Max(0, shieldCharges) + Mathf.Max(1, count), 0, MaxShieldCharges);
            hasOneTimeShieldBlock = shieldCharges > 0;
            UpdateShieldVisual(hasOneTimeShieldBlock);
            SaveState();
        }

        public Vector3 GetShieldWorldPosition()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null && mr.bounds.size.y > 0.1f)
            {
                float localMidY = mr.bounds.size.y * 0.46f;
                float scaleX = Mathf.Abs(transform.localScale.x);
                if (scaleX < 0.01f) scaleX = 1f;
                float leftDistFromOrigin = (transform.position.x - mr.bounds.min.x) / scaleX;
                float localSideX = -(leftDistFromOrigin + 0.45f);
                shieldBaseLocalPos = new Vector3(Mathf.Min(-1.05f, localSideX), Mathf.Clamp(localMidY, 0.95f, 1.15f), -0.15f);
            }
            return transform.TransformPoint(shieldBaseLocalPos + new Vector3(0f, 0.6f, 0f));
        }

        /// <summary>
        /// Resets 1-time Shield (e.g. when entering a new room — shields cannot be carried over from the previous room).
        /// </summary>
        public void ResetOneTimeShield()
        {
            shieldCharges = 0;
            hasOneTimeShieldBlock = false;
            pendingGuardianBlock = false;
            UpdateShieldVisual(false);
        }

        private void UpdateShieldVisual(bool active)
        {
            if (!active || shieldCharges <= 0)
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
                shieldIndicatorObj = new GameObject("GuardianShieldIndicator");
                shieldIndicatorObj.transform.SetParent(transform, false);

                // Position on the left side of the player character at mid-height
                var mr = GetComponent<MeshRenderer>();
                if (mr != null && mr.bounds.size.y > 0.1f)
                {
                    float localMidY = mr.bounds.size.y * 0.46f;
                    float scaleX = Mathf.Abs(transform.localScale.x);
                    if (scaleX < 0.01f) scaleX = 1f;
                    float leftDistFromOrigin = (transform.position.x - mr.bounds.min.x) / scaleX;
                    float localSideX = -(leftDistFromOrigin + 0.45f);
                    shieldBaseLocalPos = new Vector3(Mathf.Min(-1.05f, localSideX), Mathf.Clamp(localMidY, 0.95f, 1.15f), -0.15f);
                }
                else
                {
                    shieldBaseLocalPos = new Vector3(-1.05f, 1.05f, -0.15f);
                }

                shieldIndicatorObj.transform.localPosition = shieldBaseLocalPos;
                shieldIndicatorObj.transform.localScale = shieldBaseScale;

                stackedShieldRenderers.Clear();
                for (int i = 0; i < MaxShieldCharges; i++)
                {
                    var layerObj = new GameObject($"ShieldLayer_{i + 1}");
                    layerObj.transform.SetParent(shieldIndicatorObj.transform, false);
                    layerObj.transform.localPosition = new Vector3(-i * 0.14f, 0.6f + (i * 0.08f), -i * 0.02f);
                    layerObj.transform.localScale = Vector3.one;

                    var sr = layerObj.AddComponent<SpriteRenderer>();
                    sr.sprite = shieldSprite;
                    sr.sortingLayerName = "Default";
                    sr.sortingOrder = 27 - i;
                    stackedShieldRenderers.Add(sr);
                }

                // Count badge below shield when charges > 1
                shieldCountBadgeObj = new GameObject("ShieldCountBadge");
                shieldCountBadgeObj.transform.SetParent(shieldIndicatorObj.transform, false);
                shieldCountBadgeObj.transform.localPosition = new Vector3(-0.07f, 0.05f, -0.08f);

                var canvas = shieldCountBadgeObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingLayerName = "Default";
                canvas.sortingOrder = 30;

                var rt = shieldCountBadgeObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(46f, 22f);
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

            int visibleCount = Mathf.Clamp(shieldCharges, 1, MaxShieldCharges);
            for (int i = 0; i < stackedShieldRenderers.Count; i++)
            {
                if (stackedShieldRenderers[i] != null)
                {
                    stackedShieldRenderers[i].gameObject.SetActive(i < visibleCount);
                }
            }

            if (shieldCountBadgeObj != null && shieldCountBadgeText != null)
            {
                shieldCountBadgeObj.SetActive(visibleCount > 1);
                shieldCountBadgeText.text = $"x{visibleCount}";
            }

            shieldIndicatorObj.SetActive(true);
        }

        /// <summary>
        /// Called when the player attacks an enemy during a combat clash.
        /// </summary>
        public int ProcessPlayerAttack(EnemyInstance enemy, int baseDamage, out bool isCrit, out bool isStun)
        {
            isCrit = false;
            isStun = false;
            pendingGuardianBlock = false;
            int dealtDamage = baseDamage;

            if (playerInstance == null)
            {
                playerInstance = GetComponent<PlayerInstance>();
            }

            int hpPerDamageStep = (characterData != null && characterData.procHpPerDamageStep > 0)
                ? characterData.procHpPerDamageStep
                : 1;

            switch (catType)
            {
                case CatClassType.Blaster:
                    // If isBlasterCritReady is true (or currentAttackCount >= 4), this attack deals Damage x2!
                    if (isBlasterCritReady || currentAttackCount >= 4)
                    {
                        isCrit = true;
                        dealtDamage = baseDamage * 2;

                        int hpBonus = GetBucketStep(dealtDamage, 10) * hpPerDamageStep;
                        playerInstance?.Heal(hpBonus);
                        GrantOneTimeShield(1);
                        Digit7s.UI.UISoundManager.PlayCatBlasterEffect();

                        if (blasterFastComboCharges > 0)
                        {
                            blasterFastComboCharges--;
                            Debug.Log($"<color=yellow>[CatBlaster] Fast Combo (Damage x2) triggered! Dealt {dealtDamage} DMG, +{hpBonus} HP, and Shield! (Remaining charges: {blasterFastComboCharges}/{MaxFastComboCharges}). Counter reset to 0.</color>");
                            OnPowerUpNotification?.Invoke($"FAST COMBO (DAMAGE x2)! (+{hpBonus} HP & SHIELD)", new Color(1f, 0.85f, 0.2f));
                        }
                        else
                        {
                            Debug.Log($"<color=yellow>[CatBlaster] 5th Attack triggered Damage x2 ({dealtDamage} DMG), +{hpBonus} HP, and Shield! Counter reset to 0.</color>");
                            OnPowerUpNotification?.Invoke($"DAMAGE x2! (+{hpBonus} HP & SHIELD)", new Color(1f, 0.85f, 0.2f));
                        }

                        currentAttackCount = 0;
                        EvaluateBlasterRandomCrit();
                    }
                    else
                    {
                        // Regular attack
                        currentAttackCount++;
                        Debug.Log($"<color=gray>[CatBlaster] Regular attack {currentAttackCount}/4 toward Damage x2.</color>");
                        EvaluateBlasterRandomCrit();
                    }
                    break;

                case CatClassType.Brawler:
                    // 5 regular attacks -> on the 6th attack, player's dealt damage is multiplied by 1.5, heal HP based on Player's dealt damage and grant Shield
                    currentAttackCount++;
                    if (currentAttackCount > attacksRequired)
                    {
                        currentAttackCount = 0;
                        isCrit = true;
                        dealtDamage = Mathf.CeilToInt(baseDamage * 1.5f);

                        int hpBonus = GetBucketStep(dealtDamage, 10) * hpPerDamageStep;
                        playerInstance?.Heal(hpBonus);
                        GrantOneTimeShield(1);
                        Digit7s.UI.UISoundManager.PlayCatBrawlerEffect();

                        Debug.Log($"<color=yellow>[CatBrawler] 6th Attack triggered Combo: Damage x1.5 ({dealtDamage} DMG), +{hpBonus} HP and Shield! Counter reset to 0.</color>");
                        OnPowerUpNotification?.Invoke($"COMBO (DAMAGE x1.5)! (+{hpBonus} HP & SHIELD)", new Color(1f, 0.85f, 0.2f));
                    }
                    else
                    {
                        Debug.Log($"<color=gray>[CatBrawler] Regular attack {currentAttackCount}/{attacksRequired}.</color>");
                    }
                    break;

                case CatClassType.Guardian:
                    // 5 regular attacks -> on the 6th attack, block Enemy Damage (0 DMG taken), grant 1 Shield, and heal HP based on Enemy Damage (0-10 -> 2hp, 11-20 -> 4hp, 21-30 -> 6hp)
                    currentAttackCount++;
                    if (currentAttackCount > attacksRequired)
                    {
                        currentAttackCount = 0;
                        pendingGuardianBlock = true;

                        int enemyDmg = enemy != null ? enemy.Damage : 1;
                        int guardianHpMultiplier = (characterData != null && characterData.procHpPerDamageStep > 0)
                            ? characterData.procHpPerDamageStep
                            : 2;
                        int hpBonus = GetBucketStep(enemyDmg, 10) * guardianHpMultiplier;
                        playerInstance?.Heal(hpBonus);
                        GrantOneTimeShield(1);
                        Digit7s.UI.UISoundManager.PlayCatGuardianEffect();

                        Debug.Log($"<color=cyan>[CatGuardian] 6th Attack triggered Damage Block, +1 Shield, & +{hpBonus} HP (from Enemy DMG {enemyDmg})! Counter reset to 0.</color>");
                        OnPowerUpNotification?.Invoke($"BLOCKED! (+{hpBonus} HP & SHIELD)", new Color(0.3f, 0.85f, 1f));
                    }
                    else
                    {
                        Debug.Log($"<color=gray>[CatGuardian] Attack {currentAttackCount}/{attacksRequired} toward next Block.</color>");
                    }
                    break;
            }

            SaveState();
            return dealtDamage;
        }

        /// <summary>
        /// Called when an enemy attempts to counter-attack the player.
        /// Evaluates Damage Block from either Guardian's combo/shield or a picked-up Shield item.
        /// </summary>
        public bool ProcessEnemyCounterAttack(EnemyInstance enemy, int enemyDamage, out int damageDealtToPlayer)
        {
            damageDealtToPlayer = enemyDamage;

            if (pendingGuardianBlock)
            {
                pendingGuardianBlock = false;
                damageDealtToPlayer = 0;
                SaveState();
                return true;
            }

            if (HasOneTimeShieldBlock)
            {
                shieldCharges = Mathf.Max(0, shieldCharges - 1);
                hasOneTimeShieldBlock = shieldCharges > 0;
                UpdateShieldVisual(hasOneTimeShieldBlock);
                damageDealtToPlayer = 0;
                Digit7s.UI.UISoundManager.PlayCatGuardianEffect();

                Debug.Log($"<color=cyan>[CatPowerUp] Consumed Shield Item Block against {enemy?.name}! Enemy damage ({enemyDamage}) reduced to 0.</color>");
                OnPowerUpNotification?.Invoke("SHIELD BLOCK! (0 DMG)", new Color(0.3f, 0.85f, 1f));
                SaveState();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when any enemy is killed.
        /// - Cat Blaster: Grants 1-time Damage x2 effect for next attack (does NOT reset attack count!)
        /// - Cat Brawler: Heals HP based on Enemy Total HP (0-10 -> 1hp, 11-20 -> 2hp, 21-30 -> 3hp...)
        /// - Cat Guardian: Grants 1-time Damage Block shield with Shield icon in front of player (does NOT reset block count!)
        /// </summary>
        private void HandleEnemyKilled(EnemyInstance enemy)
        {
            if (playerInstance == null)
            {
                playerInstance = GetComponent<PlayerInstance>();
            }
            if (playerInstance == null)
            {
                playerInstance = PlayerInstance.Instance;
            }

            switch (catType)
            {
                case CatClassType.Blaster:
                    // Enemy kill grants +2 Fast Combo stacks, stackable up to max 5!
                    // ("Cat Blaster , Enemy တကောင် kill လိုက်ရင် Fast Combo stack 2ခါပဲရမယ် (အများဆုံး stack 5ခါပဲနော်)")
                    blasterFastComboCharges = Mathf.Clamp(blasterFastComboCharges + 2, 0, MaxFastComboCharges);
                    Digit7s.UI.UISoundManager.PlayCatBlasterEffect();
                    EvaluateBlasterRandomCrit();
                    Debug.Log($"<color=green>[CatBlaster] Enemy killed! Gained +2 Fast Combos (Current charges: {blasterFastComboCharges}/{MaxFastComboCharges}). Crit ready: {isBlasterCritReady}.</color>");
                    OnPowerUpNotification?.Invoke($"KILL BONUS: FAST COMBO +2! ({blasterFastComboCharges}/{MaxFastComboCharges})", new Color(1f, 0.85f, 0.2f));
                    break;

                case CatClassType.Brawler:
                    int onKillEnemyHpStep = (characterData != null && characterData.onKillHpPerEnemyHpStep > 0)
                        ? characterData.onKillHpPerEnemyHpStep
                        : 1;
                    int enemyMaxHp = enemy != null ? enemy.MaxHealth : 10;
                    int healAmount = GetBucketStep(enemyMaxHp, 10) * onKillEnemyHpStep;
                    playerInstance?.Heal(healAmount);
                    // User: (Shield ရတာကိုယ်ဖြတ်ပေးပါ) - Shield removed on kill
                    Digit7s.UI.UISoundManager.PlayCatBrawlerEffect();

                    Debug.Log($"<color=green>[CatBrawler] Enemy killed! Gained +{healAmount} HP (from enemy total HP {enemyMaxHp}).</color>");
                    OnPowerUpNotification?.Invoke($"KILL HEAL: +{healAmount} HP", new Color(0.2f, 1f, 0.4f));
                    break;

                case CatClassType.Guardian:
                    // User: Enemy တကောင် kill လိုက်ရင် shield တခုရမယ်
                    GrantOneTimeShield(1);
                    Digit7s.UI.UISoundManager.PlayCatGuardianEffect();
                    Debug.Log($"<color=cyan>[CatGuardian] Enemy killed! Gained 1 Shield ({shieldCharges}/{MaxShieldCharges}). Block count preserved at {currentAttackCount}/{attacksRequired}.</color>");
                    OnPowerUpNotification?.Invoke($"SHIELD READY! ({shieldCharges}/{MaxShieldCharges} BLOCK)", new Color(0.3f, 0.85f, 1f));
                    break;
            }

            SaveState();
        }

        /// <summary>
        /// Resets all temporary room counters, chances, and shields when advancing to the next ladder room:
        /// - Cat Blaster, Cat Brawler, Cat Guardian attack counters reset to 0
        /// - Cat Blaster on-kill bonus combo chances reset to 0
        /// - Shields and pending damage blocks reset to 0
        /// ("Cat Blaster,Cat Brawler,Cat Guardian (အားလုံး ရေတွက်တယ် chance to အကုန် Rest ဖြစ်မယ်နော် (next ladder room ရောက်သွားရင်)")
        /// </summary>
        public void ResetAllRoomCounters()
        {
            currentAttackCount = 0;
            blasterFastComboCharges = 0;
            isBlasterCritReady = false;
            guaranteedNextAttackCrit = false;
            pendingGuardianBlock = false;
            ResetOneTimeShield();
        }

        /// <summary>
        /// Called on room transition: resets all counters, chances, and shields as requested.
        /// </summary>
        public void ApplyRoomLevelBonus(int roomLevel)
        {
            ResetAllRoomCounters();
            SaveState();
        }

        /// <summary>
        /// Converts each remaining Shield charge into 1 Coin at the ladder/end of the room:
        /// - Plays sparkling coin pickup SFX
        /// - Spawns animated floating Coin + Text effect at the shield position
        /// - Awards +1 Coin to the player
        /// - Decrements shield charges visually one-by-one
        /// ("Cat အားလုံ; Shield အခန်းအဆုံးမှာ ladder နောက်တခန်းရောက်သွားရင် Shield တခုကိုယ် = Coin 1 ပြန်ပြီးလဲပေးမယ် (coin exchange animation and sfx) ထည့်ပေးပါ")
        /// </summary>
        public IEnumerator ConvertShieldsToCoinsRoutine()
        {
            if (shieldCharges <= 0) yield break;

            int totalShields = shieldCharges;
            Sprite coinSprite = Resources.Load<Sprite>("Icons/Icon_Coin");
#if UNITY_EDITOR
            if (coinSprite == null)
            {
                coinSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Coin.png");
            }
#endif

            Vector3 basePos = GetShieldWorldPosition();

            for (int i = 0; i < totalShields; i++)
            {
                shieldCharges = Mathf.Max(0, shieldCharges - 1);
                hasOneTimeShieldBlock = shieldCharges > 0;
                UpdateShieldVisual(hasOneTimeShieldBlock);

                // Play Coin Pickup SFX
                Digit7s.UI.UISoundManager.PlayCoinPickup();

                // Award 1 coin to player
                if (playerInstance != null)
                {
                    playerInstance.AddCoins(1);
                }
                else if (PlayerInstance.Instance != null)
                {
                    PlayerInstance.Instance.AddCoins(1);
                }

                // Spawn floating/sparkling coin visual effect
                StartCoroutine(SpawnShieldToCoinFx(basePos, coinSprite, i + 1, totalShields));

                TriggerNotification($"SHIELD -> COIN! (+1 COIN)", new Color(1f, 0.88f, 0.2f));

                yield return new WaitForSeconds(0.24f);
            }

            yield return new WaitForSeconds(0.32f);

            TriggerNotification($"EXCHANGED {totalShields} SHIELD{(totalShields > 1 ? "S" : "")} FOR +{totalShields} COIN{(totalShields > 1 ? "S" : "")}!", new Color(1f, 0.85f, 0.2f));

            ResetOneTimeShield();
            SaveState();
        }

        private IEnumerator SpawnShieldToCoinFx(Vector3 spawnPos, Sprite coinSprite, int index, int total)
        {
            var coinObj = new GameObject($"CoinExchangeFx_{index}");
            coinObj.transform.position = spawnPos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0f, 0f);
            var sr = coinObj.AddComponent<SpriteRenderer>();
            sr.sprite = coinSprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 35;

            // Add +1 COIN floating text above coin
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(coinObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            var tm = textObj.AddComponent<TextMesh>();
            tm.text = "+1 COIN";
            tm.characterSize = 0.16f;
            tm.fontSize = 28;
            tm.color = new Color(1f, 0.88f, 0.2f, 1f);
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            var mr = textObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = 36;
            }

            // Animation: punch scale & float up arc
            Vector3 startScale = Vector3.zero;
            Vector3 punchScale = new Vector3(1.35f, 1.35f, 1f);
            Vector3 normalScale = new Vector3(1.05f, 1.05f, 1f);

            Vector3 startPos = coinObj.transform.position;
            Vector3 targetPos = startPos + new Vector3(0.15f * (index % 2 == 0 ? 1 : -1), 1.25f, 0f);

            float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (coinObj == null) yield break;

                // Smooth float up
                coinObj.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

                // Punch scale then settle
                if (t < 0.25f)
                {
                    coinObj.transform.localScale = Vector3.Lerp(startScale, punchScale, t / 0.25f);
                }
                else
                {
                    coinObj.transform.localScale = Vector3.Lerp(punchScale, normalScale, (t - 0.25f) / 0.75f);
                }

                // Fade out in last 30%
                if (t > 0.7f)
                {
                    float alpha = (1f - t) / 0.3f;
                    if (sr != null) sr.color = new Color(1f, 1f, 1f, alpha);
                    if (tm != null) tm.color = new Color(1f, 0.88f, 0.2f, alpha);
                }

                yield return null;
            }

            if (coinObj != null)
            {
                Destroy(coinObj);
            }
        }

        private void SaveState()
        {
            if (playerInstance != null)
            {
                PlayerInstance.SaveRunStats(
                    playerInstance.CurrentHealth,
                    playerInstance.MaxHealth,
                    playerInstance.Damage,
                    currentAttackCount,
                    blasterFastComboCharges
                );
            }
        }
    }
}
