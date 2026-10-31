using System;
using UnityEngine;
using Spine.Unity;

namespace Digit7s.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Digit7s/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID identifier for this enemy")]
        public string enemyId = "enemy_reg_01";

        [Tooltip("Display name shown in UI and gameplay")]
        public string enemyName = "Enemy Reg 1";

        [Tooltip("Classification: Regular (ရိုးရိုး enemy) or Boss (Boss enemy)")]
        public EnemyType enemyType = EnemyType.Regular;

        [TextArea(2, 4)]
        [Tooltip("Description or lore notes for this enemy")]
        public string description;

        [Tooltip("UI Icon or portrait sprite")]
        public Sprite enemyIcon;

        [Header("Spine & Prefab Assets")]
        [Tooltip("Enemy prefab instantiated in the game scene")]
        public GameObject enemyPrefab;

        [Tooltip("Spine SkeletonDataAsset for live animation and preview")]
        public SkeletonDataAsset skeletonDataAsset;

        [Header("Spawn & Placement")]
        [Tooltip("Custom spawn offset relative to block center. If (0,0,0), automatically computed from skeletonDataAsset bounds.")]
        public Vector3 spawnOffset = Vector3.zero;

        [Header("Stat Ranges (Base Stats)")]
        [Tooltip("Base Minimum HP (ကျန်းမာရေး အနည်းဆုံး)")]
        [Min(1)] public int minHp = 1;

        [Tooltip("Base Maximum HP (ကျန်းမာရေး အများဆုံး)")]
        [Min(1)] public int maxHp = 3;

        [Tooltip("Base Minimum Damage (တိုက်ခိုက်မှု အနည်းဆုံး)")]
        [Min(0)] public int minDamage = 1;

        [Tooltip("Base Maximum Damage (တိုက်ခိုက်မှု အများဆုံး)")]
        [Min(0)] public int maxDamage = 1;

        [Header("Spine Animation Names")]
        public string idleAnimation = "Idle";
        public string walkAnimation = "Walking";
        public string attackAnimation = "Attack";
        public string deadAnimation = "Dead";

        public bool IsBoss => enemyType == EnemyType.Boss;

        /// <summary>
        /// Gets the spawn offset to center this enemy relative to a block or spawn point.
        /// If spawnOffset is manually configured, it is used. Otherwise, it calculates the offset from the Spine skeleton bounds.
        /// </summary>
        public Vector3 GetSpawnOffset()
        {
            if (spawnOffset != Vector3.zero)
            {
                return spawnOffset;
            }

            if (skeletonDataAsset != null)
            {
                try
                {
                    var skelData = skeletonDataAsset.GetSkeletonData(false);
                    if (skelData != null)
                    {
                        float scale = skeletonDataAsset.scale;
                        float skelCenterX = (skelData.X + (skelData.Width / 2f)) * scale;
                        float skelCenterY = (skelData.Y + (skelData.Height / 2f)) * scale;
                        return new Vector3(-skelCenterX, -skelCenterY, 0f);
                    }
                }
                catch (Exception)
                {
                    // Fallback if skeleton data cannot be read
                }
            }

            return IsBoss ? new Vector3(0f, -1.66f, 0f) : new Vector3(0f, -2.04f, 0f);
        }

        private void OnValidate()
        {
            if (maxHp < minHp) maxHp = minHp;
            if (maxDamage < minDamage) maxDamage = minDamage;
        }

        /// <summary>
        /// Calculates stat bonus based on completed room levels.
        /// Every 3 levels completed gives +1 (e.g. Lv 1-3: +0, Lv 4-6: +1, Lv 7-9: +2, Lv 10-12: +3...).
        /// </summary>
        public static int CalculateLevelBonus(int roomLevel, int stepLevels = 3, int bonusPerStep = 1)
        {
            if (roomLevel <= 1 || stepLevels <= 0) return 0;
            return ((roomLevel - 1) / stepLevels) * bonusPerStep;
        }

        /// <summary>
        /// Gets the scaled HP range for a specific room level.
        /// </summary>
        public void GetScaledHpRange(int roomLevel, out int scaledMinHp, out int scaledMaxHp, int stepLevels = 3, int bonusPerStep = 1)
        {
            int bonus = CalculateLevelBonus(roomLevel, stepLevels, bonusPerStep);
            scaledMinHp = minHp + bonus;
            scaledMaxHp = maxHp + bonus;
        }

        /// <summary>
        /// Gets the scaled Damage range for a specific room level.
        /// </summary>
        public void GetScaledDamageRange(int roomLevel, out int scaledMinDamage, out int scaledMaxDamage, int stepLevels = 3, int bonusPerStep = 1)
        {
            int bonus = CalculateLevelBonus(roomLevel, stepLevels, bonusPerStep);
            scaledMinDamage = minDamage + bonus;
            scaledMaxDamage = maxDamage + bonus;
        }

        /// <summary>
        /// Rolls an HP value uniformly within the scaled range for this room level.
        /// </summary>
        public int RollHp(int roomLevel, int stepLevels = 3, int bonusPerStep = 1)
        {
            GetScaledHpRange(roomLevel, out int sMin, out int sMax, stepLevels, bonusPerStep);
            return UnityEngine.Random.Range(sMin, sMax + 1);
        }

        /// <summary>
        /// Rolls a Damage value uniformly within the scaled range for this room level.
        /// </summary>
        public int RollDamage(int roomLevel, int stepLevels = 3, int bonusPerStep = 1)
        {
            GetScaledDamageRange(roomLevel, out int sMin, out int sMax, stepLevels, bonusPerStep);
            return UnityEngine.Random.Range(sMin, sMax + 1);
        }

        /// <summary>
        /// Generates a complete rolled runtime stats instance for this enemy at the specified room level.
        /// </summary>
        public RolledEnemyStats RollStats(int roomLevel, int stepLevels = 3, int hpBonusPerStep = 1, int dmgBonusPerStep = 1)
        {
            int bonus = CalculateLevelBonus(roomLevel, stepLevels, 1);
            int rolledHp = UnityEngine.Random.Range(minHp + (bonus * hpBonusPerStep), maxHp + (bonus * hpBonusPerStep) + 1);
            int rolledDmg = UnityEngine.Random.Range(minDamage + (bonus * dmgBonusPerStep), maxDamage + (bonus * dmgBonusPerStep) + 1);

            return new RolledEnemyStats
            {
                enemyData = this,
                roomLevel = roomLevel,
                levelBonus = bonus,
                health = rolledHp,
                maxHealth = rolledHp,
                damage = rolledDmg
            };
        }
    }

    [System.Serializable]
    public struct RolledEnemyStats
    {
        public EnemyData enemyData;
        public int roomLevel;
        public int levelBonus;
        public int health;
        public int maxHealth;
        public int damage;

        public override string ToString()
        {
            string name = enemyData != null ? enemyData.enemyName : "Enemy";
            return $"{name} [Lv.{roomLevel} (Bonus:+{levelBonus})] -> HP: {health}/{maxHealth}, DMG: {damage}";
        }
    }
}
