using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Digit7s.Enemies;

namespace Digit7s.Editor
{
    public static class EnemySystemTests
    {
        [MenuItem("Digit7s/Tests/Run Enemy Spawner Verification Tests")]
        public static void RunAllTests()
        {
            Debug.Log("<color=cyan>=== [EnemySystemTests] STARTING VERIFICATION ===</color>");

            // First ensure assets are generated/up-to-date
            EnemySystemSetup.GenerateAllEnemyAssets();

            var regCollection = AssetDatabase.LoadAssetAtPath<EnemyCollection>("Assets/Data/Enemies/EnemyCollection_Regular.asset");
            if (regCollection == null)
            {
                Debug.LogError("[EnemySystemTests] FAIL: EnemyCollection_Regular.asset not found!");
                return;
            }

            int passed = 0;
            int total = 0;

            // Test 1: Check Base Stats for Reg 1 - 8
            total++;
            if (TestBaseStats(regCollection)) passed++;

            // Test 2: Check Level Scaling (+1 every 3 completed levels)
            total++;
            if (TestLevelScaling(regCollection)) passed++;

            // Test 3: Check Room Limits and Pools (1-5, 6-10, 11+)
            total++;
            if (TestRoomLimitsAndPools(regCollection)) passed++;

            // Test 4: Check No Duplicate Draw / Refill Bag Rule
            total++;
            if (TestNoDuplicateBagRefill(regCollection)) passed++;

            // Test 5: Check Room Generation Execution
            total++;
            if (TestRoomGenerationExecution(regCollection)) passed++;

            // Test 6: Check Block Enemy Distribution and Spawning on Break
            total++;
            if (TestBlockEnemySpawning(regCollection)) passed++;

            // Test 7: Check Boss Room Tiers & Endless Shuffle Bag
            total++;
            var bossCollection = AssetDatabase.LoadAssetAtPath<EnemyCollection>("Assets/Data/Enemies/EnemyCollection_Boss.asset");
            if (TestBossTiersAndEndlessBag(bossCollection)) passed++;

            if (passed == total)
            {
                Debug.Log($"<color=green>=== [EnemySystemTests] ALL {total}/{total} TESTS PASSED! ===</color>");
            }
            else
            {
                Debug.LogError($"[EnemySystemTests] TESTS FAILED: {passed}/{total} passed.");
            }
        }

        private static bool TestBaseStats(EnemyCollection collection)
        {
            int[] expectedMinHp =     { 1, 1, 3, 4, 5, 5, 2, 7 };
            int[] expectedMaxHp =     { 3, 5, 6, 7, 7, 6, 5, 9 };
            int[] expectedMinDamage = { 1, 1, 1, 1, 1, 2, 3, 2 };
            int[] expectedMaxDamage = { 1, 2, 3, 2, 6, 4, 7, 6 };

            for (int i = 0; i < 8; i++)
            {
                var enemy = collection.GetEnemy(i);
                if (enemy == null)
                {
                    Debug.LogError($"[TestBaseStats] Reg {i + 1} is null!");
                    return false;
                }

                if (enemy.minHp != expectedMinHp[i] || enemy.maxHp != expectedMaxHp[i] ||
                    enemy.minDamage != expectedMinDamage[i] || enemy.maxDamage != expectedMaxDamage[i])
                {
                    Debug.LogError($"[TestBaseStats] Mismatch on Reg {i + 1}: expected HP {expectedMinHp[i]}-{expectedMaxHp[i]}, DMG {expectedMinDamage[i]}-{expectedMaxDamage[i]}, but got HP {enemy.minHp}-{enemy.maxHp}, DMG {enemy.minDamage}-{enemy.maxDamage}");
                    return false;
                }
            }

            Debug.Log("[Test 1 PASS] Base stats for Enemy Reg 1 to 8 match user specifications exactly.");
            return true;
        }

