using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Spine.Unity;

namespace Spine.Unity.Editor {
    public static class SpineBatchImporter {
        [MenuItem("Spine/Batch Import All Json Atlas Folders")]
        public static void BatchImportAll() {
            // Disable workflow mismatch dialogs
            SpineEditorUtilities.Preferences.ShowWorkflowMismatchDialog = false;
            SpineEditorUtilities.Preferences.defaultShader = "Universal Render Pipeline/2D/Spine/Skeleton Lit";

            var shader = Shader.Find("Universal Render Pipeline/2D/Spine/Skeleton Lit");
            if (shader == null) {
                Debug.LogError("Spine URP 2D Shader 'Universal Render Pipeline/2D/Spine/Skeleton Lit' not found!");
                return;
            }

            string rootDir = "Assets/Json Atlas";
            string[] atlasTxtFiles = Directory.GetFiles(rootDir, "*.atlas.txt", SearchOption.AllDirectories);

            int successCount = 0;
            int errorCount = 0;

            foreach (string rawAtlasTxtPath in atlasTxtFiles) {
                string atlasTxtPath = rawAtlasTxtPath.Replace('\\', '/');
                string dir = Path.GetDirectoryName(atlasTxtPath).Replace('\\', '/');

                try {
                    // Find json file in this dir
                    string[] jsonFiles = Directory.GetFiles(dir, "*.json");
                    if (jsonFiles.Length == 0) {
                        Debug.LogWarning($"No .json file found in {dir}");
                        continue;
                    }
                    string jsonPath = jsonFiles[0].Replace('\\', '/');

                    // Read atlas to find texture image name
                    string[] lines = File.ReadAllLines(atlasTxtPath);
                    string pageImageName = "";
                    foreach (string line in lines) {
                        string trimmed = line.Trim();
                        if (trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
                            pageImageName = trimmed;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(pageImageName)) {
                        string[] pngFiles = Directory.GetFiles(dir, "*.png");
                        if (pngFiles.Length > 0) {
                            pageImageName = Path.GetFileName(pngFiles[0]);
                        }
                    }

                    string pngPath = $"{dir}/{pageImageName}";

                    // 1. Configure Texture Importer for PMA
                    AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
                    var texImporter = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                    if (texImporter != null) {
                        bool dirty = false;
                        if (texImporter.textureType != TextureImporterType.Default) { texImporter.textureType = TextureImporterType.Default; dirty = true; }
                        if (texImporter.spriteImportMode != SpriteImportMode.None) { texImporter.spriteImportMode = SpriteImportMode.None; dirty = true; }
                        if (texImporter.sRGBTexture != false) { texImporter.sRGBTexture = false; dirty = true; }
                        if (texImporter.alphaIsTransparency != false) { texImporter.alphaIsTransparency = false; dirty = true; }
                        if (texImporter.mipmapEnabled != false) { texImporter.mipmapEnabled = false; dirty = true; }
                        if (texImporter.wrapMode != TextureWrapMode.Clamp) { texImporter.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                        if (texImporter.filterMode != FilterMode.Bilinear) { texImporter.filterMode = FilterMode.Bilinear; dirty = true; }
                        if (texImporter.textureCompression != TextureImporterCompression.Uncompressed) { texImporter.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                        if (texImporter.maxTextureSize != 2048) { texImporter.maxTextureSize = 2048; dirty = true; }
                        if (dirty) {
                            texImporter.SaveAndReimport();
                        }
                    }

                    AssetDatabase.ImportAsset(atlasTxtPath, ImportAssetOptions.ForceSynchronousImport);

                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
                    var atlasText = AssetDatabase.LoadAssetAtPath<TextAsset>(atlasTxtPath);
                    var jsonText = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);

                    string primaryName = Path.GetFileNameWithoutExtension(atlasTxtPath).Replace(".atlas", "");

                    // 2. Material
                    string matPath = $"{dir}/{primaryName}_Material.mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) {
                        mat = new Material(shader);
                        mat.mainTexture = tex;
                        mat.SetFloat("_StraightAlphaInput", 0);
                        mat.DisableKeyword("_STRAIGHT_ALPHA_INPUT");
                        AssetDatabase.CreateAsset(mat, matPath);
                    } else {
                        mat.shader = shader;
                        mat.mainTexture = tex;
                        mat.SetFloat("_StraightAlphaInput", 0);
                        mat.DisableKeyword("_STRAIGHT_ALPHA_INPUT");
                        EditorUtility.SetDirty(mat);
                    }

                    // 3. SpineAtlasAsset
                    string atlasAssetPath = $"{dir}/{primaryName}_Atlas.asset";
                    var atlasAsset = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(atlasAssetPath);
                    if (atlasAsset == null) {
                        atlasAsset = ScriptableObject.CreateInstance<SpineAtlasAsset>();
                        atlasAsset.atlasFile = atlasText;
                        atlasAsset.materials = new Material[] { mat };
                        AssetDatabase.CreateAsset(atlasAsset, atlasAssetPath);
                    } else {
                        atlasAsset.atlasFile = atlasText;
                        atlasAsset.materials = new Material[] { mat };
                        atlasAsset.Clear();
                        EditorUtility.SetDirty(atlasAsset);
                    }

                    // 4. SkeletonDataAsset
                    string jsonName = Path.GetFileNameWithoutExtension(jsonPath);
                    string sdaPath = $"{dir}/{jsonName}_SkeletonData.asset";
                    var sda = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(sdaPath);
                    if (sda == null) {
                        sda = ScriptableObject.CreateInstance<SkeletonDataAsset>();
                        sda.skeletonJSON = jsonText;
                        sda.atlasAssets = new AtlasAssetBase[] { atlasAsset };
                        sda.scale = 0.01f;
                        sda.defaultMix = 0.2f;
                        sda.controller = null;
                        AssetDatabase.CreateAsset(sda, sdaPath);
                    } else {
                        sda.skeletonJSON = jsonText;
                        sda.atlasAssets = new AtlasAssetBase[] { atlasAsset };
                        sda.scale = 0.01f;
                        sda.defaultMix = 0.2f;
                        sda.controller = null;
                        sda.Clear();
                        EditorUtility.SetDirty(sda);
                    }

                    // If Cat1, also update New SkeletonDataAsset if it exists
                    if (primaryName == "Character1") {
                        string legacySdaPath = $"{dir}/New SkeletonDataAsset.asset";
                        var legacySda = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(legacySdaPath);
                        if (legacySda != null) {
                            legacySda.atlasAssets = new AtlasAssetBase[] { atlasAsset };
                            legacySda.Clear();
                            legacySda.GetSkeletonData(true);
                            EditorUtility.SetDirty(legacySda);
                        }
                    }

                    var skeletonData = sda.GetSkeletonData(true);
                    if (skeletonData != null) {
                        successCount++;
                        Debug.Log($"[SpineBatchImporter] Successfully configured: {primaryName} (Bones: {skeletonData.Bones.Count}, Animations: {skeletonData.Animations.Count})");
                    } else {
                        errorCount++;
                        Debug.LogError($"[SpineBatchImporter] Failed to load skeleton data for: {primaryName}");
                    }
                } catch (Exception ex) {
                    errorCount++;
                    Debug.LogError($"[SpineBatchImporter] Exception processing {dir}: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SpineBatchImporter] Finished! Success: {successCount}, Errors: {errorCount}");
        }

        [MenuItem("Spine/Generate Prefabs For All SkeletonDataAssets")]
        public static void GeneratePrefabsForAll() {
            string rootDir = "Assets/Json Atlas";
            string[] sdaFiles = Directory.GetFiles(rootDir, "*_SkeletonData.asset", SearchOption.AllDirectories);

            string prefabsRoot = "Assets/Prefabs/Spine Characters";

            int count = 0;
            foreach (string sdaPath in sdaFiles) {
                string normPath = sdaPath.Replace('\\', '/');
                var sda = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(normPath);
                if (sda == null) continue;

                var skelData = sda.GetSkeletonData(false);
                if (skelData == null) continue;

                string dir = Path.GetDirectoryName(normPath).Replace('\\', '/');
                string relDir = dir.Substring(rootDir.Length).TrimStart('/');
                string targetDir = string.IsNullOrEmpty(relDir) ? prefabsRoot : $"{prefabsRoot}/{relDir}";

                if (!Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }

                string charName = Path.GetFileNameWithoutExtension(normPath).Replace("_SkeletonData", "");

                // Create instance in scene temporarily
                GameObject go = new GameObject(charName);
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                var sr = go.AddComponent<SkeletonRenderer>();
                var sa = go.AddComponent<SkeletonAnimation>();

                sr.skeletonDataAsset = sda;
                sa.skeletonDataAsset = sda;

                sa.Initialize(true);

                // Set initial animation if available
                if (skelData.Animations.Count > 0) {
                    var idleAnim = skelData.FindAnimation("Idle") ?? skelData.FindAnimation("idle") ?? skelData.Animations.Items[0];
                    if (idleAnim != null) {
                        sa.AnimationName = idleAnim.Name;
                        sa.loop = true;
                    }
                }

                sr.LateUpdate();

                string prefabPath = $"{targetDir}/{charName}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                GameObject.DestroyImmediate(go);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpineBatchImporter] Generated {count} prefabs in {prefabsRoot}!");
        }
    }
}
