using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using Spine.Unity;
using Digit7s.Characters;
using Digit7s.UI;
using Digit7s.Gameplay;

namespace Digit7s.Editor
{
    public static class CharacterSystemSetup
    {
        private const string DataFolder = "Assets/Data/Characters";
        private const string DatabasePath = "Assets/Data/Characters/CharacterDatabase.asset";
        private const string MainMenuScenePath = "Assets/Scenes/mainmenu.unity";
        private const string LevelScenePath = "Assets/Scenes/level.unity";

        [MenuItem("Digit7s/Setup Character System & MainMenu")]
        public static void SetupAll()
        {
            Debug.Log("[CharacterSystemSetup] Starting full setup...");

            // 1. Create ScriptableObjects for the 3 Characters and Database
            var db = CreateOrUpdateCharacterData();

            // 2. Setup Build Settings (mainmenu -> 0, level -> 1)
            SetupBuildSettings();

            // 3. Setup Main Menu scene
            SetupMainMenuScene(db);

            // 4. Setup Level scene with PlayerSpawner
            SetupLevelScene(db);

            // 5. Open MainMenu scene so it is the active scene in the Editor
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CharacterSystemSetup] Complete! Character System & MainMenu have been successfully set up.");
        }

        [MenuItem("Digit7s/Open MainMenu Scene")]
        public static void OpenMainMenu()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Digit7s/Open Level Scene")]
        public static void OpenLevel()
        {
            EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        }