        private static bool TestLevelScaling(EnemyCollection collection)
        {
            // Level 1-3: bonus = 0
            // Level 4-6: bonus = 1
            // Level 7-9: bonus = 2
            // Level 10-12: bonus = 3
            if (EnemyData.CalculateLevelBonus(1) != 0 ||
                EnemyData.CalculateLevelBonus(2) != 0 ||
                EnemyData.CalculateLevelBonus(3) != 0)
            {
                Debug.LogError("[TestLevelScaling] Bonus for Lv 1-3 should be 0!");
                return false;
            }

            if (EnemyData.CalculateLevelBonus(4) != 1 ||
                EnemyData.CalculateLevelBonus(5) != 1 ||
                EnemyData.CalculateLevelBonus(6) != 1)
            {
                Debug.LogError("[TestLevelScaling] Bonus for Lv 4-6 should be 1!");
                return false;
            }

            if (EnemyData.CalculateLevelBonus(7) != 2 || EnemyData.CalculateLevelBonus(9) != 2)
            {
                Debug.LogError("[TestLevelScaling] Bonus for Lv 7-9 should be 2!");
                return false;
            }

            if (EnemyData.CalculateLevelBonus(10) != 3 || EnemyData.CalculateLevelBonus(12) != 3)
            {
                Debug.LogError("[TestLevelScaling] Bonus for Lv 10-12 should be 3!");
                return false;
            }

            // Check scaled ranges for Reg 1 at Level 4 (bonus +1)
            var reg1 = collection.GetEnemy(0); // Reg 1: Base HP 1-3, DMG 1-1
            reg1.GetScaledHpRange(4, out int minHp, out int maxHp);
            reg1.GetScaledDamageRange(4, out int minDmg, out int maxDmg);

            if (minHp != 2 || maxHp != 4 || minDmg != 2 || maxDmg != 2)
            {
                Debug.LogError($"[TestLevelScaling] Reg 1 at Lv 4 expected HP 2-4, DMG 2-2; got HP {minHp}-{maxHp}, DMG {minDmg}-{maxDmg}");
                return false;
            }

            // Check scaled ranges for Reg 8 at Level 10 (bonus +3)
            var reg8 = collection.GetEnemy(7); // Reg 8: Base HP 7-9, DMG 2-6
            reg8.GetScaledHpRange(10, out minHp, out maxHp);
            reg8.GetScaledDamageRange(10, out minDmg, out maxDmg);

            if (minHp != 10 || maxHp != 12 || minDmg != 5 || maxDmg != 9)
            {
                Debug.LogError($"[TestLevelScaling] Reg 8 at Lv 10 expected HP 10-12, DMG 5-9; got HP {minHp}-{maxHp}, DMG {minDmg}-{maxDmg}");
                return false;
            }

            Debug.Log("[Test 2 PASS] Level scaling (+1 every 3 completed levels) works correctly for all tested levels.");
            return true;
        }

