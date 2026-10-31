using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Spine.Unity;
using Digit7s.Enemies;

namespace Digit7s.Editor
{
    public static class EnemySystemSetup
    {
        private const string RootDataDir = "Assets/Data/Enemies";
        private const string RegularDataDir = "Assets/Data/Enemies/Regular";
        private const string BossDataDir = "Assets/Data/Enemies/Boss";

        private const string RegularCollectionPath = "Assets/Data/Enemies/EnemyCollection_Regular.asset";
        private const string BossCollectionPath = "Assets/Data/Enemies/EnemyCollection_Boss.asset";
        private const string EnemyDatabasePath = "Assets/Data/Enemies/EnemyDatabase.asset";

        [MenuItem("Digit7s/Generate Enemy ScriptableObjects & Collections")]
        public static void GenerateAllEnemyAssets()
        {
            EnsureDirectories();

            // 1. Generate 8 Regular Enemies
            var regularEnemies = GenerateRegularEnemies();

            // 2. Generate 7 Boss Enemies
            var bossEnemies = GenerateBossEnemies();

            // 3. Create or Update EnemyCollection_Regular
            var regCollection = CreateOrUpdateCollection(
                RegularCollectionPath,
                "Regular Enemies Collection (ရိုးရိုး enemy)",
                EnemyType.Regular,
                "Collection of all standard ground and aerial regular enemies encountered in levels.",
                regularEnemies
            );

            regCollection.roomTiers = new List<RoomSpawnTier>
            {
                new RoomSpawnTier
                {
                    tierName = "Tier 1 (Lv 1 - 5)",
                    minRoomLevel = 1,
                    maxRoomLevel = 5,
                    minEnemyCount = 3,
                    maxEnemyCount = 7,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 4
                },
                new RoomSpawnTier
                {
                    tierName = "Tier 2 (Lv 6 - 10)",
                    minRoomLevel = 6,
                    maxRoomLevel = 10,
                    minEnemyCount = 4,
                    maxEnemyCount = 10,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 6
                },
                new RoomSpawnTier
                {
                    tierName = "Tier 3 (Lv 11+)",
                    minRoomLevel = 11,
                    maxRoomLevel = -1,
                    minEnemyCount = 5,
                    maxEnemyCount = 15,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 8
                }
            };
            regCollection.levelsPerScalingStep = 3;
            regCollection.hpBonusPerStep = 1;
            regCollection.damageBonusPerStep = 1;
            EditorUtility.SetDirty(regCollection);

            // 4. Create or Update EnemyCollection_Boss
            var bossCollection = CreateOrUpdateCollection(
                BossCollectionPath,
                "Boss Enemies Collection (Boss enemy)",
                EnemyType.Boss,
                "Collection of all boss tier enemies featuring high durability and special attack phases.",
                bossEnemies
            );
            bossCollection.roomTiers = new List<RoomSpawnTier>
            {
                new RoomSpawnTier
                {
                    tierName = "Boss Tier 1 (Lv 2 - 3)",
                    minRoomLevel = 2,
                    maxRoomLevel = 3,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 1
                },
                new RoomSpawnTier
                {
                    tierName = "Boss Tier 2 (Lv 5 - 6)",
                    minRoomLevel = 5,
                    maxRoomLevel = 6,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 2,
                    maxEnemyNumber = 2
                },
                new RoomSpawnTier
                {
                    tierName = "Boss Tier 3 (Lv 8 - 9)",
                    minRoomLevel = 8,
                    maxRoomLevel = 9,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 3,
                    maxEnemyNumber = 3
                },
                new RoomSpawnTier
                {
                    tierName = "Boss Tier 4 (Lv 11 - 12)",
                    minRoomLevel = 11,
                    maxRoomLevel = 12,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 4,
                    maxEnemyNumber = 4
                },
                new RoomSpawnTier
                {
                    tierName = "Boss Tier 5 (Lv 14 - 15)",
                    minRoomLevel = 14,
                    maxRoomLevel = 15,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 5,
                    maxEnemyNumber = 5
                },
                new RoomSpawnTier
                {
                    tierName = "Boss Tier 6 (Lv 17 - 18)",
                    minRoomLevel = 17,
                    maxRoomLevel = 18,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 6,
                    maxEnemyNumber = 6
                },
                new RoomSpawnTier
                {
                    tierName = "Boss Tier 7 (Lv 20 - 21)",
                    minRoomLevel = 20,
                    maxRoomLevel = 21,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 7,
                    maxEnemyNumber = 7
                },
                new RoomSpawnTier
                {
                    tierName = "Boss Tier Endless (Lv 22+)",
                    minRoomLevel = 22,
                    maxRoomLevel = -1,
                    minEnemyCount = 1,
                    maxEnemyCount = 1,
                    minEnemyNumber = 1,
                    maxEnemyNumber = 7
                }
            };
            bossCollection.levelsPerScalingStep = 3;
            bossCollection.hpBonusPerStep = 1;
            bossCollection.damageBonusPerStep = 1;
            EditorUtility.SetDirty(bossCollection);

            // 5. Create or Update central EnemyDatabase
            CreateOrUpdateDatabase(regCollection, bossCollection);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EnemySystemSetup] Complete! Created {regularEnemies.Count} Regular Enemies, {bossEnemies.Count} Boss Enemies, 2 Collections, and EnemyDatabase.");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(RootDataDir)) Directory.CreateDirectory(RootDataDir);
            if (!Directory.Exists(RegularDataDir)) Directory.CreateDirectory(RegularDataDir);
            if (!Directory.Exists(BossDataDir)) Directory.CreateDirectory(BossDataDir);
        }