        public static CharacterDatabase CreateOrUpdateCharacterData()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }

            // Character 1: Cat Blaster (Ranger)
            var char1 = AssetDatabase.LoadAssetAtPath<CharacterData>($"{DataFolder}/Character_01_BlasterCat.asset");
            if (char1 == null)
            {
                char1 = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(char1, $"{DataFolder}/Character_01_BlasterCat.asset");
            }
            char1.characterId = "blaster_cat";
            char1.characterName = "Cat Blaster";
            char1.characterClass = "Ranged Gunner";
            char1.characterIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Target.png");
            char1.description = "<b><color=#FFD700>[ HP: 35  |  DMG: 2 ]</color></b>\n• <color=#FFBB33><b>4-Hits Combo:</b></color> 5th Attack deals Damage x2 (+1~3 HP) & Shield\n• <color=#55FF88><b>On Kill:</b></color> +2 Fast Combos (Max 5 Stacks, RNG 25%~100% to trigger Damage x2 & Shield)";
            char1.catClassType = CatClassType.Blaster;
            char1.baseHealth = 35;
            char1.baseDamage = 2;
            char1.attacksRequiredForSpecial = 4;
            char1.procHpPerDamageStep = 1;
            char1.stunBlockClicksRequired = 0;
            char1.onKillHpPerEnemyHpStep = 0;
            char1.skins = new List<CharacterSkinData>
            {
                CreateSkin("Classic Blue", "Cat Characters/Cat1/Character1", "Cat Characters/Cat1/Character1", new Color(0.35f, 0.75f, 1f)),
                CreateSkin("Aqua Scout", "Cat Characters/Cat2/Character2", "Cat Characters/Cat2/Character2", new Color(0.2f, 0.95f, 0.9f)),
                CreateSkin("Pink Blossom", "Cat Characters/Cat3/Character3", "Cat Characters/Cat3/Character3", new Color(1f, 0.5f, 0.75f)),
                CreateSkin("Golden Spark", "Cat Characters/Cat4/Character4", "Cat Characters/Cat4/Character4", new Color(1f, 0.85f, 0.25f)),
                CreateSkin("Emerald Strike", "Cat Characters/Cat5/Character5", "Cat Characters/Cat5/Character5", new Color(0.3f, 0.95f, 0.45f))
            };
            EditorUtility.SetDirty(char1);

            // Character 2: Cat Brawler (Boxer)
            var char2 = AssetDatabase.LoadAssetAtPath<CharacterData>($"{DataFolder}/Character_02_BrawlerCat.asset");
            if (char2 == null)
            {
                char2 = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(char2, $"{DataFolder}/Character_02_BrawlerCat.asset");
            }
            char2.characterId = "brawler_cat";
            char2.characterName = "Cat Brawler";
            char2.characterClass = "Heavy Boxer";
            char2.characterIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_BoxingGloves.png");
            char2.description = "<b><color=#FFD700>[ HP: 30  |  DMG: 4 ]</color></b>\n• <color=#FFE855><b>5-Hits Combo:</b></color> 6th Attack deals Damage x1.5, Heals (+1~3 HP) & Grants Shield\n• <color=#55FF88><b>On Kill:</b></color> Heals +1~3 HP based on Enemy Total HP";
            char2.catClassType = CatClassType.Brawler;
            char2.baseHealth = 30;
            char2.baseDamage = 4;
            char2.attacksRequiredForSpecial = 5;
            char2.procHpPerDamageStep = 1;
            char2.stunBlockClicksRequired = 0;
            char2.onKillHpPerEnemyHpStep = 1;
            char2.skins = new List<CharacterSkinData>
            {
                CreateSkin("Shadow Pugilist", "Cat Characters/Cat6/Character6", "Cat Characters/Cat6/Character6", new Color(0.5f, 0.5f, 0.65f)),
                CreateSkin("Crimson Gloves", "Cat Characters/Cat7/Character7", "Cat Characters/Cat7/Character7", new Color(0.95f, 0.25f, 0.35f)),
                CreateSkin("Thunder Mitts", "Cat Characters/Cat8/Character8", "Cat Characters/Cat8/Character8", new Color(1f, 0.7f, 0.15f)),
                CreateSkin("Iron Claw", "Cat Characters/Cat9/Character9", "Cat Characters/Cat9/Character9", new Color(0.8f, 0.85f, 0.95f)),
                CreateSkin("Steam Raider", "Cat Characters/Cat10/Character10", "Cat Characters/Cat10/Character10", new Color(0.98f, 0.55f, 0.15f))
            };
            EditorUtility.SetDirty(char2);

            // Character 3: Cat Guardian (Defender)
            var char3 = AssetDatabase.LoadAssetAtPath<CharacterData>($"{DataFolder}/Character_03_GuardianCat.asset");
            if (char3 == null)
            {
                char3 = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(char3, $"{DataFolder}/Character_03_GuardianCat.asset");
            }
            char3.characterId = "guardian_cat";
            char3.characterName = "Cat Guardian";
            char3.characterClass = "Armored Sentinel";
            char3.characterIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Shield.png");
            char3.description = "<b><color=#FFD700>[ HP: 40  |  DMG: 3 ]</color></b>\n• <color=#55CCFF><b>5-Hits Combo:</b></color> 6th Attack Blocks Enemy DMG, Grants Shield & Heals (+2/4/6 HP)\n• <color=#55FF88><b>On Kill:</b></color> Grants 1-Time Shield Block";
            char3.catClassType = CatClassType.Guardian;
            char3.baseHealth = 40;
            char3.baseDamage = 3;
            char3.attacksRequiredForSpecial = 5;
            char3.procHpPerDamageStep = 2;
            char3.stunBlockClicksRequired = 0;
            char3.onKillHpPerEnemyHpStep = 0;
            char3.skins = new List<CharacterSkinData>
            {
                CreateSkin("Royal Knight", "Cat Characters/Cat11/Character11", "Cat Characters/Cat11/Character11", new Color(0.9f, 0.75f, 0.2f)),
                CreateSkin("Neon Phantom", "Cat Characters/Cat12/Character12", "Cat Characters/Cat12/Character12", new Color(0.75f, 0.35f, 1f)),
                CreateSkin("Forest Warden", "Cat Characters/Cat13/Character13", "Cat Characters/Cat13/Character13", new Color(0.35f, 0.85f, 0.45f)),
                CreateSkin("Abyssal Sentinel", "Cat Characters/Cat14/Character14", "Cat Characters/Cat14/Character14", new Color(0.35f, 0.45f, 0.7f)),
                CreateSkin("Crimson Shinobi", "Cat Characters/Cat15/Character15", "Cat Characters/Cat15/Character15", new Color(0.95f, 0.25f, 0.25f))
            };
            EditorUtility.SetDirty(char3);

            // Database
            var db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CharacterDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.characters = new List<CharacterData> { char1, char2, char3 };
            EditorUtility.SetDirty(db);

            // Copy to Resources for standalone runtime loading
            if (!Directory.Exists("Assets/Resources"))
            {
                Directory.CreateDirectory("Assets/Resources");
            }
            AssetDatabase.DeleteAsset("Assets/Resources/CharacterDatabase.asset");
            AssetDatabase.CopyAsset(DatabasePath, "Assets/Resources/CharacterDatabase.asset");

            AssetDatabase.SaveAssets();
            Debug.Log($"[CharacterSystemSetup] Created CharacterDatabase with 3 characters at {DatabasePath}");
            return db;
        }

        private static CharacterSkinData CreateSkin(string name, string prefabSubPath, string sdaSubPath, Color themeColor)
        {
            string prefabPath = $"Assets/Prefabs/Spine Characters/{prefabSubPath}.prefab";
            string sdaPath = $"Assets/Json Atlas/{sdaSubPath}_SkeletonData.asset";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var sda = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(sdaPath);

            return new CharacterSkinData
            {
                skinName = name,
                characterPrefab = prefab,
                skeletonDataAsset = sda,
                themeColor = themeColor
            };
        }

        public static void SetupBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[CharacterSystemSetup] Configured EditorBuildSettings: 0 -> mainmenu.unity, 1 -> level.unity");
        }

        public static void SetupMainMenuScene(CharacterDatabase db)
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            // 1. Camera adjustments for crisp 2D
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5f;
                mainCam.backgroundColor = new Color(0.12f, 0.14f, 0.20f, 1f);
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }

            // 2. Cleanup any legacy world-space preview anchor
            var oldAnchor = GameObject.Find("CharacterPreviewAnchor");
            if (oldAnchor != null) Object.DestroyImmediate(oldAnchor);

            // 3. Setup UI Canvas (ScreenSpaceOverlay for crisp UI and SkeletonGraphic)
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                Object.DestroyImmediate(canvasObj);
            }

            canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 10;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            // Standard crisp built-in font
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // UI Background Panel
            var bgObj = CreateUIObject("BackgroundGradient", canvasObj.transform);
            SetStretch(bgObj.GetComponent<RectTransform>());
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.16f, 0.85f);

            // ==========================================
            // Main Menu Panel
            // ==========================================
            var mainMenuPanelObj = CreateUIObject("MainMenuPanel", canvasObj.transform);
            SetStretch(mainMenuPanelObj.GetComponent<RectTransform>());

            // Title Container
            var titleBox = CreateUIObject("TitleBox", mainMenuPanelObj.transform);
            var titleBoxRT = titleBox.GetComponent<RectTransform>();
            titleBoxRT.anchorMin = new Vector2(0.5f, 0.72f);
            titleBoxRT.anchorMax = new Vector2(0.5f, 0.72f);
            titleBoxRT.sizeDelta = new Vector2(1000, 180);
            titleBoxRT.anchoredPosition = Vector2.zero;

            var titleText = CreateText("GameTitle", titleBox.transform, "Cat Dungeon Adventure", 56, font, FontStyle.Bold, new Color(1f, 0.85f, 0.25f));
            var titleRT = titleText.GetComponent<RectTransform>();
            titleRT.anchoredPosition = Vector2.zero;
            titleRT.sizeDelta = new Vector2(1000, 80);
            titleText.alignment = TextAnchor.MiddleCenter;
            var shadow = titleText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(3, -3);

            // Main Buttons Column
            var buttonCol = CreateUIObject("ButtonColumn", mainMenuPanelObj.transform);
            var colRT = buttonCol.GetComponent<RectTransform>();
            colRT.anchorMin = new Vector2(0.5f, 0.34f);
            colRT.anchorMax = new Vector2(0.5f, 0.34f);
            colRT.sizeDelta = new Vector2(500, 220);
            colRT.anchoredPosition = Vector2.zero;
            var colLayout = buttonCol.AddComponent<VerticalLayoutGroup>();
            colLayout.spacing = 25;
            colLayout.childAlignment = TextAnchor.MiddleCenter;
            colLayout.childControlHeight = false;
            colLayout.childControlWidth = true;
            colLayout.childForceExpandHeight = false;
            colLayout.childForceExpandWidth = true;

            var newGameBtn = CreateStyledButton("Btn_NewGame", buttonCol.transform, "▶  NEW GAME", 34, font, new Color(0.16f, 0.72f, 0.38f), Color.white, 88);
            var quitBtn = CreateStyledButton("Btn_Quit", buttonCol.transform, "✕  QUIT", 26, font, new Color(0.32f, 0.34f, 0.42f), new Color(0.92f, 0.92f, 0.95f), 75);

            // ==========================================
            // Character Select Panel
            // ==========================================
            var charSelectPanelObj = CreateUIObject("CharacterSelectPanel", canvasObj.transform);
            SetStretch(charSelectPanelObj.GetComponent<RectTransform>());

            // Top Header
            var selectHeader = CreateText("HeaderTitle", charSelectPanelObj.transform, "CHARACTER SELECTION", 44, font, FontStyle.Bold, new Color(1f, 0.85f, 0.25f));
            var headRT = selectHeader.GetComponent<RectTransform>();
            headRT.anchorMin = new Vector2(0.5f, 0.925f);
            headRT.anchorMax = new Vector2(0.5f, 0.925f);
            headRT.sizeDelta = new Vector2(900, 70);
            headRT.anchoredPosition = Vector2.zero;
            selectHeader.alignment = TextAnchor.MiddleCenter;

            // Character Tabs (Top bar with 3 tabs)
            var tabsContainer = CreateUIObject("CharacterTabs", charSelectPanelObj.transform);
            var tabsRT = tabsContainer.GetComponent<RectTransform>();
            tabsRT.anchorMin = new Vector2(0.5f, 0.84f);
            tabsRT.anchorMax = new Vector2(0.5f, 0.84f);
            tabsRT.sizeDelta = new Vector2(960, 64);
            tabsRT.anchoredPosition = Vector2.zero;
            var tabLayout = tabsContainer.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 20;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = true;

            var iconBlaster = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Target.png");
            var iconBrawler = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_BoxingGloves.png");
            var iconGuardian = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/Icon_Shield.png");

            Image tabIcon1, tabIcon2, tabIcon3;
            var tabBtn1 = CreateStyledTabButton("Tab_Char1", tabsContainer.transform, "1. CAT BLASTER", iconBlaster, 22, font, new Color(0.25f, 0.35f, 0.55f), Color.white, 64, 300, out tabIcon1);
            var tabBtn2 = CreateStyledTabButton("Tab_Char2", tabsContainer.transform, "2. CAT BRAWLER", iconBrawler, 22, font, new Color(0.25f, 0.35f, 0.55f), Color.white, 64, 300, out tabIcon2);
            var tabBtn3 = CreateStyledTabButton("Tab_Char3", tabsContainer.transform, "3. CAT GUARDIAN", iconGuardian, 22, font, new Color(0.25f, 0.35f, 0.55f), Color.white, 64, 300, out tabIcon3);

            // Left Side: Preview Framing Card
            var previewFrame = CreateUIObject("PreviewFrame", charSelectPanelObj.transform);
            var prevFrameRT = previewFrame.GetComponent<RectTransform>();
            prevFrameRT.anchorMin = new Vector2(0.08f, 0.20f);
            prevFrameRT.anchorMax = new Vector2(0.48f, 0.76f);
            prevFrameRT.offsetMin = Vector2.zero;
            prevFrameRT.offsetMax = Vector2.zero;
            var frameImg = previewFrame.AddComponent<Image>();
            frameImg.color = new Color(0.12f, 0.15f, 0.24f, 0.6f);
            var frameOutline = previewFrame.AddComponent<Outline>();
            frameOutline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.5f);
            frameOutline.effectDistance = new Vector2(2, 2);

            var previewHint = CreateText("PreviewHint", previewFrame.transform, "LIVE HERO PREVIEW", 18, font, FontStyle.Bold, new Color(0.6f, 0.7f, 0.85f));
            var phRT = previewHint.GetComponent<RectTransform>();
            phRT.anchorMin = new Vector2(0.5f, 0.93f);
            phRT.anchorMax = new Vector2(0.5f, 0.93f);
            phRT.sizeDelta = new Vector2(300, 30);
            phRT.anchoredPosition = Vector2.zero;
            previewHint.alignment = TextAnchor.MiddleCenter;

            // UI Preview Anchor inside Preview Frame
            var previewAnchorObj = CreateUIObject("PreviewAnchor", previewFrame.transform);
            var paRT = previewAnchorObj.GetComponent<RectTransform>();
            paRT.anchorMin = new Vector2(0.5f, 0.46f);
            paRT.anchorMax = new Vector2(0.5f, 0.46f);
            paRT.sizeDelta = new Vector2(200, 200);
            paRT.anchoredPosition = Vector2.zero;

            // Right Side: Info & Customization Card
            var infoCard = CreateUIObject("InfoCard", charSelectPanelObj.transform);
            var infoRT = infoCard.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0.52f, 0.20f);
            infoRT.anchorMax = new Vector2(0.92f, 0.76f);
            infoRT.offsetMin = Vector2.zero;
            infoRT.offsetMax = Vector2.zero;
            var infoImg = infoCard.AddComponent<Image>();
            infoImg.color = new Color(0.12f, 0.15f, 0.24f, 0.9f);
            var infoOutline = infoCard.AddComponent<Outline>();
            infoOutline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.6f);
            infoOutline.effectDistance = new Vector2(2, 2);

            // Character Header Navigation row (< Name >)
            var charNavRow = CreateUIObject("CharNavRow", infoCard.transform);
            var cNavRT = charNavRow.GetComponent<RectTransform>();
            cNavRT.anchorMin = new Vector2(0.05f, 0.82f);
            cNavRT.anchorMax = new Vector2(0.95f, 0.96f);
            cNavRT.offsetMin = Vector2.zero;
            cNavRT.offsetMax = Vector2.zero;

            var prevCharBtn = CreateStyledButton("Btn_PrevChar", charNavRow.transform, "◀", 32, font, new Color(0.25f, 0.35f, 0.5f), Color.white, 62, 75);
            var pBtnRT = prevCharBtn.GetComponent<RectTransform>();
            pBtnRT.anchorMin = new Vector2(0f, 0.5f);
            pBtnRT.anchorMax = new Vector2(0f, 0.5f);
            pBtnRT.anchoredPosition = new Vector2(40, 0);

            var nextCharBtn = CreateStyledButton("Btn_NextChar", charNavRow.transform, "▶", 32, font, new Color(0.25f, 0.35f, 0.5f), Color.white, 62, 75);
            var nBtnRT = nextCharBtn.GetComponent<RectTransform>();
            nBtnRT.anchorMin = new Vector2(1f, 0.5f);
            nBtnRT.anchorMax = new Vector2(1f, 0.5f);
            nBtnRT.anchoredPosition = new Vector2(-40, 0);

            var centerBox = CreateUIObject("CenterBox", charNavRow.transform);
            var cbRT = centerBox.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(0.5f, 0.5f);
            cbRT.anchorMax = new Vector2(0.5f, 0.5f);
            cbRT.pivot = new Vector2(0.5f, 0.5f);
            cbRT.anchoredPosition = Vector2.zero;
            cbRT.sizeDelta = new Vector2(320, 50);

            var cbLayout = centerBox.AddComponent<HorizontalLayoutGroup>();
            cbLayout.spacing = 12;
            cbLayout.childAlignment = TextAnchor.MiddleCenter;
            cbLayout.childControlWidth = false;
            cbLayout.childControlHeight = false;
            cbLayout.childForceExpandWidth = false;
            cbLayout.childForceExpandHeight = false;

            var iconBadgeObj = CreateUIObject("CharIconBadge", centerBox.transform);
            var ibRT = iconBadgeObj.GetComponent<RectTransform>();
            ibRT.sizeDelta = new Vector2(36, 36);
            var charIconImg = iconBadgeObj.AddComponent<Image>();
            charIconImg.sprite = iconBlaster;
            charIconImg.preserveAspect = true;
            charIconImg.raycastTarget = false;

            var charNameText = CreateText("CharName", centerBox.transform, "Cat Blaster", 32, font, FontStyle.Bold, Color.white);
            var cnRT = charNameText.GetComponent<RectTransform>();
            cnRT.sizeDelta = new Vector2(230, 44);
            charNameText.alignment = TextAnchor.MiddleLeft;

            // Class Badge & Counter
            var classText = CreateText("CharClass", infoCard.transform, "CLASS: RANGED GUNNER", 20, font, FontStyle.Bold, new Color(0.35f, 0.8f, 1f));
            var classRT = classText.GetComponent<RectTransform>();
            classRT.anchorMin = new Vector2(0.08f, 0.72f);
            classRT.anchorMax = new Vector2(0.92f, 0.80f);
            classRT.offsetMin = Vector2.zero;
            classRT.offsetMax = Vector2.zero;
            classText.alignment = TextAnchor.MiddleLeft;

            // Description
            var descText = CreateText("CharDesc", infoCard.transform, "Agile cat with rapid-fire dual blasters. Excels at high mobility and long-range pressure.", 18, font, FontStyle.Normal, new Color(0.8f, 0.85f, 0.95f));
            var descRT = descText.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.08f, 0.52f);
            descRT.anchorMax = new Vector2(0.92f, 0.70f);
            descRT.offsetMin = Vector2.zero;
            descRT.offsetMax = Vector2.zero;
            descText.alignment = TextAnchor.UpperLeft;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Truncate;

            // Character Counter
            var charCounterText = CreateText("CharCounter", infoCard.transform, "1 / 3", 16, font, FontStyle.Normal, new Color(0.6f, 0.65f, 0.75f));
            var ccRT = charCounterText.GetComponent<RectTransform>();
            ccRT.anchorMin = new Vector2(0.80f, 0.72f);
            ccRT.anchorMax = new Vector2(0.92f, 0.80f);
            ccRT.offsetMin = Vector2.zero;
            ccRT.offsetMax = Vector2.zero;
            charCounterText.alignment = TextAnchor.MiddleRight;

            // Divider Line
            var divObj = CreateUIObject("Divider", infoCard.transform);
            var divRT = divObj.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.08f, 0.48f);
            divRT.anchorMax = new Vector2(0.92f, 0.485f);
            divRT.offsetMin = Vector2.zero;
            divRT.offsetMax = Vector2.zero;
            var divImg = divObj.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.4f, 0.55f, 0.5f);

            // ==========================================
            // Skin Selection Section
            // ==========================================
            var skinHeader = CreateText("SkinHeader", infoCard.transform, "CUSTOMIZE SKIN", 20, font, FontStyle.Bold, new Color(1f, 0.85f, 0.3f));
            var skhRT = skinHeader.GetComponent<RectTransform>();
            skhRT.anchorMin = new Vector2(0.08f, 0.38f);
            skhRT.anchorMax = new Vector2(0.50f, 0.46f);
            skhRT.offsetMin = Vector2.zero;
            skhRT.offsetMax = Vector2.zero;
            skinHeader.alignment = TextAnchor.MiddleLeft;

            var skinCounterText = CreateText("SkinCounter", infoCard.transform, "Skin 1 / 5", 16, font, FontStyle.Normal, new Color(0.65f, 0.7f, 0.85f));
            var scRT = skinCounterText.GetComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0.55f, 0.38f);
            scRT.anchorMax = new Vector2(0.92f, 0.46f);
            scRT.offsetMin = Vector2.zero;
            scRT.offsetMax = Vector2.zero;
            skinCounterText.alignment = TextAnchor.MiddleRight;

            // Skin Switcher Row (< SkinName >)
            var skinSwitcherRow = CreateUIObject("SkinSwitcherRow", infoCard.transform);
            var ssRT = skinSwitcherRow.GetComponent<RectTransform>();
            ssRT.anchorMin = new Vector2(0.08f, 0.22f);
            ssRT.anchorMax = new Vector2(0.92f, 0.36f);
            ssRT.offsetMin = Vector2.zero;
            ssRT.offsetMax = Vector2.zero;
            var ssImg = skinSwitcherRow.AddComponent<Image>();
            ssImg.color = new Color(0.08f, 0.10f, 0.16f, 0.6f);

            var prevSkinBtn = CreateStyledButton("Btn_PrevSkin", skinSwitcherRow.transform, "◀", 28, font, new Color(0.25f, 0.35f, 0.5f), Color.white, 58, 70);
            var psRT = prevSkinBtn.GetComponent<RectTransform>();
            psRT.anchorMin = new Vector2(0f, 0.5f);
            psRT.anchorMax = new Vector2(0f, 0.5f);
            psRT.anchoredPosition = new Vector2(35, 0);

            var nextSkinBtn = CreateStyledButton("Btn_NextSkin", skinSwitcherRow.transform, "▶", 28, font, new Color(0.25f, 0.35f, 0.5f), Color.white, 58, 70);
            var nsRT = nextSkinBtn.GetComponent<RectTransform>();
            nsRT.anchorMin = new Vector2(1f, 0.5f);
            nsRT.anchorMax = new Vector2(1f, 0.5f);
            nsRT.anchoredPosition = new Vector2(-35, 0);

            var skinNameText = CreateText("SkinName", skinSwitcherRow.transform, "Classic Blue", 26, font, FontStyle.Bold, new Color(0.35f, 0.75f, 1f));
            var snRT = skinNameText.GetComponent<RectTransform>();
            snRT.anchorMin = new Vector2(0.18f, 0f);
            snRT.anchorMax = new Vector2(0.82f, 1f);
            snRT.offsetMin = Vector2.zero;
            snRT.offsetMax = Vector2.zero;
            skinNameText.alignment = TextAnchor.MiddleCenter;

            // Skin Dots Container
            var dotsObj = CreateUIObject("SkinDotsContainer", infoCard.transform);
            var dotsRT = dotsObj.GetComponent<RectTransform>();
            dotsRT.anchorMin = new Vector2(0.08f, 0.08f);
            dotsRT.anchorMax = new Vector2(0.92f, 0.18f);
            dotsRT.offsetMin = Vector2.zero;
            dotsRT.offsetMax = Vector2.zero;
            var dotsLayout = dotsObj.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 18;
            dotsLayout.childAlignment = TextAnchor.MiddleCenter;
            dotsLayout.childControlWidth = false;
            dotsLayout.childControlHeight = false;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;

            // Bottom Buttons (Start Game & Back)
            var startBtn = CreateStyledButton("Btn_StartGame", charSelectPanelObj.transform, "CONFIRM & PLAY  ▶", 32, font, new Color(0.14f, 0.78f, 0.38f), Color.white, 80, 420);
            var stRT = startBtn.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.72f, 0.08f);
            stRT.anchorMax = new Vector2(0.72f, 0.08f);
            stRT.anchoredPosition = Vector2.zero;

            var backBtn = CreateStyledButton("Btn_Back", charSelectPanelObj.transform, "◀  BACK", 26, font, new Color(0.35f, 0.38f, 0.48f), Color.white, 72, 250);
            var bkRT = backBtn.GetComponent<RectTransform>();
            bkRT.anchorMin = new Vector2(0.22f, 0.08f);
            bkRT.anchorMax = new Vector2(0.22f, 0.08f);
            bkRT.anchoredPosition = Vector2.zero;

            // ==========================================
            // MainMenuController Component Wiring
            // ==========================================
            var controller = canvasObj.AddComponent<MainMenuController>();

            // Wire SerializedFields using SerializedObject
            var so = new SerializedObject(controller);
            var dbAsset = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DatabasePath);
            so.FindProperty("characterDatabase").objectReferenceValue = dbAsset;
            so.FindProperty("gameplaySceneName").stringValue = "level";
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanelObj;
            so.FindProperty("characterSelectPanel").objectReferenceValue = charSelectPanelObj;
            so.FindProperty("newGameButton").objectReferenceValue = newGameBtn;
            so.FindProperty("characterSelectButton").objectReferenceValue = null;
            so.FindProperty("quitButton").objectReferenceValue = quitBtn;

            so.FindProperty("characterNameText").objectReferenceValue = charNameText;
            so.FindProperty("characterClassText").objectReferenceValue = classText;
            so.FindProperty("characterDescText").objectReferenceValue = descText;
            so.FindProperty("characterCounterText").objectReferenceValue = charCounterText;
            so.FindProperty("characterIconImage").objectReferenceValue = charIconImg;
            so.FindProperty("prevCharacterButton").objectReferenceValue = prevCharBtn;
            so.FindProperty("nextCharacterButton").objectReferenceValue = nextCharBtn;

            var tabsProp = so.FindProperty("characterTabButtons");
            tabsProp.arraySize = 3;
            tabsProp.GetArrayElementAtIndex(0).objectReferenceValue = tabBtn1;
            tabsProp.GetArrayElementAtIndex(1).objectReferenceValue = tabBtn2;
            tabsProp.GetArrayElementAtIndex(2).objectReferenceValue = tabBtn3;

            var tabIconsProp = so.FindProperty("characterTabIcons");
            tabIconsProp.arraySize = 3;
            tabIconsProp.GetArrayElementAtIndex(0).objectReferenceValue = tabIcon1;
            tabIconsProp.GetArrayElementAtIndex(1).objectReferenceValue = tabIcon2;
            tabIconsProp.GetArrayElementAtIndex(2).objectReferenceValue = tabIcon3;

            so.FindProperty("skinNameText").objectReferenceValue = skinNameText;
            so.FindProperty("skinCounterText").objectReferenceValue = skinCounterText;
            so.FindProperty("prevSkinButton").objectReferenceValue = prevSkinBtn;
            so.FindProperty("nextSkinButton").objectReferenceValue = nextSkinBtn;
            so.FindProperty("skinDotsContainer").objectReferenceValue = dotsObj.transform;

            so.FindProperty("startGameButton").objectReferenceValue = startBtn;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("previewAnchor").objectReferenceValue = previewAnchorObj.transform;
            so.FindProperty("previewScale").vector3Value = new Vector3(1.25f, 1.25f, 1f);

            // Wire adaptive layout containers
            so.FindProperty("canvasScaler").objectReferenceValue = scaler;
            so.FindProperty("titleBoxRT").objectReferenceValue = titleBoxRT;
            so.FindProperty("buttonColRT").objectReferenceValue = colRT;
            so.FindProperty("headerTitleRT").objectReferenceValue = headRT;
            so.FindProperty("tabsContainerRT").objectReferenceValue = tabsRT;
            so.FindProperty("previewFrameRT").objectReferenceValue = prevFrameRT;
            so.FindProperty("infoCardRT").objectReferenceValue = infoRT;
            so.FindProperty("charNavRowRT").objectReferenceValue = cNavRT;
            so.FindProperty("skinSwitcherRowRT").objectReferenceValue = ssRT;

            so.ApplyModifiedProperties();

            // Run initial adaptive layout
            controller.ApplyAdaptiveLayout(true);

            // Set initial active states for Edit mode
            mainMenuPanelObj.SetActive(true);
            charSelectPanelObj.SetActive(false);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CharacterSystemSetup] Configured MainMenuController and UI in mainmenu.unity!");
        }

        public static void SetupLevelScene(CharacterDatabase db)
        {
            var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

            var spawnerObj = GameObject.Find("PlayerSpawner");
            if (spawnerObj == null)
            {
                spawnerObj = new GameObject("PlayerSpawner");
            }
            spawnerObj.transform.position = new Vector3(-3.5f, -0.8f, 0f);

            var spawner = spawnerObj.GetComponent<PlayerSpawner>();
            if (spawner == null)
            {
                spawner = spawnerObj.AddComponent<PlayerSpawner>();
            }

            var so = new SerializedObject(spawner);
            so.FindProperty("characterDatabase").objectReferenceValue = db;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CharacterSystemSetup] Configured PlayerSpawner in level.unity!");
        }

        // ==========================================
        // Helper UI creation functions
        // ==========================================
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, Font font, FontStyle fontStyle, Color color)
        {
            var go = CreateUIObject(name, parent);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.font = font;
            t.fontStyle = fontStyle;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Button CreateStyledButton(string name, Transform parent, string label, int fontSize, Font font, Color bgColor, Color textColor, float height, float width = -1)
        {
            var go = CreateUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            if (width > 0)
            {
                rt.sizeDelta = new Vector2(width, height);
            }
            else
            {
                rt.sizeDelta = new Vector2(250, height);
            }

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            btn.colors = colors;

            // Subtle outline for modern UI look
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Label
            var textObj = CreateUIObject("Label", go.transform);
            var textRT = textObj.GetComponent<RectTransform>();
            SetStretch(textRT);
            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.font = font;
            txt.fontStyle = FontStyle.Bold;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            return btn;
        }

        private static Button CreateStyledTabButton(string name, Transform parent, string label, Sprite icon, int fontSize, Font font, Color bgColor, Color textColor, float height, float width, out Image outIcon)
        {
            var go = CreateUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            if (width > 0)
            {
                rt.sizeDelta = new Vector2(width, height);
            }
            else
            {
                rt.sizeDelta = new Vector2(250, height);
            }

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            btn.colors = colors;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Tab Icon
            var iconObj = CreateUIObject("Icon", go.transform);
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0f, 0.5f);
            iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0f, 0.5f);
            iconRT.anchoredPosition = new Vector2(14f, 0f);
            iconRT.sizeDelta = new Vector2(28f, 28f);
            outIcon = iconObj.AddComponent<Image>();
            outIcon.sprite = icon;
            outIcon.preserveAspect = true;
            outIcon.raycastTarget = false;

            // Label
            var textObj = CreateUIObject("Label", go.transform);
            var textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0f, 0f);
            textRT.anchorMax = new Vector2(1f, 1f);
            textRT.offsetMin = new Vector2(46f, 0f);
            textRT.offsetMax = new Vector2(-10f, 0f);
            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.font = font;
            txt.fontStyle = FontStyle.Bold;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            return btn;
        }
    }
}