        private static bool TestRoomLimitsAndPools(EnemyCollection collection)
        {
            // Level 1 - 5: 3 - 7 count, Reg 1 - 4
            var tier1 = collection.GetTierForRoom(1);
            if (tier1.minEnemyCount != 3 || tier1.maxEnemyCount != 7 || tier1.minEnemyNumber != 1 || tier1.maxEnemyNumber != 4)
            {
                Debug.LogError("[TestRoomLimitsAndPools] Tier 1 configuration mismatch!");
                return false;
            }
            var pool1 = collection.GetAllowedEnemiesForRoom(3);
            if (pool1.Count != 4)
            {
                Debug.LogError($"[TestRoomLimitsAndPools] Pool for Lv 3 should have 4 enemies, got {pool1.Count}");
                return false;
            }

            // Level 6 - 10: 4 - 10 count, Reg 1 - 6
            var tier2 = collection.GetTierForRoom(6);
            if (tier2.minEnemyCount != 4 || tier2.maxEnemyCount != 10 || tier2.minEnemyNumber != 1 || tier2.maxEnemyNumber != 6)
            {
                Debug.LogError("[TestRoomLimitsAndPools] Tier 2 configuration mismatch!");
                return false;
            }
            var pool2 = collection.GetAllowedEnemiesForRoom(8);
            if (pool2.Count != 6)
            {
                Debug.LogError($"[TestRoomLimitsAndPools] Pool for Lv 8 should have 6 enemies, got {pool2.Count}");
                return false;
            }

            // Level 11+: 5 - 15 count, Reg 1 - 8
            var tier3 = collection.GetTierForRoom(15);
            if (tier3.minEnemyCount != 5 || tier3.maxEnemyCount != 15 || tier3.minEnemyNumber != 1 || tier3.maxEnemyNumber != 8)
            {
                Debug.LogError("[TestRoomLimitsAndPools] Tier 3 configuration mismatch!");
                return false;
            }
            var pool3 = collection.GetAllowedEnemiesForRoom(20);
            if (pool3.Count != 8)
            {
                Debug.LogError($"[TestRoomLimitsAndPools] Pool for Lv 20 should have 8 enemies, got {pool3.Count}");
                return false;
            }

            Debug.Log("[Test 3 PASS] Room limits and allowed pools (Lv 1-5: 3-7 Reg 1-4, Lv 6-10: 4-10 Reg 1-6, Lv 11+: 5-15 Reg 1-8) verified.");
            return true;
        }

        private static bool TestNoDuplicateBagRefill(EnemyCollection collection)
        {
            var allowed = collection.GetAllowedEnemiesForRoom(1); // Reg 1 - 4 (size 4)

            // Test 100 simulation draws with count = 4
            // Each set of 4 drawn from a pool of 4 MUST be completely unique!
            for (int sim = 0; sim < 50; sim++)
            {
                var drawn = collection.DrawEnemiesWithoutImmediateDuplicates(4, allowed);
                var set = new HashSet<EnemyData>(drawn);
                if (set.Count != 4)
                {
                    Debug.LogError($"[TestNoDuplicateBagRefill] Expected 4 distinct enemies in a 4-draw, but got {set.Count} unique!");
                    return false;
                }
            }

            // Test draw of 7 enemies from pool of 4:
            // First 4 must be completely unique (exhausts pool)
            // Next 3 must be drawn from the refilled pool
            for (int sim = 0; sim < 50; sim++)
            {
                var drawn = collection.DrawEnemiesWithoutImmediateDuplicates(7, allowed);
                var first4 = new HashSet<EnemyData>(drawn.GetRange(0, 4));
                if (first4.Count != 4)
                {
                    Debug.LogError("[TestNoDuplicateBagRefill] First 4 draws did not contain all 4 distinct pool members!");
                    return false;
                }

                var next3 = new HashSet<EnemyData>(drawn.GetRange(4, 3));
                if (next3.Count != 3)
                {
                    Debug.LogError("[TestNoDuplicateBagRefill] Refilled 3 draws contained duplicates among themselves!");
                    return false;
                }
            }

            Debug.Log("[Test 4 PASS] No duplicate rule & automatic bag refill verified with 100 simulations.");
            return true;
        }