        private static List<EnemyData> GenerateRegularEnemies()
        {
            var list = new List<EnemyData>();
            int count = 8;

            // User-specified stats table:
            // Reg 1: HP 1-3, DMG 1-1
            // Reg 2: HP 1-5, DMG 1-2
            // Reg 3: HP 3-6, DMG 1-3
            // Reg 4: HP 4-7, DMG 1-2
            // Reg 5: HP 5-7, DMG 1-6
            // Reg 6: HP 5-6, DMG 2-4
            // Reg 7: HP 2-5, DMG 3-7
            // Reg 8: HP 7-9, DMG 2-6
            int[] minHps =     { 1, 1, 3, 4, 5, 5, 2, 7 };
            int[] maxHps =     { 3, 5, 6, 7, 7, 6, 5, 9 };
            int[] minDamages = { 1, 1, 1, 1, 1, 2, 3, 2 };
            int[] maxDamages = { 1, 2, 3, 2, 6, 4, 7, 6 };

            for (int i = 1; i <= count; i++)
            {
                string assetPath = $"{RegularDataDir}/Enemy_Reg_{i:D2}.asset";
                var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
                if (enemyData == null)
                {
                    enemyData = ScriptableObject.CreateInstance<EnemyData>();
                    AssetDatabase.CreateAsset(enemyData, assetPath);
                }

                enemyData.enemyId = $"enemy_reg_{i:D2}";
                enemyData.enemyName = $"Enemy Reg {i}";
                enemyData.enemyType = EnemyType.Regular;
                enemyData.description = $"Regular enemy variant #{i}. Standard combat unit equipped with melee and chasing behaviors.";

                // Skeleton & Prefab
                string spinePath = $"Assets/Json Atlas/Enemies/Enemy Reg {i}/Enemy_SkeletonData.asset";
                string prefabPath = $"Assets/Prefabs/Spine Characters/Enemies/Enemy Reg {i}/Enemy.prefab";

                enemyData.skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(spinePath);
                enemyData.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                // Stats based on specification
                int idx = i - 1;
                enemyData.minHp = minHps[idx];
                enemyData.maxHp = maxHps[idx];
                enemyData.minDamage = minDamages[idx];
                enemyData.maxDamage = maxDamages[idx];

                // Spine Animations
                enemyData.idleAnimation = "Idle";
                enemyData.walkAnimation = "Walking";
                enemyData.attackAnimation = "Attack";
                enemyData.deadAnimation = "Dead";

                EditorUtility.SetDirty(enemyData);
                list.Add(enemyData);
            }

            return list;
        }

        private static List<EnemyData> GenerateBossEnemies()
        {
            var list = new List<EnemyData>();
            int count = 7;

            for (int i = 1; i <= count; i++)
            {
                string assetPath = $"{BossDataDir}/Enemy_Boss_{i:D2}.asset";
                var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
                if (enemyData == null)
                {
                    enemyData = ScriptableObject.CreateInstance<EnemyData>();
                    AssetDatabase.CreateAsset(enemyData, assetPath);
                }

                enemyData.enemyId = $"enemy_boss_{i:D2}";
                enemyData.enemyName = $"Enemy Boss {i}";
                enemyData.enemyType = EnemyType.Boss;
                enemyData.description = $"Boss enemy variant #{i}. Elite heavy encounter with formidable health pools and high-damage attacks.";

                // Skeleton & Prefab
                string spinePath = $"Assets/Json Atlas/Enemies/Enemy Boss {i}/Enemy_SkeletonData.asset";
                string prefabPath = $"Assets/Prefabs/Spine Characters/Enemies/Enemy Boss {i}/Enemy.prefab";

                enemyData.skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(spinePath);
                enemyData.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                // Default Boss Stat Ranges if uninitialized
                if (enemyData.minHp <= 1 && enemyData.maxHp <= 3)
                {
                    enemyData.minHp = 15 + (i * 2);
                    enemyData.maxHp = 25 + (i * 3);
                    enemyData.minDamage = 2 + i;
                    enemyData.maxDamage = 5 + (i * 2);
                }

                // Spine Animations (Boss Spine uses "Walk" instead of "Walking")
                enemyData.idleAnimation = "Idle";
                enemyData.walkAnimation = "Walk";
                enemyData.attackAnimation = "Attack";
                enemyData.deadAnimation = "Dead";

                EditorUtility.SetDirty(enemyData);
                list.Add(enemyData);
            }

            return list;
        }

        private static EnemyCollection CreateOrUpdateCollection(string path, string title, EnemyType type, string description, List<EnemyData> enemies)
        {
            var collection = AssetDatabase.LoadAssetAtPath<EnemyCollection>(path);
            if (collection == null)
            {
                collection = ScriptableObject.CreateInstance<EnemyCollection>();
                AssetDatabase.CreateAsset(collection, path);
            }

            collection.collectionName = title;
            collection.collectionType = type;
            collection.description = description;
            collection.enemies = new List<EnemyData>(enemies);

            EditorUtility.SetDirty(collection);
            return collection;
        }

        private static EnemyDatabase CreateOrUpdateDatabase(EnemyCollection regularCollection, EnemyCollection bossCollection)
        {
            var db = AssetDatabase.LoadAssetAtPath<EnemyDatabase>(EnemyDatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<EnemyDatabase>();
                AssetDatabase.CreateAsset(db, EnemyDatabasePath);
            }

            db.regularEnemiesCollection = regularCollection;
            db.bossEnemiesCollection = bossCollection;

            EditorUtility.SetDirty(db);
            return db;
        }
    }
}
