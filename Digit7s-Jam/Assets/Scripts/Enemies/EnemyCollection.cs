using System.Collections.Generic;
using UnityEngine;

namespace Digit7s.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemyCollection", menuName = "Digit7s/Enemies/Enemy Collection")]
    public class EnemyCollection : ScriptableObject
    {
        [Header("Collection Information")]
        [Tooltip("Collection title (e.g. Regular Enemies Collection, Boss Collection)")]
        public string collectionName = "Enemy Collection";

        [Tooltip("Type category of this collection")]
        public EnemyType collectionType = EnemyType.Regular;

        [TextArea(2, 3)]
        [Tooltip("Description or notes for this collection")]
        public string description;

        [Header("Enemies List")]
        [Tooltip("List of enemy ScriptableObjects in this collection")]
        public List<EnemyData> enemies = new List<EnemyData>();

        [Header("Room Level Spawning Tiers")]
        [Tooltip("Configuration of enemy count limits and allowed enemy range per room level")]
        public List<RoomSpawnTier> roomTiers = new List<RoomSpawnTier>();

        [Header("Stat Scaling Rule Settings")]
        [Tooltip("Number of completed levels for each scaling step (default: 3)")]
        [Min(1)] public int levelsPerScalingStep = 3;

        [Tooltip("Base HP min & max increase per step (default: 1)")]
        [Min(0)] public int hpBonusPerStep = 1;

        [Tooltip("Base Damage min & max increase per step (default: 1)")]
        [Min(0)] public int damageBonusPerStep = 1;

        public int Count => enemies != null ? enemies.Count : 0;

        public EnemyData GetEnemy(int index)
        {
            if (enemies == null || enemies.Count == 0) return null;
            int clamped = Mathf.Clamp(index, 0, enemies.Count - 1);
            return enemies[clamped];
        }

        public EnemyData GetEnemyById(string id)
        {
            if (enemies == null) return null;
            return enemies.Find(e => e != null && e.enemyId == id);
        }

        public EnemyData GetRandomEnemy()
        {
            if (enemies == null || enemies.Count == 0) return null;
            return enemies[Random.Range(0, enemies.Count)];
        }

        public bool Contains(EnemyData enemy)
        {
            return enemies != null && enemies.Contains(enemy);
        }

        /// <summary>
        /// Gets the matching RoomSpawnTier for the given room level.
        /// Falls back to default rules if roomTiers is empty or unconfigured:
        /// Lv 1 - 5: 3 - 7 count, Reg 1 - 4
        /// Lv 6 - 10: 4 - 10 count, Reg 1 - 6
        /// Lv 11+: 5 - 15 count, Reg 1 - 8
        /// </summary>
        public RoomSpawnTier GetTierForRoom(int roomLevel)
        {
            if (roomTiers != null && roomTiers.Count > 0)
            {
                foreach (var tier in roomTiers)
                {
                    if (tier != null && tier.MatchesRoomLevel(roomLevel))
                    {
                        return tier;
                    }
                }

                if (collectionType == EnemyType.Boss)
                {
                    return null;
                }
            }

            if (collectionType == EnemyType.Boss)
            {
                return null;
            }

            // Built-in fallback matching the specification for regular enemies
            if (roomLevel <= 5)
            {
                return new RoomSpawnTier
                {
                    tierName = "Fallback Tier 1 (Lv 1 - 5)",
                    minRoomLevel = 1,
                    maxRoomLevel = 5,
                    minEnemyCount = 3,
                    maxEnemyCount = 7,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 4
                };
            }
            else if (roomLevel <= 10)
            {
                return new RoomSpawnTier
                {
                    tierName = "Fallback Tier 2 (Lv 6 - 10)",
                    minRoomLevel = 6,
                    maxRoomLevel = 10,
                    minEnemyCount = 4,
                    maxEnemyCount = 10,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 6
                };
            }
            else
            {
                return new RoomSpawnTier
                {
                    tierName = "Fallback Tier 3 (Lv 11+)",
                    minRoomLevel = 11,
                    maxRoomLevel = -1,
                    minEnemyCount = 5,
                    maxEnemyCount = 15,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 8
                };
            }
        }

        /// <summary>
        /// Gets the list of allowed enemies for the given room level according to the tier rules.
        /// </summary>
        public List<EnemyData> GetAllowedEnemiesForRoom(int roomLevel)
        {
            var allowed = new List<EnemyData>();
            if (enemies == null || enemies.Count == 0) return allowed;

            var tier = GetTierForRoom(roomLevel);
            int minNum = tier != null ? tier.minEnemyNumber : 1;
            int maxNum = tier != null ? tier.maxEnemyNumber : enemies.Count;

            // 1-based indexing: minNum 1 to maxNum 4 -> indices 0 to 3
            int startIdx = Mathf.Clamp(minNum - 1, 0, enemies.Count - 1);
            int endIdx = Mathf.Clamp(maxNum - 1, startIdx, enemies.Count - 1);

            for (int i = startIdx; i <= endIdx; i++)
            {
                if (enemies[i] != null)
                {
                    allowed.Add(enemies[i]);
                }
            }

            // Safety fallback if empty
            if (allowed.Count == 0)
            {
                allowed.AddRange(enemies);
            }

            return allowed;
        }

        /// <summary>
        /// Rolls random enemy count for the given room level based on tier limits.
        /// </summary>
        public int GetRandomEnemyCount(int roomLevel)
        {
            var tier = GetTierForRoom(roomLevel);
            return tier != null ? tier.RollEnemyCount() : Random.Range(3, 8);
        }

        /// <summary>
        /// Draws 'count' enemies from the allowed pool without duplicates until all enemies have dropped.
        /// Once all enemies in the pool have been selected, the pool is refilled from the beginning.
        /// (".အကောင်တူမကျအောင်လုပ်မယ် , အကယ်ရှ် အကောင်အကုန် ကျပြီးရင်တော့, အစကနေပြန်ပြီတော့ထည့်မယ်")
        /// </summary>
        public List<EnemyData> DrawEnemiesWithoutImmediateDuplicates(int count, List<EnemyData> pool)
        {
            var result = new List<EnemyData>();
            if (pool == null || pool.Count == 0 || count <= 0) return result;

            var workingBag = new List<EnemyData>(pool);
            EnemyData lastPicked = null;

            for (int i = 0; i < count; i++)
            {
                // If bag is empty (all enemies dropped once), refill from beginning
                if (workingBag.Count == 0)
                {
                    workingBag.AddRange(pool);
                }

                int pickIndex;
                // If we just refilled and have multiple choices, avoid immediately repeating the exact same enemy
                if (workingBag.Count > 1 && lastPicked != null && workingBag.Contains(lastPicked))
                {
                    do
                    {
                        pickIndex = Random.Range(0, workingBag.Count);
                    } while (workingBag.Count > 1 && workingBag[pickIndex] == lastPicked);
                }
                else
                {
                    pickIndex = Random.Range(0, workingBag.Count);
                }

                EnemyData picked = workingBag[pickIndex];
                result.Add(picked);
                lastPicked = picked;

                // Remove to prevent duplicate until all other enemies have dropped
                workingBag.RemoveAt(pickIndex);
            }

            return result;
        }

        /// <summary>
        /// Generates the complete spawn list of enemies for a room level:
        /// 1. Rolls enemy count (Lv 1-5: 3-7, Lv 6-10: 4-10, Lv 11+: 5-15).
        /// 2. Selects allowed enemy pool (Lv 1-5: Reg 1-4, Lv 6-10: Reg 1-6, Lv 11+: Reg 1-8).
        /// 3. Draws enemies without duplicates until pool is exhausted, then refills.
        /// 4. Scales stats (every 3 levels completed: HP min/max +1, DMG min/max +1).
        /// 5. Rolls final HP and Damage within the scaled ranges.
        /// </summary>
        public List<RolledEnemyStats> GenerateRoomEnemies(int roomLevel)
        {
            int count = GetRandomEnemyCount(roomLevel);
            var allowed = GetAllowedEnemiesForRoom(roomLevel);
            var drawnEnemies = DrawEnemiesWithoutImmediateDuplicates(count, allowed);

            var result = new List<RolledEnemyStats>();
            foreach (var enemy in drawnEnemies)
            {
                result.Add(enemy.RollStats(roomLevel, levelsPerScalingStep, hpBonusPerStep, damageBonusPerStep));
            }

            return result;
        }

        /// <summary>
        /// Checks whether the given room level should spawn a Boss based on tier rules.
        /// </summary>
        public bool HasBossForRoom(int roomLevel)
        {
            if (collectionType != EnemyType.Boss) return false;
            if (roomLevel <= 21)
            {
                return GetTierForRoom(roomLevel) != null;
            }
            // Room 22+: 1-room gap (even rooms: gap, odd rooms: boss)
            return roomLevel % 2 == 1;
        }

        /// <summary>
        /// Creates a persistent stateful deck that maintains draw bag state across multiple calls.
        /// </summary>
        public EnemyDeck CreateDeck()
        {
            return new EnemyDeck(this);
        }

        /// <summary>
        /// Creates a persistent stateful boss deck that adheres to room tier progression and endless shuffle bag rules.
        /// </summary>
        public BossDeck CreateBossDeck()
        {
            return new BossDeck(this);
        }
    }

    [System.Serializable]
    public class RoomSpawnTier
    {
        [Tooltip("Identifier name for this tier")]
        public string tierName = "Tier 1 (Lv 1 - 5)";

        [Header("Room Level Range")]
        [Tooltip("Minimum room level for this tier (inclusive)")]
        [Min(1)] public int minRoomLevel = 1;

        [Tooltip("Maximum room level for this tier (inclusive). Set to -1 for endless / infinite.")]
        public int maxRoomLevel = 5;

        [Header("Enemy Count Limits (Random Amount)")]
        [Tooltip("Minimum enemies to spawn in this room")]
        [Min(1)] public int minEnemyCount = 3;

        [Tooltip("Maximum enemies to spawn in this room")]
        [Min(1)] public int maxEnemyCount = 7;

        [Header("Allowed Enemy Pool (1-based Index)")]
        [Tooltip("Start enemy number (e.g. 1 for Reg 1)")]
        [Min(1)] public int minEnemyNumber = 1;

        [Tooltip("End enemy number (e.g. 4 for Reg 4)")]
        [Min(1)] public int maxEnemyNumber = 4;

        public bool MatchesRoomLevel(int roomLevel)
        {
            if (roomLevel < minRoomLevel) return false;
            if (maxRoomLevel > 0 && roomLevel > maxRoomLevel) return false;
            return true;
        }

        public int RollEnemyCount()
        {
            int min = Mathf.Min(minEnemyCount, maxEnemyCount);
            int max = Mathf.Max(minEnemyCount, maxEnemyCount);
            return Random.Range(min, max + 1);
        }
    }

    /// <summary>
    /// Persistent deck for drawing enemies without duplicates across continuous spawns.
    /// Refills automatically when exhausted.
    /// </summary>
    public class EnemyDeck
    {
        private readonly EnemyCollection collection;
        private readonly List<EnemyData> currentBag = new List<EnemyData>();
        private int lastRoomLevel = -1;
        private EnemyData lastPicked = null;

        public EnemyDeck(EnemyCollection col)
        {
            collection = col;
        }

        public EnemyData DrawNext(int roomLevel)
        {
            if (collection == null) return null;

            // If room level changed tier, refresh bag
            var allowed = collection.GetAllowedEnemiesForRoom(roomLevel);
            if (allowed == null || allowed.Count == 0) return null;

            if (currentBag.Count == 0 || lastRoomLevel != roomLevel)
            {
                currentBag.Clear();
                currentBag.AddRange(allowed);
                lastRoomLevel = roomLevel;
            }

            int index;
            if (currentBag.Count > 1 && lastPicked != null && currentBag.Contains(lastPicked))
            {
                do
                {
                    index = Random.Range(0, currentBag.Count);
                } while (currentBag.Count > 1 && currentBag[index] == lastPicked);
            }
            else
            {
                index = Random.Range(0, currentBag.Count);
            }

            EnemyData picked = currentBag[index];
            lastPicked = picked;
            currentBag.RemoveAt(index);
            return picked;
        }

        public void Reset()
        {
            currentBag.Clear();
            lastPicked = null;
            lastRoomLevel = -1;
        }
    }

    /// <summary>
    /// Persistent deck for drawing Boss enemies according to room tiers and rules:
    /// - Lv 2-3: Boss 1
    /// - Lv 5-6: Boss 2
    /// - Lv 8-9: Boss 3
    /// - Lv 11-12: Boss 4
    /// - Lv 14-15: Boss 5
    /// - Lv 17-18: Boss 6
    /// - Lv 20-21: Boss 7
    /// - Lv 22+: 1-room gap between bosses (odd levels: 23, 25, 27...), random Boss 1-7 without duplicate until all 7 are drawn, then refills from beginning.
    /// </summary>
    public class BossDeck
    {
        private readonly EnemyCollection bossCollection;
        private readonly List<EnemyData> currentBag = new List<EnemyData>();
        private EnemyData lastPicked = null;

        public BossDeck(EnemyCollection collection)
        {
            bossCollection = collection;
        }

        public bool HasBossForRoom(int roomLevel)
        {
            if (bossCollection == null || bossCollection.Count == 0) return false;
            return bossCollection.HasBossForRoom(roomLevel);
        }

        public EnemyData DrawBossForRoom(int roomLevel)
        {
            if (!HasBossForRoom(roomLevel)) return null;

            // Tiers 1-7 (Lv 1 - 21)
            if (roomLevel <= 21)
            {
                var tier = bossCollection.GetTierForRoom(roomLevel);
                if (tier == null) return null;
                int bossIndex = Mathf.Clamp(tier.minEnemyNumber - 1, 0, bossCollection.Count - 1);
                return bossCollection.GetEnemy(bossIndex);
            }

            // Endless Tier (Lv 22+): Draw from persistent bag without duplicate until exhausted
            if (currentBag.Count == 0)
            {
                currentBag.AddRange(bossCollection.enemies);
            }

            int pickIndex;
            if (currentBag.Count > 1 && lastPicked != null && currentBag.Contains(lastPicked))
            {
                do
                {
                    pickIndex = Random.Range(0, currentBag.Count);
                } while (currentBag.Count > 1 && currentBag[pickIndex] == lastPicked);
            }
            else
            {
                pickIndex = Random.Range(0, currentBag.Count);
            }

            EnemyData picked = currentBag[pickIndex];
            lastPicked = picked;
            currentBag.RemoveAt(pickIndex);
            return picked;
        }

        public void Reset()
        {
            currentBag.Clear();
            lastPicked = null;
        }
    }
}