        private static bool TestRoomGenerationExecution(EnemyCollection collection)
        {
            for (int level = 1; level <= 15; level++)
            {
                var generated = collection.GenerateRoomEnemies(level);
                var tier = collection.GetTierForRoom(level);

                if (generated.Count < tier.minEnemyCount || generated.Count > tier.maxEnemyCount)
                {
                    Debug.LogError($"[TestRoomGenerationExecution] Generated count {generated.Count} out of bounds [{tier.minEnemyCount}, {tier.maxEnemyCount}] for Lv {level}!");
                    return false;
                }

                int bonus = EnemyData.CalculateLevelBonus(level);
                foreach (var stats in generated)
                {
                    if (stats.levelBonus != bonus)
                    {
                        Debug.LogError($"[TestRoomGenerationExecution] Level bonus mismatch for Lv {level}!");
                        return false;
                    }

                    if (stats.health < stats.enemyData.minHp + bonus || stats.health > stats.enemyData.maxHp + bonus)
                    {
                        Debug.LogError($"[TestRoomGenerationExecution] Rolled HP {stats.health} out of range for {stats.enemyData.enemyName} at Lv {level}!");
                        return false;
                    }

                    if (stats.damage < stats.enemyData.minDamage + bonus || stats.damage > stats.enemyData.maxDamage + bonus)
                    {
                        Debug.LogError($"[TestRoomGenerationExecution] Rolled DMG {stats.damage} out of range for {stats.enemyData.enemyName} at Lv {level}!");
                        return false;
                    }
                }
            }

            Debug.Log("[Test 5 PASS] GenerateRoomEnemies successfully executed and validated across Room Levels 1 through 15.");
            return true;
        }

        private static bool TestBlockEnemySpawning(EnemyCollection collection)
        {
            var rootObj = new GameObject("TempTest_BlockContainer");
            var spawnerObj = new GameObject("TempTest_Spawner");
            var spawner = spawnerObj.AddComponent<Digit7s.Gameplay.EnemySpawner>();

            var spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("regularEnemyCollection").objectReferenceValue = collection;
            spawnerSO.FindProperty("blockContainer").objectReferenceValue = rootObj.transform;
            spawnerSO.ApplyModifiedProperties();

            // Create 10 dummy blocks
            var blocks = new List<Digit7s.Gameplay.DestructibleBlock>();
            for (int i = 0; i < 10; i++)
            {
                var bObj = new GameObject($"DummyBlock_{i}");
                bObj.transform.SetParent(rootObj.transform);
                bObj.AddComponent<BoxCollider2D>();
                var dBlock = bObj.AddComponent<Digit7s.Gameplay.DestructibleBlock>();
                blocks.Add(dBlock);
            }

            // Distribute for Room Level 1 (Count should be between 3 and 7)
            spawner.DistributeEnemiesToBlocks(1);

            int assignedCount = 0;
            foreach (var b in blocks)
            {
                if (b.HasAssignedEnemy)
                {
                    assignedCount++;
                    // Check pool: Lv 1 must be within Reg 1 - 4
                    var id = b.AssignedEnemyStats.enemyData.enemyId;
                    if (id != "enemy_reg_01" && id != "enemy_reg_02" && id != "enemy_reg_03" && id != "enemy_reg_04")
                    {
                        Debug.LogError($"[TestBlockEnemySpawning] Block has invalid enemy {id} for Level 1!");
                        Object.DestroyImmediate(rootObj);
                        Object.DestroyImmediate(spawnerObj);
                        return false;
                    }
                }
            }

            if (assignedCount < 3 || assignedCount > 7)
            {
                Debug.LogError($"[TestBlockEnemySpawning] Assigned blocks count {assignedCount} out of [3, 7] range!");
                Object.DestroyImmediate(rootObj);
                Object.DestroyImmediate(spawnerObj);
                return false;
            }

            // Clean up
            Object.DestroyImmediate(rootObj);
            Object.DestroyImmediate(spawnerObj);

            // Test SpawnOnePerBlockUntilLimit mode (1 enemy per block click until quota)
            var spawnerObj2 = new GameObject("TempTest_Spawner2");
            var spawner2 = spawnerObj2.AddComponent<Digit7s.Gameplay.EnemySpawner>();
            var sSO2 = new SerializedObject(spawner2);
            sSO2.FindProperty("regularEnemyCollection").objectReferenceValue = collection;
            sSO2.FindProperty("blockSpawnMode").enumValueIndex = (int)Digit7s.Gameplay.BlockSpawnMode.SpawnOnePerBlockUntilLimit;
            sSO2.ApplyModifiedProperties();

            spawner2.InitializeRoom(1);
            int quota = spawner2.CurrentRoomTargetQuota;
            if (quota < 3 || quota > 7)
            {
                Debug.LogError($"[TestBlockEnemySpawning] SpawnOnePerBlockUntilLimit quota {quota} not in [3, 7]!");
                Object.DestroyImmediate(spawnerObj2);
                return false;
            }

            // Simulate clicking 10 blocks (quota is 3-7, so it should stop spawning once quota is reached)
            for (int i = 0; i < 10; i++)
            {
                var dummyBlockObj = new GameObject($"DummyTestBlock_{i}");
                dummyBlockObj.AddComponent<BoxCollider2D>();
                var dBlock = dummyBlockObj.AddComponent<Digit7s.Gameplay.DestructibleBlock>();
                dBlock.BreakBlock();
                if (dummyBlockObj != null)
                {
                    Object.DestroyImmediate(dummyBlockObj);
                }
            }

            if (spawner2.EnemiesSpawnedFromClicks != quota)
            {
                Debug.LogError($"[TestBlockEnemySpawning] Expected exactly {quota} enemies spawned from block clicks, got {spawner2.EnemiesSpawnedFromClicks}!");
                spawner2.UnregisterEvents();
                Object.DestroyImmediate(spawnerObj2);
                return false;
            }

            spawner2.ClearActiveEnemies();
            spawner2.UnregisterEvents();
            Object.DestroyImmediate(spawnerObj2);

            Debug.Log($"[Test 6 PASS] Block Enemy Distribution & SpawnOnePerBlockUntilLimit verified (Quota {quota} stopped cleanly after quota met).");
            return true;
        }

