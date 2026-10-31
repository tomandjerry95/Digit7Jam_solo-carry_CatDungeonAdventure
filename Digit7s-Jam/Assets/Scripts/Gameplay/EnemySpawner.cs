using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Digit7s.Enemies;
using Digit7s.UI;

namespace Digit7s.Gameplay
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Database & Collection")]
        [Tooltip("The Regular Enemies collection to draw from")]
        [SerializeField] private EnemyCollection regularEnemyCollection;

        [Tooltip("The Boss Enemies collection to draw from")]
        [SerializeField] private EnemyCollection bossEnemyCollection;

        [Header("Room Settings")]
        [Tooltip("Current room level (1-5: 3-7 enemies Reg 1-4, 6-10: 4-10 enemies Reg 1-6, 11+: 5-15 enemies Reg 1-8)")]
        [Min(1)]
        [SerializeField] private int roomLevel = 1;

        public static EnemySpawner Instance { get; private set; }
        public static int CurrentRoomLevel = 1;
        public static bool IsCurrentRoomAllEnemiesCleared { get; set; } = false;
        public static bool WasPreviousRoomAllEnemiesCleared { get; set; } = false;
        public static bool PendingBonusSlotSpin { get; set; } = false;
        public static bool HasLuckDropBonus { get; set; } = false;
        public static float CurrentNpcSpawnChance = 0.10f;
        private static int lastNpcRolledRoomLevel = 0;
        private static bool lastRoomSpawnedNpc = false;
        private static NpcAllyType lastRoomNpcType = NpcAllyType.CatBoxing;
        public static event System.Action OnRoomAllEnemiesCleared;

        private static BossDeck runtimeBossDeck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStaticData()
        {
            CurrentRoomLevel = 1;
            IsCurrentRoomAllEnemiesCleared = false;
            WasPreviousRoomAllEnemiesCleared = false;
            PendingBonusSlotSpin = false;
            HasLuckDropBonus = false;
            CurrentNpcSpawnChance = 0.10f;
            lastNpcRolledRoomLevel = 0;
            lastRoomSpawnedNpc = false;
            runtimeBossDeck?.Reset();
            PlayerInstance.ResetRunStats(30, 3);
            DestructibleBlock.ResetClearedPositions();
            EnemyInstance.ClearStaticEvents();
        }

        public static void ResetNpcSpawnState()
        {
            CurrentNpcSpawnChance = 0.10f;
            lastNpcRolledRoomLevel = 0;
            lastRoomSpawnedNpc = false;
        }

        [Header("NPC Ally Settings")]
        [SerializeField] private GameObject catBoxingNpcPrefab;
        [SerializeField] private GameObject catGuardianNpcPrefab;

        [Header("Hidden Item Drop Settings")]
        [SerializeField] private Sprite ladderSprite;
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private Sprite keyItemSprite;
        [SerializeField] private Sprite heartItemSprite;
        [SerializeField] private Sprite damageItemSprite;
        [SerializeField] private Sprite shieldItemSprite;
        [SerializeField] private Sprite coinItemSprite;
        [SerializeField] private Sprite woodenBarrelSprite;
        [SerializeField] private Sprite woodenBoxSprite;

        [Tooltip("Drop probability (0.20 = 20%) for each potential Shield item in a dungeon (0 - 2 per dungeon)")]
        [Range(0f, 1f)]
        [SerializeField] private float shieldDropChance = 0.20f;

        [Tooltip("Maximum number of Shield items that can spawn per dungeon (0 - 2)")]
        [SerializeField] private int maxShieldsPerRoom = 2;

        [Header("Spawn Layout Settings")]
        [Tooltip("Optional predefined spawn point markers. If empty, enemies are distributed around transform.position")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        [Tooltip("Spacing between enemies when auto-arranging")]
        [SerializeField] private float spawnSpacing = 2.0f;

        [Header("Block Spawning Integration")]
        [Tooltip("How enemies should be spawned when blocks are clicked/destroyed")]
        [SerializeField] private BlockSpawnMode blockSpawnMode = BlockSpawnMode.DistributeIntoBlocks;

        [Tooltip("Optional parent container holding blocks (e.g. Block_Collection). If null, will auto-find in scene.")]
        [SerializeField] private Transform blockContainer;

        [Tooltip("If true, automatically distributes enemies into random blocks on Start() (only when mode is DistributeIntoBlocks)")]
        [SerializeField] private bool autoDistributeToBlocksOnStart = true;

        [Header("Direct Spawn Settings")]
        [Tooltip("If true, automatically spawns room enemies directly into scene on Start() (separate from block hiding)")]
        [SerializeField] private bool autoSpawnOnStart = false;

        public BlockSpawnMode CurrentBlockSpawnMode
        {
            get => blockSpawnMode;
            set => blockSpawnMode = value;
        }

        public int CurrentRoomTargetQuota => currentRoomTargetQuota;
        public int EnemiesSpawnedFromClicks => enemiesSpawnedFromClicks;
        public int EnemiesKilledInRoom => enemiesKilledInRoom;
        public bool IsQuotaReached => enemiesSpawnedFromClicks >= currentRoomTargetQuota;

        private readonly List<GameObject> activeEnemies = new List<GameObject>();
        private readonly List<EnemyInstance> roomEnemyInstances = new List<EnemyInstance>();
        private EnemyDeck runtimeDeck;
        private int currentRoomTargetQuota = 0;
        private int enemiesSpawnedFromClicks = 0;
        private int enemiesKilledInRoom = 0;

        public int RoomLevel
        {
            get => roomLevel;
            set => roomLevel = Mathf.Max(1, value);
        }

        public IReadOnlyList<GameObject> ActiveEnemies => activeEnemies;

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        public void RegisterEvents()
        {
            DestructibleBlock.OnAnyBlockDestroyed -= HandleBlockDestroyed;
            DestructibleBlock.OnAnyBlockDestroyed += HandleBlockDestroyed;
            DestructibleBlock.OnBlockSpawnedEnemy -= HandleBlockSpawnedEnemy;
            DestructibleBlock.OnBlockSpawnedEnemy += HandleBlockSpawnedEnemy;
            EnemyInstance.OnEnemyKilled -= HandleRoomEnemyKilled;
            EnemyInstance.OnEnemyKilled += HandleRoomEnemyKilled;
        }

        public void UnregisterEvents()
        {
            DestructibleBlock.OnAnyBlockDestroyed -= HandleBlockDestroyed;
            DestructibleBlock.OnBlockSpawnedEnemy -= HandleBlockSpawnedEnemy;
            EnemyInstance.OnEnemyKilled -= HandleRoomEnemyKilled;
        }

        private void Awake()
        {
            Instance = this;
            EnsureCollection();
            EnsureBlockContainer();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (CurrentRoomLevel > 0)
            {
                roomLevel = CurrentRoomLevel;
            }
            InitializeRoom(roomLevel);
        }

        /// <summary>
        /// Initializes the room level, rolls the random quota of enemies, and prepares spawner.
        /// </summary>
        public void InitializeRoom(int targetRoomLevel)
        {
            RegisterEvents();
            RoomLevel = targetRoomLevel;
            CurrentRoomLevel = RoomLevel;
            IsCurrentRoomAllEnemiesCleared = false;
            enemiesKilledInRoom = 0;
            roomEnemyInstances.Clear();
            EnsureCollection();
            EnsureBlockContainer();
            EnsurePropSprites();

            enemiesSpawnedFromClicks = 0;
            if (regularEnemyCollection != null)
            {
                currentRoomTargetQuota = regularEnemyCollection.GetRandomEnemyCount(RoomLevel);
                Debug.Log($"<color=cyan>[EnemySpawner] Initialized Room Level {RoomLevel}: Enemy quota set to {currentRoomTargetQuota} (Mode: {blockSpawnMode}).</color>");
            }

            if (blockSpawnMode == BlockSpawnMode.DistributeIntoBlocks && autoDistributeToBlocksOnStart)
            {
                DistributeEnemiesToBlocks(RoomLevel);
            }
            else if (autoSpawnOnStart)
            {
                SpawnRoom(RoomLevel);
            }
        }

        private void HandleRoomEnemyKilled(EnemyInstance enemy)
        {
            enemiesKilledInRoom++;

            bool allDead = currentRoomTargetQuota > 0 && enemiesKilledInRoom >= currentRoomTargetQuota;
            if (!allDead && roomEnemyInstances.Count > 0)
            {
                bool anyAlive = false;
                for (int i = 0; i < roomEnemyInstances.Count; i++)
                {
                    if (roomEnemyInstances[i] != null && !roomEnemyInstances[i].IsDead)
                    {
                        anyAlive = true;
                        break;
                    }
                }
                if (!anyAlive)
                {
                    allDead = true;
                }
            }

            if (allDead && !IsCurrentRoomAllEnemiesCleared)
            {
                IsCurrentRoomAllEnemiesCleared = true;
                Debug.Log($"<color=yellow>[EnemySpawner] All {enemiesKilledInRoom}/{currentRoomTargetQuota} enemies in Room {RoomLevel} killed! Auto-breaking all remaining blocks...</color>");
                CatPowerUpController.TriggerNotification("ALL ENEMIES CLEARED!", new Color(1f, 0.88f, 0.2f));
                OnRoomAllEnemiesCleared?.Invoke();

                if (gameObject.activeInHierarchy && Application.isPlaying)
                {
                    StartCoroutine(ClearAllRemainingBlocksAndAutoLootRoutine());
                }
                else
                {
                    ClearAllRemainingBlocksImmediate();
                }
            }
        }

        public void ClearAllRemainingBlocksImmediate()
        {
            var blocks = GetAvailableBlocks();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null && !blocks[i].IsDestroyed)
                {
                    blocks[i].BreakBlock(true);
                }
            }
        }

        private IEnumerator ClearAllRemainingBlocksAndAutoLootRoutine()
        {
            yield return new WaitForSeconds(0.35f);

            CatPowerUpController.TriggerNotification("ROOM CLEARED! AUTO LOOTING...", new Color(1f, 0.88f, 0.2f));

            var blocks = GetAvailableBlocks();
            var remainingBlocks = new List<DestructibleBlock>();
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b != null && !b.IsDestroyed)
                {
                    remainingBlocks.Add(b);
                }
            }

            // Staggered sequential block breaking (cascade burst effect)
            for (int i = 0; i < remainingBlocks.Count; i++)
            {
                var b = remainingBlocks[i];
                if (b != null && !b.IsDestroyed)
                {
                    b.BreakBlock(true);
                    yield return new WaitForSeconds(0.045f);
                }
            }

            // Wait for all items to fly to the player and get auto-collected
            yield return new WaitForSeconds(0.85f);
            CatPowerUpController.TriggerNotification("ALL LOOT COLLECTED! LADDER READY!", new Color(0.2f, 1f, 0.5f));
        }

        public static void AdvanceToNextLevel()
        {
            // If all enemies were cleared in this room, or if player has >= 5 coins, open bonus slot machine in next room:
            // ("Enemy တွေ့ အကုန်ရှင်းသွားလိုရှိရင် (နောက်တခန်းရောက်ရင် bonus item player ကနေ spin ဆိုပြီးလည့်ရပါမယ်)
            //  coing 5 ခုရှိရင် ဒဲဒီအချိန်မှာ နောက်တခါ လည့်ရပါမယ် (-5 coinနုတ်ပါမယ်)")
            WasPreviousRoomAllEnemiesCleared = IsCurrentRoomAllEnemiesCleared;
            int coins = PlayerInstance.Instance != null ? PlayerInstance.Instance.CurrentCoins : PlayerInstance.SavedCoins;
            PendingBonusSlotSpin = WasPreviousRoomAllEnemiesCleared || coins >= 5;
            IsCurrentRoomAllEnemiesCleared = false;

            if (PlayerInstance.Instance != null && !PlayerInstance.Instance.IsDead)
            {
                // All counted chances, attack counters, bonus chances, and shields are reset when advancing to the next ladder room:
                // ("Cat Blaster,Cat Brawler,Cat Guardian (အားလုံး ရေတွက်တယ် chance to အကုန် Rest ဖြစ်မယ်နော် (next ladder room ရောက်သွားရင်)")
                if (CatPowerUpController.Instance != null)
                {
                    CatPowerUpController.Instance.ResetAllRoomCounters();
                }
                PlayerInstance.SaveRunStats(
                    PlayerInstance.Instance.CurrentHealth,
                    PlayerInstance.Instance.MaxHealth,
                    PlayerInstance.Instance.Damage,
                    0, // Attack counter reset to 0
                    0  // Bonus combo chances reset to 0
                );
                Debug.Log($"<color=cyan>[EnemySpawner] Carrying over Player stats to Room {CurrentRoomLevel + 1}: HP {PlayerInstance.Instance.CurrentHealth}, DMG {PlayerInstance.Instance.Damage}, All counters/chances RESET to 0, BonusSlotPending: {PendingBonusSlotSpin}</color>");
            }

            PlayerInstance.SetHasKey(false);
            CurrentRoomLevel++;
            EnemyInstance.ClearStaticEvents();
            Debug.Log($"<color=green>[EnemySpawner] Advancing to Room Level {CurrentRoomLevel}...</color>");
            if (Application.isPlaying)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        public void EnsureBlockContainer()
        {
            if (blockContainer == null)
            {
                var bcObj = GameObject.Find("Block_Collection");
                if (bcObj != null)
                {
                    blockContainer = bcObj.transform;
                }
            }
        }

        public void EnsureCollection()
        {
            if (regularEnemyCollection == null)
            {
                regularEnemyCollection = Resources.Load<EnemyCollection>("EnemyCollection_Regular");
#if UNITY_EDITOR
                if (regularEnemyCollection == null)
                {
                    regularEnemyCollection = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCollection>("Assets/Data/Enemies/EnemyCollection_Regular.asset");
                }
#endif
            }

            if (bossEnemyCollection == null)
            {
                bossEnemyCollection = Resources.Load<EnemyCollection>("EnemyCollection_Boss");
#if UNITY_EDITOR
                if (bossEnemyCollection == null)
                {
                    bossEnemyCollection = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCollection>("Assets/Data/Enemies/EnemyCollection_Boss.asset");
                }
#endif
            }

            if (regularEnemyCollection != null && runtimeDeck == null)
            {
                runtimeDeck = regularEnemyCollection.CreateDeck();
            }

            if (bossEnemyCollection != null && runtimeBossDeck == null)
            {
                runtimeBossDeck = bossEnemyCollection.CreateBossDeck();
            }
        }

        /// <summary>
        /// Clears previously spawned enemies and generates/spawns a new wave for the given room level.
        /// </summary>
        public List<GameObject> SpawnRoom(int targetRoomLevel)
        {
            RoomLevel = targetRoomLevel;
            ClearActiveEnemies();
            EnsureCollection();

            if (regularEnemyCollection == null)
            {
                Debug.LogError("[EnemySpawner] No regular enemy collection assigned!");
                return activeEnemies;
            }

            // 1. Generate rolled enemies using the collection's room rules
            List<RolledEnemyStats> rolledList = regularEnemyCollection.GenerateRoomEnemies(RoomLevel);
            Debug.Log($"[EnemySpawner] Spawning Room Level {RoomLevel}: {rolledList.Count} enemies generated.");

            // 2. Instantiate and setup each enemy
            for (int i = 0; i < rolledList.Count; i++)
            {
                var stats = rolledList[i];
                Vector3 spawnPos = CalculateSpawnPosition(i, rolledList.Count);

                GameObject go = SpawnSingleEnemy(stats, spawnPos);
                if (go != null)
                {
                    activeEnemies.Add(go);
                }
            }

            return activeEnemies;
        }

        public GameObject SpawnSingleEnemy(RolledEnemyStats stats, Vector3 position, float attackDelay = 0f)
        {
            if (stats.enemyData == null || stats.enemyData.enemyPrefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Cannot spawn {stats.enemyData?.enemyName}: missing prefab!");
                return null;
            }

            GameObject enemyObj = Instantiate(stats.enemyData.enemyPrefab, position, Quaternion.identity);
            enemyObj.name = $"{stats.enemyData.enemyName}_Lv{stats.roomLevel}_HP{stats.health}_DMG{stats.damage}";

            // Attach or initialize EnemyInstance
            var instance = enemyObj.GetComponent<EnemyInstance>();
            if (instance == null)
            {
                instance = enemyObj.AddComponent<EnemyInstance>();
            }
            instance.Initialize(stats, attackDelay);
            if (!roomEnemyInstances.Contains(instance))
            {
                roomEnemyInstances.Add(instance);
            }

            return enemyObj;
        }

        public void ClearActiveEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    if (Application.isPlaying)
                        Destroy(enemy);
                    else
                        DestroyImmediate(enemy);
                }
            }
            activeEnemies.Clear();
        }

        private Vector3 CalculateSpawnPosition(int index, int total)
        {
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                var point = spawnPoints[index % spawnPoints.Count];
                if (point != null) return point.position;
            }

            // Auto-arrange in horizontal row centered around this spawner
            float offset = (index - (total - 1) / 2.0f) * spawnSpacing;
            return transform.position + new Vector3(offset, 0f, 0f);
        }

        public void EnsurePropSprites()
        {
            if (ladderSprite == null)
            {
                ladderSprite = Resources.Load<Sprite>("Ladder");
#if UNITY_EDITOR
                if (ladderSprite == null)
                {
                    ladderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Crystal_Caves_Forest_2D_Platformer_Tileset_Platformer - Ladder.png");
                }
#endif
            }

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

            if (keyItemSprite == null)
            {
                keyItemSprite = Resources.Load<Sprite>("Icons/Icon_Key_Gold");
            }
            if (keyItemSprite == null)
            {
                keyItemSprite = Resources.Load<Sprite>("Icon_Key_Gold");
            }
#if UNITY_EDITOR
            if (keyItemSprite == null)
            {
                keyItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Key_Gold.png");
            }
            if (keyItemSprite == null)
            {
                keyItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Key_Gold.png");
            }
#endif

            if (heartItemSprite == null)
            {
                heartItemSprite = Resources.Load<Sprite>("Icons/Icon_Heart");
#if UNITY_EDITOR
                if (heartItemSprite == null)
                {
                    heartItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Heart.png");
                }
#endif
            }

            if (damageItemSprite == null)
            {
                damageItemSprite = Resources.Load<Sprite>("Icons/Icon_Sword");
#if UNITY_EDITOR
                if (damageItemSprite == null)
                {
                    damageItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Sword.png");
                }
#endif
            }

            if (shieldItemSprite == null)
            {
                shieldItemSprite = Resources.Load<Sprite>("Icons/Icon_Shield");
#if UNITY_EDITOR
                if (shieldItemSprite == null)
                {
                    shieldItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Shield.png");
                }
                if (shieldItemSprite == null)
                {
                    shieldItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Shield.png");
                }
#endif
            }

            if (coinItemSprite == null)
            {
                coinItemSprite = Resources.Load<Sprite>("Icons/Icon_Coin");
#if UNITY_EDITOR
                if (coinItemSprite == null)
                {
                    coinItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Coin.png");
                }
                if (coinItemSprite == null)
                {
                    coinItemSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/IconMisc/Icon_ImageIcon_Coin01_l.png");
                }
#endif
            }

            if (woodenBarrelSprite == null)
            {
                woodenBarrelSprite = Resources.Load<Sprite>("Wooden Barrel");
#if UNITY_EDITOR
                if (woodenBarrelSprite == null)
                {
                    woodenBarrelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Crystal_Caves_Forest_2D_Platformer_Tileset_Platformer - Wooden Barrel.png");
                }
#endif
            }

            if (woodenBoxSprite == null)
            {
                woodenBoxSprite = Resources.Load<Sprite>("Wooden Box");
#if UNITY_EDITOR
                if (woodenBoxSprite == null)
                {
                    woodenBoxSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Crystal_Caves_Forest_2D_Platformer_Tileset_Platformer - Wooden Box.png");
                }
#endif
            }

            if (catBoxingNpcPrefab == null)
            {
                catBoxingNpcPrefab = Resources.Load<GameObject>("NpcAllies/CatBoxing");
#if UNITY_EDITOR
                if (catBoxingNpcPrefab == null)
                {
                    catBoxingNpcPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spine Characters/CatBoxing/CatBoxing.prefab");
                }
#endif
            }

            if (catGuardianNpcPrefab == null)
            {
                catGuardianNpcPrefab = Resources.Load<GameObject>("NpcAllies/CatGuardian");
#if UNITY_EDITOR
                if (catGuardianNpcPrefab == null)
                {
                    catGuardianNpcPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spine Characters/CatGuardian/Enemy.prefab");
                }
#endif
            }
        }

        public static int RollHeartValue(int roomLevel)
        {
            if (roomLevel <= 5)
                return Random.Range(1, 4); // 1 - 3
            else if (roomLevel <= 10)
                return Random.Range(2, 6); // 2 - 5
            else if (roomLevel <= 15)
                return Random.Range(3, 7); // 3 - 6
            else
            {
                int min = 1 + (roomLevel - 1) / 5;
                int max = min + 3;
                return Random.Range(min, max + 1);
            }
        }

        /// <summary>
        /// Rolls 0 to maxShields (default 0 - 2) Shield items for a dungeon,
        /// where each potential Shield drop has a dropChance (default 20% = 0.20f) probability.
        /// </summary>
        public static int RollShieldDropCount(float dropChance = 0.20f, int maxShields = 2)
        {
            int count = 0;
            int slots = Mathf.Max(0, maxShields);
            for (int i = 0; i < slots; i++)
            {
                if (Random.value < dropChance)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Pre-allocates and hides under blocks:
        /// 1. Exactly 1 Ladder (progresses to next level on click)
        /// 2. Regular Enemies matching room level quota (Lv 1-5: 3-7)
        /// 3. Wooden Props (3 to 10 random Barrels & Boxes)
        /// All objects are pre-spawned and disabled with gameObject.SetActive(false) before the game starts.
        /// </summary>
        public void DistributeEnemiesToBlocks(int targetRoomLevel)
        {
            RoomLevel = targetRoomLevel;
            CurrentRoomLevel = RoomLevel;
            EnsureCollection();
            EnsureBlockContainer();
            EnsurePropSprites();

            if (regularEnemyCollection == null)
            {
                Debug.LogError("[EnemySpawner] Regular enemy collection not assigned!");
                return;
            }

            var blocks = GetAvailableBlocks();
            if (blocks.Count == 0)
            {
                Debug.LogWarning("[EnemySpawner] No DestructibleBlocks found to distribute enemies into!");
                return;
            }

            DestructibleBlock.ResetClearedPositions();

            // 1. Clear previous assignments & pre-spawned hidden contents
            foreach (var b in blocks)
            {
                if (b != null)
                {
                    b.ClearAssignedEnemy();
                    b.ClearHiddenContent();
                }
            }

            // 2. Generate rolled enemies list based on Room Level rules
            List<RolledEnemyStats> rolledList = regularEnemyCollection.GenerateRoomEnemies(RoomLevel);
            currentRoomTargetQuota = rolledList.Count;
            enemiesSpawnedFromClicks = 0;

            // 3. Shuffle blocks using Fisher-Yates
            for (int i = 0; i < blocks.Count; i++)
            {
                int r = Random.Range(i, blocks.Count);
                var temp = blocks[i];
                blocks[i] = blocks[r];
                blocks[r] = temp;
            }

            int blockIdx = 0;

            // 4. Place exactly 1 Locked Ladder, removing the block covering it so it starts revealed
            if (blockIdx < blocks.Count)
            {
                var ladderBlock = blocks[blockIdx++];
                Vector3 ladderPos = ladderBlock.transform.position;
                var ladderObj = new GameObject("Ladder_Prop");
                ladderObj.transform.position = ladderPos;
                ladderObj.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
                var sr = ladderObj.AddComponent<SpriteRenderer>();
                sr.sprite = ladderSprite;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 1;
                var col = ladderObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);
                var ladder = ladderObj.AddComponent<LevelLadder>();
                ladder.Initialize(lockSprite);
                ladderObj.SetActive(true);
                ladder.OnRevealedFromBlock(0f);

                // Destroy the block covering the ladder so the locked ladder is visible immediately at start
                if (Application.isPlaying)
                {
                    Destroy(ladderBlock.gameObject);
                }
                else
                {
                    DestroyImmediate(ladderBlock.gameObject);
                }

                // Register ladder position as cleared starting tile (4 directions will be clickable)
                DestructibleBlock.RegisterClearedPosition(ladderPos);
            }

            // 4b. Place exactly 1 Gold Key under another random block
            if (blockIdx < blocks.Count)
            {
                var keyBlock = blocks[blockIdx++];
                var keyObj = new GameObject("Item_Key_Prop");
                keyObj.transform.position = keyBlock.transform.position;
                keyObj.transform.localScale = new Vector3(1.7f, 1.7f, 1f);

                var sr = keyObj.AddComponent<SpriteRenderer>();
                sr.sprite = keyItemSprite;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 5;

                var col = keyObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.2f, 1.2f);

                var pickup = keyObj.AddComponent<ItemPickupProp>();
                pickup.Initialize(ItemPickupType.Key, 1, keyItemSprite);

                keyObj.SetActive(false);
                keyBlock.SetHiddenContent(keyObj);
                Debug.Log($"<color=yellow>[EnemySpawner] Room Level {RoomLevel}: Pre-allocated 1 Gold Key under block at {keyBlock.transform.position}.</color>");
            }

            // 5. If this room is scheduled for a Boss, place exactly 1 Boss under a block
            EnemyData bossData = null;
            if (runtimeBossDeck != null)
            {
                bossData = runtimeBossDeck.DrawBossForRoom(RoomLevel);
            }
            else if (bossEnemyCollection != null)
            {
                runtimeBossDeck = bossEnemyCollection.CreateBossDeck();
                bossData = runtimeBossDeck.DrawBossForRoom(RoomLevel);
            }

            if (bossData != null && blockIdx < blocks.Count)
            {
                var bossBlock = blocks[blockIdx++];
                int stepLevels = bossEnemyCollection != null ? bossEnemyCollection.levelsPerScalingStep : 3;
                int hpBonus = bossEnemyCollection != null ? bossEnemyCollection.hpBonusPerStep : 1;
                int dmgBonus = bossEnemyCollection != null ? bossEnemyCollection.damageBonusPerStep : 1;
                var bossStats = bossData.RollStats(RoomLevel, stepLevels, hpBonus, dmgBonus);

                if (bossStats.enemyData != null && bossStats.enemyData.enemyPrefab != null)
                {
                    Vector3 spawnOffset = bossStats.enemyData.GetSpawnOffset();
                    Vector3 spawnPos = bossBlock.transform.position + spawnOffset;
                    var bossObj = Instantiate(bossStats.enemyData.enemyPrefab, spawnPos, Quaternion.identity);
                    bossObj.name = $"Boss_{bossStats.enemyData.enemyName}_Lv{bossStats.roomLevel}_HP{bossStats.health}_DMG{bossStats.damage}";
                    var instance = bossObj.GetComponent<EnemyInstance>() ?? bossObj.AddComponent<EnemyInstance>();
                    instance.SourceBlockPosition = bossBlock.transform.position;
                    instance.Initialize(bossStats, 0.65f);
                    bossObj.SetActive(false);
                    bossBlock.SetHiddenContent(bossObj);
                    roomEnemyInstances.Add(instance);
                }
                bossBlock.SetAssignedEnemy(bossStats);
                Debug.Log($"<color=yellow>[EnemySpawner] Room Level {RoomLevel}: Placed Boss '{bossStats.enemyData.enemyName}' (HP: {bossStats.health}, DMG: {bossStats.damage}) under block at {bossBlock.transform.position}.</color>");
            }

            // 6. Place room quota of enemies under subsequent blocks
            int enemiesToPlace = Mathf.Min(rolledList.Count, blocks.Count - blockIdx);
            for (int i = 0; i < enemiesToPlace; i++)
            {
                var block = blocks[blockIdx++];
                var stats = rolledList[i];
                if (stats.enemyData != null && stats.enemyData.enemyPrefab != null)
                {
                    Vector3 spawnOffset = stats.enemyData.GetSpawnOffset();
                    Vector3 spawnPos = block.transform.position + spawnOffset;
                    var enemyObj = Instantiate(stats.enemyData.enemyPrefab, spawnPos, Quaternion.identity);
                    enemyObj.name = $"{stats.enemyData.enemyName}_Lv{stats.roomLevel}_HP{stats.health}_DMG{stats.damage}";
                    var instance = enemyObj.GetComponent<EnemyInstance>() ?? enemyObj.AddComponent<EnemyInstance>();
                    instance.SourceBlockPosition = block.transform.position;
                    instance.Initialize(stats, 0.65f);
                    enemyObj.SetActive(false);
                    block.SetHiddenContent(enemyObj);
                    roomEnemyInstances.Add(instance);
                }
                block.SetAssignedEnemy(stats);
            }

            currentRoomTargetQuota = (bossData != null ? 1 : 0) + enemiesToPlace;

            // 6b. Place at most 1 NPC Ally (CatBoxing or CatGuardian) under a random remaining block
            // Starts at 10% chance; if not spawned in this room, increases by +10% for the next room; if spawned, resets to 10%.
            // When HasLuckDropBonus is active, adds +25% bonus chance so Cat Ally spawns noticeably more frequently:
            // ("Cat NPC ကိုယ်ဘက်ကပါတယ်သူရော်ပဲ (နည်းနည်း drop rate) ပိုကျပေးပါ")
            if (blockIdx < blocks.Count)
            {
                bool shouldSpawnNpc;
                NpcAllyType chosenNpcType;
                float effectiveNpcChance = HasLuckDropBonus
                    ? Mathf.Clamp01(CurrentNpcSpawnChance + 0.25f)
                    : CurrentNpcSpawnChance;
                float rollChanceUsed = effectiveNpcChance;

                if (lastNpcRolledRoomLevel != RoomLevel)
                {
                    lastNpcRolledRoomLevel = RoomLevel;
                    float roll = Random.value;
                    shouldSpawnNpc = roll < effectiveNpcChance;
                    if (shouldSpawnNpc)
                    {
                        chosenNpcType = Random.Range(0, 2) == 0 ? NpcAllyType.CatBoxing : NpcAllyType.CatGuardian;
                        lastRoomSpawnedNpc = true;
                        lastRoomNpcType = chosenNpcType;
                        CurrentNpcSpawnChance = 0.10f; // Reset base back to 10% when spawned
                        Debug.Log($"<color=green>[EnemySpawner] Room Level {RoomLevel}: NPC Ally '{chosenNpcType}' rolled SUCCESS (Roll {roll:F2} < {rollChanceUsed:F2}, LuckBonus: {HasLuckDropBonus})! Base NPC chance reset to 10%.</color>");
                    }
                    else
                    {
                        chosenNpcType = NpcAllyType.CatBoxing;
                        lastRoomSpawnedNpc = false;
                        CurrentNpcSpawnChance = Mathf.Clamp01(CurrentNpcSpawnChance + 0.10f); // Increase by +10% for next level
                        Debug.Log($"<color=gray>[EnemySpawner] Room Level {RoomLevel}: No NPC Ally spawned (Roll {roll:F2} >= {rollChanceUsed:F2}, LuckBonus: {HasLuckDropBonus}). Next level base NPC chance increased to {CurrentNpcSpawnChance * 100f:F0}%.</color>");
                    }
                }
                else
                {
                    shouldSpawnNpc = lastRoomSpawnedNpc;
                    chosenNpcType = lastRoomNpcType;
                }

                if (shouldSpawnNpc)
                {
                    var npcBlock = blocks[blockIdx++];
                    SpawnNpcAllyUnderBlock(npcBlock, chosenNpcType);
                }
            }

            // 7. Place Item Drops (Heart, Damage, Shield, Coin) under random remaining blocks:
            // ("bonus item ထဲမှာ (luck ကျရင်) Damage + ,hp+, shield, Coin , Cat NPC ကိုယ်ဘက်ကပါတယ်သူရော်ပဲ (နည်းနည်း drop rate) ပိုကျပေးပါ")
            // With Luck Bonus: higher drop counts and probabilities for Heart, Damage, Shield, Coin
            int heartsToPlace = HasLuckDropBonus ? Random.Range(2, 5) : Random.Range(1, 4); // 2 to 4 hearts with Luck, else 1 to 3

            int damageToPlace;
            if (HasLuckDropBonus)
            {
                // With Luck Bonus: On even rooms, 1 to 2 Damage drops; on odd rooms, 50% chance for 1 Damage drop
                damageToPlace = (roomLevel >= 2 && roomLevel % 2 == 0)
                    ? (Random.value < 0.50f ? 2 : 1)
                    : (Random.value < 0.50f ? 1 : 0);
            }
            else
            {
                damageToPlace = (roomLevel >= 2 && roomLevel % 2 == 0) ? 1 : 0;
            }

            // Shield items: 20% base probability, increased to 35% with Luck (max up to 3)
            float effectiveShieldChance = HasLuckDropBonus ? 0.35f : shieldDropChance;
            int effectiveMaxShields = HasLuckDropBonus ? Mathf.Max(3, maxShieldsPerRoom + 1) : maxShieldsPerRoom;
            int shieldsToPlace = RollShieldDropCount(effectiveShieldChance, effectiveMaxShields);

            // Coins: 1 to 3 normally; 2 to 4 with Luck Bonus
            int coinsToPlace = HasLuckDropBonus ? Random.Range(2, 5) : Random.Range(1, 4);

            var itemRequests = new List<(ItemPickupType type, int value, Sprite sprite)>();

            for (int i = 0; i < heartsToPlace; i++)
            {
                itemRequests.Add((ItemPickupType.Heart, RollHeartValue(roomLevel), heartItemSprite));
            }

            for (int i = 0; i < damageToPlace; i++)
            {
                itemRequests.Add((ItemPickupType.Damage, 1, damageItemSprite));
            }

            for (int i = 0; i < shieldsToPlace; i++)
            {
                itemRequests.Add((ItemPickupType.Shield, 1, shieldItemSprite));
            }

            for (int i = 0; i < coinsToPlace; i++)
            {
                itemRequests.Add((ItemPickupType.Coin, 1, coinItemSprite));
            }

            // If Luck Bonus was won from the Slot Machine, add 2 to 4 extra items (HP, Damage, Shield, Coin)
            int luckBonusItems = 0;
            if (HasLuckDropBonus)
            {
                luckBonusItems = Random.Range(2, 5); // 2 to 4 extra drops
                for (int i = 0; i < luckBonusItems; i++)
                {
                    itemRequests.Add(RollRandomLuckItem(roomLevel));
                }
            }

            // Shuffle item distribution order so item types are randomly mixed across blocks
            for (int i = itemRequests.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                var temp = itemRequests[i];
                itemRequests[i] = itemRequests[r];
                itemRequests[r] = temp;
            }

            int placedItems = 0;
            foreach (var req in itemRequests)
            {
                if (blockIdx >= blocks.Count) break;

                var block = blocks[blockIdx++];
                PlaceHiddenItemOnBlock(block, req.type, req.value, req.sprite);
                placedItems++;
            }

            DestructibleBlock.UpdateAllBlocksVisuals();
            Debug.Log($"<color=cyan>[EnemySpawner] Room Level {RoomLevel}: Pre-allocated 1 Locked Ladder, 1 Gold Key, {enemiesToPlace} Enemies, {placedItems} Item Drops (Hearts:{heartsToPlace}, DMG:{damageToPlace}, Shields:{shieldsToPlace}, Coins:{coinsToPlace}, LuckBonus:{luckBonusItems}) across {blocks.Count} blocks.</color>");
        }

        private (ItemPickupType type, int value, Sprite sprite) RollRandomLuckItem(int targetRoomLevel)
        {
            EnsurePropSprites();
            // 4 Items: Heart (HP), Damage, Shield, Coin
            // ("bonus item ထဲမှာ (luck ကျရင်) Damage + ,hp+, shield, Coin , Cat NPC ကိုယ်ဘက်ကပါတယ်သူရော်ပဲ (နည်းနည်း drop rate) ပိုကျပေးပါ")
            int pick = Random.Range(0, 4);
            switch (pick)
            {
                case 0:
                    return (ItemPickupType.Heart, RollHeartValue(targetRoomLevel), heartItemSprite);
                case 1:
                    return (ItemPickupType.Damage, 1, damageItemSprite);
                case 2:
                    return (ItemPickupType.Shield, 1, shieldItemSprite);
                default:
                    return (ItemPickupType.Coin, 1, coinItemSprite);
            }
        }

        public GameObject SpawnNpcAllyUnderBlock(DestructibleBlock npcBlock, NpcAllyType chosenNpcType)
        {
            if (npcBlock == null) return null;
            EnsurePropSprites();
            GameObject npcPrefab = chosenNpcType == NpcAllyType.CatBoxing ? catBoxingNpcPrefab : catGuardianNpcPrefab;
            if (npcPrefab == null) return null;

            Vector3 npcOffset = chosenNpcType == NpcAllyType.CatBoxing
                ? new Vector3(0f, -1.52f, 0f)
                : new Vector3(0.15f, -1.62f, 0f);
            Vector3 npcSpawnPos = npcBlock.transform.position + npcOffset;

            var npcObj = Instantiate(npcPrefab, npcSpawnPos, Quaternion.identity);
            var enemyComp = npcObj.GetComponent<EnemyInstance>();
            if (enemyComp != null)
            {
                if (Application.isPlaying) Destroy(enemyComp);
                else DestroyImmediate(enemyComp);
            }

            var npcAlly = npcObj.GetComponent<NpcAllyInstance>() ?? npcObj.AddComponent<NpcAllyInstance>();
            npcAlly.Initialize(chosenNpcType, RoomLevel);
            npcObj.name = $"NPC_{chosenNpcType}_Lv{RoomLevel}_HP{npcAlly.MaxHealth}_DMG{npcAlly.Damage}";
            npcObj.SetActive(false);
            npcBlock.SetHiddenContent(npcObj);
            Debug.Log($"<color=green>[EnemySpawner] Room Level {RoomLevel}: Hidden NPC Ally '{npcObj.name}' under block at {npcBlock.transform.position}.</color>");
            return npcObj;
        }

        private void PlaceHiddenItemOnBlock(DestructibleBlock block, ItemPickupType type, int value, Sprite sprite)
        {
            if (block == null) return;
            var itemObj = new GameObject($"Item_{type}_Prop");
            itemObj.transform.position = block.transform.position;
            itemObj.transform.localScale = new Vector3(1.7f, 1.7f, 1f);

            var sr = itemObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 5;

            var col = itemObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);

            var pickup = itemObj.AddComponent<ItemPickupProp>();
            pickup.Initialize(type, value, sprite);

            itemObj.SetActive(false);
            block.SetHiddenContent(itemObj);
        }

        /// <summary>
        /// Activates the Luck Bonus from the Slot Machine ("bonus item ထဲမှာ (luck ကျရင်) Damage + ,hp+, shield, Coin , Cat NPC ကိုယ်ဘက်ကပါတယ်သူရော်ပဲ (နည်းနည်း drop rate) ပိုကျပေးပါ")
        /// and injects extra items (Damage, HP, Shield, Coin) and rolls for an Ally Cat into empty blocks of the current room.
        /// </summary>
        public static int ActivateLuckBonus()
        {
            HasLuckDropBonus = true;
            int spawnedNow = 0;
            if (Instance != null)
            {
                spawnedNow = Instance.SpawnExtraLuckItemsInCurrentRoom();
            }
            Debug.Log($"<color=green>[EnemySpawner] Luck Bonus activated! Future rooms & current room boosted for DMG, HP, Shield, Coin, and Cat Ally. Added {spawnedNow} extra items/allies to current room.</color>");
            return spawnedNow;
        }

        public int SpawnExtraLuckItemsInCurrentRoom()
        {
            EnsurePropSprites();
            var blocks = GetAvailableBlocks();
            var emptyBlocks = new List<DestructibleBlock>();
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b != null && !b.IsDestroyed && b.HiddenContent == null && !b.HasAssignedEnemy)
                {
                    emptyBlocks.Add(b);
                }
            }

            if (emptyBlocks.Count == 0) return 0;

            int spawnedCount = 0;

            // 1. If this room does not have a Cat NPC yet, give a 50% chance to spawn an ally Cat NPC right now:
            // ("Cat NPC ကိုယ်ဘက်ကပါတယ်သူရော်ပဲ (နည်းနည်း drop rate) ပိုကျပေးပါ")
            bool hasNpcInRoom = lastRoomSpawnedNpc || NpcAllyInstance.ActiveAlly != null || UnityEngine.Object.FindAnyObjectByType<NpcAllyInstance>() != null;
            if (!hasNpcInRoom && emptyBlocks.Count > 1)
            {
                float npcRoll = Random.value;
                if (npcRoll < 0.50f)
                {
                    var chosenType = Random.Range(0, 2) == 0 ? NpcAllyType.CatBoxing : NpcAllyType.CatGuardian;
                    var npcBlock = emptyBlocks[0];
                    emptyBlocks.RemoveAt(0);
                    SpawnNpcAllyUnderBlock(npcBlock, chosenType);
                    lastRoomSpawnedNpc = true;
                    lastRoomNpcType = chosenType;
                    spawnedCount++;
                    CatPowerUpController.TriggerNotification($"LUCK FOUND ALLY {chosenType}!", Color.green);
                }
            }

            // 2. Spawn 2 to 4 extra items (Heart, Damage, Shield, Coin) into remaining empty blocks
            int extraCount = Mathf.Min(Random.Range(2, 5), emptyBlocks.Count);
            for (int i = 0; i < extraCount && emptyBlocks.Count > 0; i++)
            {
                int r = Random.Range(0, emptyBlocks.Count);
                var block = emptyBlocks[r];
                emptyBlocks.RemoveAt(r);

                var req = RollRandomLuckItem(RoomLevel);
                PlaceHiddenItemOnBlock(block, req.type, req.value, req.sprite);
                spawnedCount++;
            }

            DestructibleBlock.UpdateAllBlocksVisuals();
            return spawnedCount;
        }

        public List<DestructibleBlock> GetAvailableBlocks()
        {
            var list = new List<DestructibleBlock>();
            if (blockContainer != null)
            {
                blockContainer.GetComponentsInChildren(false, list);
            }
            else
            {
                var bcObj = GameObject.Find("Block_Collection");
                if (bcObj != null)
                {
                    blockContainer = bcObj.transform;
                    blockContainer.GetComponentsInChildren(false, list);
                }
                else
                {
                    list.AddRange(Object.FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None));
                }
            }
            return list;
        }

        private void HandleBlockSpawnedEnemy(DestructibleBlock block, EnemyInstance enemy)
        {
            if (enemy != null && !activeEnemies.Contains(enemy.gameObject))
            {
                activeEnemies.Add(enemy.gameObject);
                enemiesSpawnedFromClicks++;
            }
        }

        private void HandleBlockDestroyed(DestructibleBlock block)
        {
            if (block == null) return;

            // Mode: Spawn entire wave when any block is clicked
            if (blockSpawnMode == BlockSpawnMode.SpawnWaveOnBlockClick)
            {
                if (activeEnemies.Count == 0)
                {
                    SpawnWaveAtPosition(block.transform.position, roomLevel);
                }
            }
            // Mode: Spawn 1 enemy per block click until room quota is satisfied
            else if (blockSpawnMode == BlockSpawnMode.SpawnOnePerBlockUntilLimit)
            {
                if (currentRoomTargetQuota <= 0 && regularEnemyCollection != null)
                {
                    currentRoomTargetQuota = regularEnemyCollection.GetRandomEnemyCount(roomLevel);
                }

                if (enemiesSpawnedFromClicks < currentRoomTargetQuota)
                {
                    EnsureCollection();
                    if (runtimeDeck == null && regularEnemyCollection != null)
                    {
                        runtimeDeck = regularEnemyCollection.CreateDeck();
                    }

                    if (runtimeDeck != null)
                    {
                        var enemyData = runtimeDeck.DrawNext(roomLevel);
                        if (enemyData != null)
                        {
                            var stats = enemyData.RollStats(roomLevel, regularEnemyCollection.levelsPerScalingStep, regularEnemyCollection.hpBonusPerStep, regularEnemyCollection.damageBonusPerStep);
                            Vector3 spawnOffset = enemyData.GetSpawnOffset();
                            var spawned = SpawnSingleEnemy(stats, block.transform.position + spawnOffset, 0.65f);
                            if (spawned != null)
                            {
                                var instance = spawned.GetComponent<EnemyInstance>();
                                if (instance != null)
                                {
                                    instance.SourceBlockPosition = block.transform.position;
                                }
                                activeEnemies.Add(spawned);
                                enemiesSpawnedFromClicks++;
                                Debug.Log($"<color=yellow>[EnemySpawner] Block Clicked! Spawned {enemiesSpawnedFromClicks}/{currentRoomTargetQuota} ({enemyData.enemyName}) at {block.transform.position}</color>");
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"[EnemySpawner] Block Clicked: Room Level {roomLevel} enemy quota ({enemiesSpawnedFromClicks}/{currentRoomTargetQuota}) already satisfied. No more enemies will spawn from blocks.");
                }
            }
        }

        public List<GameObject> SpawnWaveAtPosition(Vector3 centerPos, int targetRoomLevel)
        {
            RoomLevel = targetRoomLevel;
            EnsureCollection();

            if (regularEnemyCollection == null) return activeEnemies;

            List<RolledEnemyStats> rolledList = regularEnemyCollection.GenerateRoomEnemies(RoomLevel);
            for (int i = 0; i < rolledList.Count; i++)
            {
                float offset = (i - (rolledList.Count - 1) / 2.0f) * spawnSpacing;
                Vector3 pos = centerPos + new Vector3(offset, 0f, 0f);
                var go = SpawnSingleEnemy(rolledList[i], pos);
                if (go != null)
                {
                    activeEnemies.Add(go);
                }
            }

            return activeEnemies;
        }
    }

    public enum BlockSpawnMode
    {
        [Tooltip("Distribute the room's calculated enemy count (Lv 1-5: 3-7) into random blocks in the scene. Clicking those blocks breaks them and reveals the enemy.")]
        DistributeIntoBlocks,

        [Tooltip("Clicking any block spawns the room's wave of enemies (Lv 1-5: 3-7) around the clicked block.")]
        SpawnWaveOnBlockClick,

        [Tooltip("Clicking each block spawns 1 random enemy until the room quota (Lv 1-5: 3-7) is reached.")]
        SpawnOnePerBlockUntilLimit,

        [Tooltip("Disable block-based spawning.")]
        None
    }
}
