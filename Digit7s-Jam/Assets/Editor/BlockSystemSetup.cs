using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Digit7s.Gameplay;

namespace Digit7s.Editor
{
    public static class BlockSystemSetup
    {
        private const string BlockPrefabPath = "Assets/Prefabs/Block.prefab";
        private const string LevelScenePath = "Assets/Scenes/level.unity";

        [MenuItem("Digit7s/Setup Destructible Blocks in Level")]
        public static void SetupAll()
        {
            // 1. Configure Block.prefab
            SetupBlockPrefab();

            // 2. Configure level.unity scene
            SetupLevelBlocks();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BlockSystemSetup] Complete! Destructible Blocks and BlockInputController have been configured.");
        }

        public static void SetupBlockPrefab()
        {
            var prefabObj = PrefabUtility.LoadPrefabContents(BlockPrefabPath);
            if (prefabObj == null)
            {
                Debug.LogError($"[BlockSystemSetup] Failed to load prefab contents at {BlockPrefabPath}");
                return;
            }

            // Ensure BoxCollider2D
            var boxCol = prefabObj.GetComponent<BoxCollider2D>();
            if (boxCol == null)
            {
                boxCol = prefabObj.AddComponent<BoxCollider2D>();
            }

            var sr = prefabObj.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                boxCol.size = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
                boxCol.offset = Vector2.zero;
            }
            else
            {
                boxCol.size = new Vector2(1.2f, 1.24f);
            }

            // Ensure DestructibleBlock
            var destructible = prefabObj.GetComponent<DestructibleBlock>();
            if (destructible == null)
            {
                destructible = prefabObj.AddComponent<DestructibleBlock>();
            }
            var destSO = new SerializedObject(destructible);
            var expScaleProp = destSO.FindProperty("explosionScale");
            if (expScaleProp != null)
            {
                expScaleProp.floatValue = 0.7f;
                destSO.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabObj, BlockPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabObj);

            Debug.Log("[BlockSystemSetup] Successfully updated Block.prefab with BoxCollider2D and DestructibleBlock!");
        }

        public static void SetupLevelBlocks()
        {
            var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

            // Find or setup Block_Collection with BlockInputController
            var blockCollectionObj = GameObject.Find("Block_Collection");
            if (blockCollectionObj != null)
            {
                var inputCtrl = blockCollectionObj.GetComponent<BlockInputController>();
                if (inputCtrl == null)
                {
                    inputCtrl = blockCollectionObj.AddComponent<BlockInputController>();
                }
            }
            else
            {
                // If not found, create BlockInputController GameObject
                var controllerObj = GameObject.Find("BlockInputController");
                if (controllerObj == null)
                {
                    controllerObj = new GameObject("BlockInputController");
                }
                var inputCtrl = controllerObj.GetComponent<BlockInputController>();
                if (inputCtrl == null)
                {
                    inputCtrl = controllerObj.AddComponent<BlockInputController>();
                }
            }

            // Ensure all Blocks under Block_Collection or in scene have BoxCollider2D and DestructibleBlock
            var allBlocks = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int blockCount = 0;
            foreach (var renderer in allBlocks)
            {
                if (renderer.gameObject.name.StartsWith("Block"))
                {
                    var col = renderer.GetComponent<BoxCollider2D>();
                    if (col == null)
                    {
                        col = renderer.gameObject.AddComponent<BoxCollider2D>();
                        if (renderer.sprite != null)
                        {
                            col.size = renderer.sprite.rect.size / renderer.sprite.pixelsPerUnit;
                        }
                    }

                    var dest = renderer.GetComponent<DestructibleBlock>();
                    if (dest == null)
                    {
                        dest = renderer.gameObject.AddComponent<DestructibleBlock>();
                    }

                    blockCount++;
                }
            }

            // Setup or ensure EnemySpawner in level scene
            var spawnerObj = GameObject.Find("EnemySpawner");
            if (spawnerObj == null)
            {
                spawnerObj = new GameObject("EnemySpawner");
            }
            var spawner = spawnerObj.GetComponent<EnemySpawner>();
            if (spawner == null)
            {
                spawner = spawnerObj.AddComponent<EnemySpawner>();
            }

            var regCollection = AssetDatabase.LoadAssetAtPath<Digit7s.Enemies.EnemyCollection>("Assets/Data/Enemies/EnemyCollection_Regular.asset");
            var bossCollection = AssetDatabase.LoadAssetAtPath<Digit7s.Enemies.EnemyCollection>("Assets/Data/Enemies/EnemyCollection_Boss.asset");
            var spawnerSO = new SerializedObject(spawner);
            var colProp = spawnerSO.FindProperty("regularEnemyCollection");
            if (colProp != null) colProp.objectReferenceValue = regCollection;

            var bossColProp = spawnerSO.FindProperty("bossEnemyCollection");
            if (bossColProp != null) bossColProp.objectReferenceValue = bossCollection;

            var blockContainerProp = spawnerSO.FindProperty("blockContainer");
            if (blockContainerProp != null && blockCollectionObj != null)
            {
                blockContainerProp.objectReferenceValue = blockCollectionObj.transform;
            }

            var spawnModeProp = spawnerSO.FindProperty("blockSpawnMode");
            if (spawnModeProp != null)
            {
                spawnModeProp.enumValueIndex = (int)BlockSpawnMode.DistributeIntoBlocks;
            }

            var autoDistProp = spawnerSO.FindProperty("autoDistributeToBlocksOnStart");
            if (autoDistProp != null)
            {
                autoDistProp.boolValue = true;
            }

            var ladderProp = spawnerSO.FindProperty("ladderSprite");
            if (ladderProp != null)
            {
                ladderProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Crystal_Caves_Forest_2D_Platformer_Tileset_Platformer - Ladder.png");
            }

            var barrelProp = spawnerSO.FindProperty("woodenBarrelSprite");
            if (barrelProp != null)
            {
                barrelProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Crystal_Caves_Forest_2D_Platformer_Tileset_Platformer - Wooden Barrel.png");
            }

            var boxProp = spawnerSO.FindProperty("woodenBoxSprite");
            if (boxProp != null)
            {
                boxProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Crystal_Caves_Forest_2D_Platformer_Tileset_Platformer - Wooden Box.png");
            }

            spawnerSO.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BlockSystemSetup] Verified and configured {blockCount} blocks, BlockInputController, and EnemySpawner in level.unity!");
        }
    }
}