        private static bool TestBossTiersAndEndlessBag(EnemyCollection bossCollection)
        {
            if (bossCollection == null || bossCollection.Count != 7)
            {
                Debug.LogError("[TestBossTiersAndEndlessBag] Boss collection null or does not have 7 bosses!");
                return false;
            }

            var deck = bossCollection.CreateBossDeck();

            // 1. Verify fixed tiers:
            // Lv 1: no boss
            if (deck.DrawBossForRoom(1) != null) { Debug.LogError("Room 1 should not have a boss!"); return false; }
            // Lv 2-3: Boss 1
            if (deck.DrawBossForRoom(2)?.enemyId != "enemy_boss_01") { Debug.LogError("Room 2 should spawn Boss 1!"); return false; }
            if (deck.DrawBossForRoom(3)?.enemyId != "enemy_boss_01") { Debug.LogError("Room 3 should spawn Boss 1!"); return false; }
            // Lv 4: no boss
            if (deck.DrawBossForRoom(4) != null) { Debug.LogError("Room 4 should not have a boss!"); return false; }
            // Lv 5-6: Boss 2
            if (deck.DrawBossForRoom(5)?.enemyId != "enemy_boss_02") { Debug.LogError("Room 5 should spawn Boss 2!"); return false; }
            if (deck.DrawBossForRoom(6)?.enemyId != "enemy_boss_02") { Debug.LogError("Room 6 should spawn Boss 2!"); return false; }
            // Lv 7: no boss
            if (deck.DrawBossForRoom(7) != null) { Debug.LogError("Room 7 should not have a boss!"); return false; }
            // Lv 8-9: Boss 3
            if (deck.DrawBossForRoom(8)?.enemyId != "enemy_boss_03") { Debug.LogError("Room 8 should spawn Boss 3!"); return false; }
            if (deck.DrawBossForRoom(9)?.enemyId != "enemy_boss_03") { Debug.LogError("Room 9 should spawn Boss 3!"); return false; }
            // Lv 11-12: Boss 4
            if (deck.DrawBossForRoom(11)?.enemyId != "enemy_boss_04") { Debug.LogError("Room 11 should spawn Boss 4!"); return false; }
            if (deck.DrawBossForRoom(12)?.enemyId != "enemy_boss_04") { Debug.LogError("Room 12 should spawn Boss 4!"); return false; }
            // Lv 14-15: Boss 5
            if (deck.DrawBossForRoom(14)?.enemyId != "enemy_boss_05") { Debug.LogError("Room 14 should spawn Boss 5!"); return false; }
            if (deck.DrawBossForRoom(15)?.enemyId != "enemy_boss_05") { Debug.LogError("Room 15 should spawn Boss 5!"); return false; }
            // Lv 17-18: Boss 6
            if (deck.DrawBossForRoom(17)?.enemyId != "enemy_boss_06") { Debug.LogError("Room 17 should spawn Boss 6!"); return false; }
            if (deck.DrawBossForRoom(18)?.enemyId != "enemy_boss_06") { Debug.LogError("Room 18 should spawn Boss 6!"); return false; }
            // Lv 20-21: Boss 7
            if (deck.DrawBossForRoom(20)?.enemyId != "enemy_boss_07") { Debug.LogError("Room 20 should spawn Boss 7!"); return false; }
            if (deck.DrawBossForRoom(21)?.enemyId != "enemy_boss_07") { Debug.LogError("Room 21 should spawn Boss 7!"); return false; }

            // 2. Room 22: gap of 1 room (no boss)
            if (deck.DrawBossForRoom(22) != null) { Debug.LogError("Room 22 should not have a boss!"); return false; }

            // 3. Endless Tier (Lv 22+): 1-room gap and no duplicate until all 7 are drawn
            var drawnBosses = new HashSet<string>();
            for (int lvl = 23; lvl <= 35; lvl += 2)
            {
                // Odd room: must spawn a boss
                var b = deck.DrawBossForRoom(lvl);
                if (b == null) { Debug.LogError($"Room {lvl} must spawn a boss!"); return false; }
                if (drawnBosses.Contains(b.enemyId)) { Debug.LogError($"Boss {b.enemyId} repeated before all 7 were drawn!"); return false; }
                drawnBosses.Add(b.enemyId);

                // Even room: must be 1-room gap
                if (lvl + 1 <= 36 && deck.DrawBossForRoom(lvl + 1) != null)
                {
                    Debug.LogError($"Room {lvl + 1} must be gap (no boss)!");
                    return false;
                }
            }

            if (drawnBosses.Count != 7)
            {
                Debug.LogError($"Expected all 7 unique bosses to be drawn in 7 boss rooms, got {drawnBosses.Count}!");
                return false;
            }

            // Room 37: Bag should have refilled from beginning
            var refillBoss = deck.DrawBossForRoom(37);
            if (refillBoss == null) { Debug.LogError("Room 37 should draw from refilled bag!"); return false; }

            // 4. Test stat scaling on Boss (every 3 levels gives +1 HP and DMG)
            var bStatsLv2 = bossCollection.GetEnemy(0).RollStats(2, 3, 1, 1);
            var bStatsLv5 = bossCollection.GetEnemy(0).RollStats(5, 3, 1, 1);
            var bStatsLv8 = bossCollection.GetEnemy(0).RollStats(8, 3, 1, 1);
            if (bStatsLv2.levelBonus != 0 || bStatsLv5.levelBonus != 1 || bStatsLv8.levelBonus != 2)
            {
                Debug.LogError($"Boss level bonus calculation incorrect! Lv2:{bStatsLv2.levelBonus}, Lv5:{bStatsLv5.levelBonus}, Lv8:{bStatsLv8.levelBonus}");
                return false;
            }

            Debug.Log("[Test 7 PASS] Boss Room Tiers (Lv 2-3 Boss 1 .. Lv 20-21 Boss 7), 1-room gaps, endless shuffle bag (all 7 drawn with zero duplicates, then refill), and 3-level stat scaling fully verified!");
            return true;
        }
    }
}
