using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Spine.Unity;
using Digit7s.Characters;

namespace Digit7s.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Character Database")]
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private string gameplaySceneName = "level";

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Menu Controls")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button settingButton;
        [SerializeField] private Button characterSelectButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Controls")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Text bgmVolumeText;
        [SerializeField] private Button bgmMuteButton;
        [SerializeField] private Text bgmMuteButtonText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text sfxVolumeText;
        [SerializeField] private Button sfxMuteButton;
        [SerializeField] private Text sfxMuteButtonText;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button bottomCloseSettingsButton;

        [Header("Character Selection - Character Info")]
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterClassText;
        [SerializeField] private Text characterDescText;
        [SerializeField] private Text characterCounterText;
        [SerializeField] private Image characterIconImage;
        [SerializeField] private Button prevCharacterButton;
        [SerializeField] private Button nextCharacterButton;
        [SerializeField] private Button[] characterTabButtons;
        [SerializeField] private Image[] characterTabIcons;

        [Header("Character Selection - Skin Controls")]
        [SerializeField] private Text skinNameText;
        [SerializeField] private Text skinCounterText;
        [SerializeField] private Button prevSkinButton;
        [SerializeField] private Button nextSkinButton;
        [SerializeField] private Transform skinDotsContainer;
        [SerializeField] private GameObject skinDotPrefab;

        [Header("Character Selection - Actions")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button backButton;

        [Header("Character 2D Preview")]
        [SerializeField] private Transform previewAnchor;
        [SerializeField] private Vector3 previewScale = new Vector3(1.25f, 1.25f, 1f);

        [Header("Adaptive Layout Elements")]
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private RectTransform titleBoxRT;
        [SerializeField] private RectTransform buttonColRT;
        [SerializeField] private RectTransform headerTitleRT;
        [SerializeField] private RectTransform tabsContainerRT;
        [SerializeField] private RectTransform previewFrameRT;
        [SerializeField] private RectTransform infoCardRT;
        [SerializeField] private RectTransform charNavRowRT;
        [SerializeField] private RectTransform skinSwitcherRowRT;

        // Runtime selection state
        public static bool OpenCharacterSelectOnLoad = false;
        private int currentCharacterIndex = 0;
        private int currentSkinIndex = 0;
        private GameObject currentPreviewObject;
        private List<Button> spawnedSkinDots = new List<Button>();
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        private void Awake()
        {
            Digit7s.FrameRateManager.ApplyFrameRate();
            Digit7s.UI.UISoundManager.StopDungeonMusic();
            Digit7s.UI.UISoundManager.StopGameOverMusic();
            Digit7s.UI.UISoundManager.PlayMainMenuMusic();
            Digit7s.Gameplay.EnemySpawner.ResetStaticData();
            Digit7s.Gameplay.PlayerInstance.ResetRunStats(30, 3);
            // Fallback load database if not assigned
            if (characterDatabase == null)
            {
                characterDatabase = Resources.Load<CharacterDatabase>("CharacterDatabase");
#if UNITY_EDITOR
                if (characterDatabase == null)
                {
                    characterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/Data/Characters/CharacterDatabase.asset");
                }
#endif
            }

            // Load saved character & skin selection
            currentCharacterIndex = CharacterSelectionState.SelectedCharacterIndex;
            currentSkinIndex = CharacterSelectionState.GetSelectedSkinForCharacter(currentCharacterIndex);
        }

        private void Start()
        {
            Digit7s.UI.UISoundManager.PlayMainMenuMusic();

            // Bind Main Menu buttons
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (leaderboardButton == null && buttonColRT != null)
            {
                var existingBtn = buttonColRT.Find("Btn_Leaderboard")?.GetComponent<Button>();
                if (existingBtn != null) leaderboardButton = existingBtn;
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.RemoveAllListeners();
                leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            }

            if (settingButton != null)
                settingButton.onClick.AddListener(OpenSettings);

            if (characterSelectButton != null)
                characterSelectButton.onClick.AddListener(OpenCharacterSelect);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Bind Settings buttons & sliders
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(CloseSettings);

            if (bottomCloseSettingsButton != null)
                bottomCloseSettingsButton.onClick.AddListener(CloseSettings);

            if (bgmVolumeSlider != null)
                bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeSliderChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);

            if (bgmMuteButton != null)
                bgmMuteButton.onClick.AddListener(OnBgmMuteButtonClicked);

            if (sfxMuteButton != null)
                sfxMuteButton.onClick.AddListener(OnSfxMuteButtonClicked);

            // Bind Character Select buttons
            if (prevCharacterButton != null)
                prevCharacterButton.onClick.AddListener(PreviousCharacter);

            if (nextCharacterButton != null)
                nextCharacterButton.onClick.AddListener(NextCharacter);

            if (prevSkinButton != null)
                prevSkinButton.onClick.AddListener(PreviousSkin);

            if (nextSkinButton != null)
                nextSkinButton.onClick.AddListener(NextSkin);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);

            if (backButton != null)
                backButton.onClick.AddListener(BackToMainMenu);

            // Bind Character Tab buttons if assigned
            if (characterTabButtons != null)
            {
                for (int i = 0; i < characterTabButtons.Length; i++)
                {
                    int index = i;
                    if (characterTabButtons[i] != null)
                    {
                        characterTabButtons[i].onClick.AddListener(() => SelectCharacter(index));
                    }
                }
            }

            // Start on Main Menu or Character Select view
            if (OpenCharacterSelectOnLoad)
            {
                OpenCharacterSelectOnLoad = false;
                OpenCharacterSelect();
            }
            else
            {
                ShowMainMenu();
            }

            // Initial adaptive layout calculation
            ApplyAdaptiveLayout(true);
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                ApplyAdaptiveLayout();
            }
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (titleBoxRT != null) titleBoxRT.gameObject.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (previewAnchor != null) previewAnchor.gameObject.SetActive(false);
        }

        public void OpenCharacterSelect()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (previewAnchor != null) previewAnchor.gameObject.SetActive(true);

            UpdateUI();
        }

        public void OnLeaderboardClicked()
        {
            UISoundManager.PlayClick();
            if (LeaderboardUIController.Instance != null)
            {
                LeaderboardUIController.Instance.OpenLeaderboard();
            }
        }

        public void OpenSettings()
        {
            UISoundManager.PlayClick();
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            UpdateSettingsUI();
        }

        public void CloseSettings()
        {
            UISoundManager.PlayBack();
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            ShowMainMenu();
        }

        private void UpdateSettingsUI()
        {
            float bgmVol = UISoundManager.BgmVolume;
            bool bgmMuted = UISoundManager.IsBgmMuted;

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(bgmVol);
                bgmVolumeSlider.interactable = !bgmMuted;
            }

            if (bgmVolumeText != null)
            {
                bgmVolumeText.text = bgmMuted ? "MUTED" : $"{Mathf.RoundToInt(bgmVol * 100f)}%";
            }

            if (bgmMuteButtonText != null)
            {
                bgmMuteButtonText.text = bgmMuted ? "UNMUTE BGM" : "MUTE BGM";
            }
            if (bgmMuteButton != null)
            {
                var img = bgmMuteButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = bgmMuted ? new Color(0.75f, 0.22f, 0.22f, 1f) : new Color(0.20f, 0.26f, 0.38f, 1f);
                }
            }

            float sfxVol = UISoundManager.SfxVolume;
            bool sfxMuted = UISoundManager.IsSfxMuted;

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfxVol);
                sfxVolumeSlider.interactable = !sfxMuted;
            }

            if (sfxVolumeText != null)
            {
                sfxVolumeText.text = sfxMuted ? "MUTED" : $"{Mathf.RoundToInt(sfxVol * 100f)}%";
            }

            if (sfxMuteButtonText != null)
            {
                sfxMuteButtonText.text = sfxMuted ? "UNMUTE SFX" : "MUTE SFX";
            }
            if (sfxMuteButton != null)
            {
                var img = sfxMuteButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = sfxMuted ? new Color(0.75f, 0.22f, 0.22f, 1f) : new Color(0.20f, 0.26f, 0.38f, 1f);
                }
            }
        }

        private void OnBgmVolumeSliderChanged(float val)
        {
            UISoundManager.SetBgmVolume(val);
            if (bgmVolumeText != null)
            {
                bgmVolumeText.text = UISoundManager.IsBgmMuted ? "MUTED" : $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void OnBgmMuteButtonClicked()
        {
            UISoundManager.ToggleBgmMute();
            UISoundManager.PlayClick();
            UpdateSettingsUI();
        }

        private void OnSfxVolumeSliderChanged(float val)
        {
            UISoundManager.SetSfxVolume(val);
            if (sfxVolumeText != null)
            {
                sfxVolumeText.text = UISoundManager.IsSfxMuted ? "MUTED" : $"{Mathf.RoundToInt(val * 100f)}%";
            }
            PlaySfxSliderPreview();
        }

        private float lastSfxPreviewTime = 0f;
        private void PlaySfxSliderPreview()
        {
            if (Time.unscaledTime - lastSfxPreviewTime > 0.08f)
            {
                lastSfxPreviewTime = Time.unscaledTime;
                UISoundManager.PlayClick();
            }
        }

        private void OnSfxMuteButtonClicked()
        {
            UISoundManager.ToggleSfxMute();
            if (!UISoundManager.IsSfxMuted)
            {
                UISoundManager.PlayClick();
            }
            UpdateSettingsUI();
        }

        private void OnNewGameClicked()
        {
            UISoundManager.PlayStart();
            // Direct user into Character Selection so they can review their hero & skin
            OpenCharacterSelect();
        }

        private void OnQuitClicked()
        {
            UISoundManager.PlayQuit();
            UISoundManager.StopMainMenuMusic(true);
            Debug.Log("[MainMenuController] Quitting application...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void BackToMainMenu()
        {
            UISoundManager.PlayBack();
            ShowMainMenu();
        }

        public void OnStartGameClicked()
        {
            UISoundManager.PlayStart();
            UISoundManager.StopMainMenuMusic(false);
            // Save final selection
            CharacterSelectionState.SaveSelection(currentCharacterIndex, currentSkinIndex);
            Digit7s.Gameplay.EnemySpawner.ResetStaticData();
            Digit7s.Gameplay.PlayerInstance.ResetRunStats(30, 3);
            Debug.Log($"[MainMenuController] Starting game with Character {currentCharacterIndex}, Skin {currentSkinIndex}...");

            if (!string.IsNullOrEmpty(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                SceneManager.LoadScene(1);
            }
        }

        public void NextCharacter()
        {
            if (characterDatabase == null || characterDatabase.CharacterCount == 0) return;
            int next = (currentCharacterIndex + 1) % characterDatabase.CharacterCount;
            SelectCharacter(next);
        }

        public void PreviousCharacter()
        {
            if (characterDatabase == null || characterDatabase.CharacterCount == 0) return;
            int prev = (currentCharacterIndex - 1 + characterDatabase.CharacterCount) % characterDatabase.CharacterCount;
            SelectCharacter(prev);
        }

        public void SelectCharacter(int index)
        {
            if (characterDatabase == null || characterDatabase.CharacterCount == 0) return;
            currentCharacterIndex = Mathf.Clamp(index, 0, characterDatabase.CharacterCount - 1);

            // Remember previous skin or load saved skin for this character
            currentSkinIndex = CharacterSelectionState.GetSelectedSkinForCharacter(currentCharacterIndex);
            var charData = characterDatabase.GetCharacter(currentCharacterIndex);
            if (charData != null && (currentSkinIndex < 0 || currentSkinIndex >= charData.SkinCount))
            {
                currentSkinIndex = 0;
            }

            UISoundManager.PlayHeroSwitch();
            UpdateUI();
        }

        public void NextSkin()
        {
            var charData = GetCurrentCharacter();
            if (charData == null || charData.SkinCount == 0) return;

            int next = (currentSkinIndex + 1) % charData.SkinCount;
            SelectSkin(next);
        }

        public void PreviousSkin()
        {
            var charData = GetCurrentCharacter();
            if (charData == null || charData.SkinCount == 0) return;

            int prev = (currentSkinIndex - 1 + charData.SkinCount) % charData.SkinCount;
            SelectSkin(prev);
        }

        public void SelectSkin(int skinIndex)
        {
            var charData = GetCurrentCharacter();
            if (charData == null || charData.SkinCount == 0) return;

            currentSkinIndex = Mathf.Clamp(skinIndex, 0, charData.SkinCount - 1);
            CharacterSelectionState.SetSelectedSkinForCharacter(currentCharacterIndex, currentSkinIndex);
            UISoundManager.PlaySkinSwitch();
            UpdateUI();
        }

        private CharacterData GetCurrentCharacter()
        {
            if (characterDatabase == null || characterDatabase.CharacterCount == 0) return null;
            return characterDatabase.GetCharacter(currentCharacterIndex);
        }

        private void UpdateUI()
        {
            var charData = GetCurrentCharacter();
            if (charData == null) return;

            // Character text
            if (characterNameText != null)
            {
                characterNameText.text = charData.characterName;
                var nRT = characterNameText.rectTransform;
                if (nRT != null)
                {
                    nRT.sizeDelta = new Vector2(characterNameText.preferredWidth + 6f, 26f);
                }
            }

            if (characterClassText != null)
                characterClassText.text = charData.characterClass;

            if (characterDescText != null)
            {
                characterDescText.supportRichText = true;
                characterDescText.text = charData.description;
            }

            if (characterCounterText != null)
                characterCounterText.text = $"{currentCharacterIndex + 1} / {characterDatabase.CharacterCount}";

            // Character icon
            if (characterIconImage != null)
            {
                characterIconImage.sprite = charData.characterIcon;
                characterIconImage.enabled = (charData.characterIcon != null);
            }

            // Update Character Tab Buttons highlighting & labels
            if (characterTabButtons != null)
            {
                for (int i = 0; i < characterTabButtons.Length; i++)
                {
                    if (characterTabButtons[i] != null)
                    {
                        var colors = characterTabButtons[i].colors;
                        colors.normalColor = (i == currentCharacterIndex) ? new Color(1f, 0.8f, 0.2f, 1f) : new Color(0.3f, 0.3f, 0.35f, 1f);
                        characterTabButtons[i].colors = colors;

                        if (characterDatabase != null && i < characterDatabase.CharacterCount)
                        {
                            var cData = characterDatabase.GetCharacter(i);
                            if (cData != null)
                            {
                                var label = characterTabButtons[i].GetComponentInChildren<Text>();
                                if (label != null && !string.IsNullOrEmpty(cData.characterName))
                                {
                                    string shortName = cData.characterName.ToUpper().Replace("CAT", "").Trim();
                                    label.text = $"{i + 1}. {shortName}";
                                    label.resizeTextForBestFit = true;
                                    label.resizeTextMinSize = 8;
                                    label.resizeTextMaxSize = 11;
                                }
                            }
                        }
                    }
                }
            }

            // Update Character Tab Icons
            if (characterTabIcons != null && characterDatabase != null)
            {
                for (int i = 0; i < characterTabIcons.Length && i < characterDatabase.CharacterCount; i++)
                {
                    if (characterTabIcons[i] != null)
                    {
                        var cData = characterDatabase.GetCharacter(i);
                        if (cData != null && cData.characterIcon != null)
                        {
                            characterTabIcons[i].sprite = cData.characterIcon;
                            characterTabIcons[i].enabled = true;
                        }
                    }
                }
            }

            // Skin text
            var skinData = charData.GetSkin(currentSkinIndex);
            if (skinData != null)
            {
                if (skinNameText != null)
                {
                    skinNameText.text = skinData.skinName;
                    skinNameText.color = skinData.themeColor != Color.clear ? skinData.themeColor : Color.white;
                }

                if (skinCounterText != null)
                    skinCounterText.text = $"Skin {currentSkinIndex + 1} / {charData.SkinCount}";
            }

            // Refresh skin dots/buttons
            UpdateSkinDots(charData);

            // Update live Spine preview
            UpdatePreview(skinData);
        }

        private void UpdateSkinDots(CharacterData charData)
        {
            if (skinDotsContainer == null || charData == null) return;

            int skinCount = charData.SkinCount;
            float dotSize = 20f;

            // Synchronize spawnedSkinDots with actual child buttons in skinDotsContainer
            spawnedSkinDots.Clear();
            for (int i = 0; i < skinDotsContainer.childCount; i++)
            {
                var btn = skinDotsContainer.GetChild(i).GetComponent<Button>();
                if (btn != null)
                {
                    spawnedSkinDots.Add(btn);
                }
            }

            // Remove any excess dots beyond skinCount
            while (spawnedSkinDots.Count > skinCount)
            {
                int lastIdx = spawnedSkinDots.Count - 1;
                var excessBtn = spawnedSkinDots[lastIdx];
                spawnedSkinDots.RemoveAt(lastIdx);
                if (excessBtn != null)
                {
                    if (Application.isPlaying)
                        Destroy(excessBtn.gameObject);
                    else
                        DestroyImmediate(excessBtn.gameObject);
                }
            }

            // Also remove any stray non-button children beyond skinCount
            while (skinDotsContainer.childCount > skinCount)
            {
                var stray = skinDotsContainer.GetChild(skinDotsContainer.childCount - 1).gameObject;
                if (Application.isPlaying)
                    Destroy(stray);
                else
                    DestroyImmediate(stray);
            }

            // Instantiate only if more dots are needed
            while (spawnedSkinDots.Count < skinCount)
            {
                GameObject dotObj;
                if (skinDotPrefab != null)
                {
                    dotObj = Instantiate(skinDotPrefab, skinDotsContainer);
                }
                else
                {
                    dotObj = new GameObject($"Dot_{spawnedSkinDots.Count}");
                    dotObj.transform.SetParent(skinDotsContainer, false);
                    var img = dotObj.AddComponent<Image>();
                    img.color = Color.white;
                    var btn = dotObj.AddComponent<Button>();
                    var outline = dotObj.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                    var rt = dotObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(dotSize, dotSize);
                }

                var button = dotObj.GetComponent<Button>();
                if (button != null)
                {
                    spawnedSkinDots.Add(button);
                }
            }

            // Update each dot's appearance, click listener, and active state
            for (int i = 0; i < skinCount; i++)
            {
                int targetIndex = i;
                var btn = spawnedSkinDots[i];
                if (btn == null) continue;

                btn.gameObject.name = $"Dot_{i}";
                btn.gameObject.SetActive(true);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectSkin(targetIndex));

                var rt = btn.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(dotSize, dotSize);

                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    var skin = charData.GetSkin(i);
                    Color activeColor = (skin != null && skin.themeColor != Color.clear)
                        ? skin.themeColor
                        : new Color(1f, 0.85f, 0.2f, 1f);

                    img.color = (i == currentSkinIndex)
                        ? activeColor
                        : new Color(0.35f, 0.38f, 0.48f, 0.7f);
                }
            }
        }

        public void ApplyAdaptiveLayout(bool force = false)
        {
            // 1. Ensure Canvas is ScreenSpaceCamera with Main Camera for clean pillarboxing on PC & Mobile
            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                if (canvas.worldCamera == null)
                {
                    canvas.worldCamera = Camera.main;
                }
                canvas.planeDistance = 5f;
            }

            // 2. Base CanvasScaler (360x640 base reference resolution, Match Width)
            if (canvasScaler == null)
            {
                canvasScaler = GetComponent<CanvasScaler>();
            }
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(360, 640);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0f;
            }

            // 3. Main Menu Panel Layout (Unified 360x640)
            if (titleBoxRT != null)
            {
                titleBoxRT.gameObject.SetActive(false);
            }

            var bgRT = mainMenuPanel != null ? mainMenuPanel.transform.Find("MainMenuBackground")?.GetComponent<RectTransform>() : null;
            if (bgRT != null)
            {
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.anchoredPosition = Vector2.zero;
                bgRT.sizeDelta = Vector2.zero;
                bgRT.pivot = new Vector2(0.5f, 0.5f);
            }

            if (buttonColRT != null)
            {
                var colLayout = buttonColRT.GetComponent<VerticalLayoutGroup>();
                buttonColRT.anchorMin = new Vector2(0.5f, 0.23f);
                buttonColRT.anchorMax = new Vector2(0.5f, 0.23f);
                buttonColRT.sizeDelta = new Vector2(270, 224);
                if (colLayout != null)
                {
                    colLayout.spacing = 10;
                    colLayout.childAlignment = TextAnchor.MiddleCenter;
                    colLayout.childControlHeight = false;
                    colLayout.childControlWidth = false;
                }

                SetButtonSize(newGameButton, 270, 48, 18);
                SetButtonSize(leaderboardButton, 270, 44, 16);
                SetButtonSize(settingButton, 270, 44, 16);
                SetButtonSize(quitButton, 270, 42, 15);
            }

            // 4. Character Selection Panel Layout (Unified 360x640)
            if (headerTitleRT != null)
            {
                var hText = headerTitleRT.GetComponent<Text>();
                headerTitleRT.anchorMin = new Vector2(0.5f, 0.955f);
                headerTitleRT.anchorMax = new Vector2(0.5f, 0.955f);
                headerTitleRT.sizeDelta = new Vector2(330, 32);
                if (hText != null) hText.fontSize = 20;
            }

            if (tabsContainerRT != null)
            {
                var tabLayout = tabsContainerRT.GetComponent<HorizontalLayoutGroup>();
                tabsContainerRT.anchorMin = new Vector2(0.5f, 0.905f);
                tabsContainerRT.anchorMax = new Vector2(0.5f, 0.905f);
                tabsContainerRT.sizeDelta = new Vector2(330, 34);
                if (tabLayout != null) tabLayout.spacing = 6;

                if (characterTabButtons != null)
                {
                    foreach (var btn in characterTabButtons)
                    {
                        if (btn != null)
                        {
                            SetButtonSize(btn, 106, 34, 10);
                            var btnTxt = btn.GetComponentInChildren<Text>();
                            if (btnTxt != null)
                            {
                                btnTxt.resizeTextForBestFit = true;
                                btnTxt.resizeTextMinSize = 8;
                                btnTxt.resizeTextMaxSize = 11;
                            }
                        }
                    }
                }
                if (characterTabIcons != null)
                {
                    foreach (var tabIcon in characterTabIcons)
                    {
                        if (tabIcon != null)
                        {
                            var trt = tabIcon.GetComponent<RectTransform>();
                            if (trt != null) trt.sizeDelta = new Vector2(18, 18);
                        }
                    }
                }
            }

            // Preview Frame & Info Card
            if (previewFrameRT != null)
            {
                previewFrameRT.anchorMin = new Vector2(0.04f, 0.525f);
                previewFrameRT.anchorMax = new Vector2(0.96f, 0.865f);
                previewFrameRT.offsetMin = Vector2.zero;
                previewFrameRT.offsetMax = Vector2.zero;

                var previewHint = previewFrameRT.Find("PreviewHint")?.GetComponent<Text>();
                if (previewHint != null)
                {
                    previewHint.fontSize = 11;
                    var phRT = previewHint.GetComponent<RectTransform>();
                    if (phRT != null) phRT.sizeDelta = new Vector2(250, 20);
                }

                if (previewAnchor != null)
                {
                    var paRT = previewAnchor.GetComponent<RectTransform>();
                    if (paRT != null)
                    {
                        paRT.anchorMin = new Vector2(0.5f, 0.46f);
                        paRT.anchorMax = new Vector2(0.5f, 0.46f);
                        paRT.sizeDelta = new Vector2(100, 100);
                        paRT.anchoredPosition = Vector2.zero;
                    }
                }
                previewScale = new Vector3(1.0f, 1.0f, 1f);

                if (currentPreviewObject != null)
                {
                    currentPreviewObject.transform.localScale = previewScale;
                    var sg = currentPreviewObject.GetComponent<SkeletonGraphic>();
                    if (sg != null && sg.SkeletonDataAsset != null)
                    {
                        var skelData = sg.SkeletonDataAsset.GetSkeletonData(false);
                        if (skelData != null)
                        {
                            var rt = currentPreviewObject.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                float centerX = skelData.X + (skelData.Width * 0.5f);
                                float centerY = skelData.Y + (skelData.Height * 0.5f);
                                rt.anchoredPosition = new Vector2(-centerX * previewScale.x, -centerY * previewScale.y);
                            }
                        }
                    }
                }
            }

            if (infoCardRT != null)
            {
                infoCardRT.anchorMin = new Vector2(0.04f, 0.13f);
                infoCardRT.anchorMax = new Vector2(0.96f, 0.515f);
                infoCardRT.offsetMin = Vector2.zero;
                infoCardRT.offsetMax = Vector2.zero;

                var charClassRT = infoCardRT.Find("CharClass")?.GetComponent<RectTransform>();
                if (charClassRT != null)
                {
                    charClassRT.anchorMin = new Vector2(0.08f, 0.73f);
                    charClassRT.anchorMax = new Vector2(0.70f, 0.81f);
                    charClassRT.offsetMin = Vector2.zero;
                    charClassRT.offsetMax = Vector2.zero;
                }

                var charCounterRT = infoCardRT.Find("CharCounter")?.GetComponent<RectTransform>();
                if (charCounterRT != null)
                {
                    charCounterRT.anchorMin = new Vector2(0.70f, 0.73f);
                    charCounterRT.anchorMax = new Vector2(0.92f, 0.81f);
                    charCounterRT.offsetMin = Vector2.zero;
                    charCounterRT.offsetMax = Vector2.zero;
                }

                var charDescRT = infoCardRT.Find("CharDesc")?.GetComponent<RectTransform>();
                if (charDescRT != null)
                {
                    charDescRT.anchorMin = new Vector2(0.08f, 0.42f);
                    charDescRT.anchorMax = new Vector2(0.92f, 0.71f);
                    charDescRT.offsetMin = Vector2.zero;
                    charDescRT.offsetMax = Vector2.zero;
                }

                var dividerRT = infoCardRT.Find("Divider")?.GetComponent<RectTransform>();
                if (dividerRT != null)
                {
                    dividerRT.anchorMin = new Vector2(0.08f, 0.395f);
                    dividerRT.anchorMax = new Vector2(0.92f, 0.40f);
                    dividerRT.offsetMin = Vector2.zero;
                    dividerRT.offsetMax = Vector2.zero;
                }

                var skinHeaderRT = infoCardRT.Find("SkinHeader")?.GetComponent<RectTransform>();
                if (skinHeaderRT != null)
                {
                    skinHeaderRT.anchorMin = new Vector2(0.08f, 0.29f);
                    skinHeaderRT.anchorMax = new Vector2(0.60f, 0.37f);
                    skinHeaderRT.offsetMin = Vector2.zero;
                    skinHeaderRT.offsetMax = Vector2.zero;
                    var sh = skinHeaderRT.GetComponent<Text>();
                    if (sh != null)
                    {
                        sh.fontSize = 10;
                        sh.alignment = TextAnchor.MiddleLeft;
                    }
                }

                var skinCounterRT = infoCardRT.Find("SkinCounter")?.GetComponent<RectTransform>();
                if (skinCounterRT != null)
                {
                    skinCounterRT.anchorMin = new Vector2(0.60f, 0.29f);
                    skinCounterRT.anchorMax = new Vector2(0.92f, 0.37f);
                    skinCounterRT.offsetMin = Vector2.zero;
                    skinCounterRT.offsetMax = Vector2.zero;
                    var sc = skinCounterRT.GetComponent<Text>();
                    if (sc != null)
                    {
                        sc.fontSize = 10;
                        sc.alignment = TextAnchor.MiddleRight;
                    }
                }
            }

            // Character Navigation Row
            if (charNavRowRT != null)
            {
                charNavRowRT.anchorMin = new Vector2(0.04f, 0.835f);
                charNavRowRT.anchorMax = new Vector2(0.96f, 0.965f);
                charNavRowRT.offsetMin = Vector2.zero;
                charNavRowRT.offsetMax = Vector2.zero;

                var rowBg = charNavRowRT.GetComponent<Image>();
                if (rowBg == null) rowBg = charNavRowRT.gameObject.AddComponent<Image>();
                rowBg.color = new Color(0.08f, 0.10f, 0.16f, 0.60f);

                if (prevCharacterButton != null)
                {
                    var pRT = prevCharacterButton.GetComponent<RectTransform>();
                    if (pRT != null)
                    {
                        pRT.anchorMin = new Vector2(0f, 0.5f);
                        pRT.anchorMax = new Vector2(0f, 0.5f);
                        pRT.pivot = new Vector2(0.5f, 0.5f);
                        pRT.anchoredPosition = new Vector2(20f, 0f);
                        pRT.sizeDelta = new Vector2(30f, 26f);
                    }
                    SetButtonSize(prevCharacterButton, 30, 26, 13);
                }

                if (nextCharacterButton != null)
                {
                    var nRT = nextCharacterButton.GetComponent<RectTransform>();
                    if (nRT != null)
                    {
                        nRT.anchorMin = new Vector2(1f, 0.5f);
                        nRT.anchorMax = new Vector2(1f, 0.5f);
                        nRT.pivot = new Vector2(0.5f, 0.5f);
                        nRT.anchoredPosition = new Vector2(-20f, 0f);
                        nRT.sizeDelta = new Vector2(30f, 26f);
                    }
                    SetButtonSize(nextCharacterButton, 30, 26, 13);
                }

                var cb = charNavRowRT.Find("CenterBox")?.GetComponent<RectTransform>();
                if (cb != null)
                {
                    cb.anchorMin = new Vector2(0.5f, 0.5f);
                    cb.anchorMax = new Vector2(0.5f, 0.5f);
                    cb.pivot = new Vector2(0.5f, 0.5f);
                    cb.anchoredPosition = Vector2.zero;
                    cb.sizeDelta = new Vector2(210f, 28f);

                    var cbLayout = cb.GetComponent<HorizontalLayoutGroup>();
                    if (cbLayout != null)
                    {
                        cbLayout.childAlignment = TextAnchor.MiddleCenter;
                        cbLayout.childControlWidth = false;
                        cbLayout.childControlHeight = false;
                        cbLayout.childForceExpandWidth = false;
                        cbLayout.childForceExpandHeight = false;
                        cbLayout.spacing = 8f;
                        cbLayout.padding = new RectOffset(0, 0, 0, 0);
                    }
                }

                if (characterIconImage != null)
                {
                    characterIconImage.preserveAspect = true;
                    var irt = characterIconImage.GetComponent<RectTransform>();
                    if (irt != null) irt.sizeDelta = new Vector2(22f, 22f);
                }

                if (characterNameText != null)
                {
                    characterNameText.fontSize = 14;
                    characterNameText.fontStyle = FontStyle.Bold;
                    characterNameText.alignment = TextAnchor.MiddleLeft;
                    characterNameText.color = Color.white;
                    var nRT = characterNameText.GetComponent<RectTransform>();
                    if (nRT != null)
                    {
                        nRT.sizeDelta = new Vector2(characterNameText.preferredWidth + 6f, 26f);
                    }
                }
            }

            // Class Badge, Description, Counter
            if (characterClassText != null)
            {
                characterClassText.fontSize = 11;
                characterClassText.fontStyle = FontStyle.Bold;
                characterClassText.alignment = TextAnchor.MiddleLeft;
                characterClassText.color = new Color(0.35f, 0.85f, 1f, 1f);
                characterClassText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            if (characterCounterText != null)
            {
                characterCounterText.fontSize = 11;
                characterCounterText.alignment = TextAnchor.MiddleRight;
                characterCounterText.color = new Color(0.7f, 0.75f, 0.85f, 0.9f);
            }
            if (characterDescText != null)
            {
                characterDescText.fontSize = 10;
                characterDescText.lineSpacing = 1.15f;
                characterDescText.alignment = TextAnchor.UpperLeft;
                characterDescText.horizontalOverflow = HorizontalWrapMode.Wrap;
                characterDescText.verticalOverflow = VerticalWrapMode.Truncate;
            }
            if (skinCounterText != null)
            {
                skinCounterText.fontSize = 10;
            }

            // Skin Switcher Row & Buttons
            if (skinSwitcherRowRT != null)
            {
                skinSwitcherRowRT.anchorMin = new Vector2(0.04f, 0.14f);
                skinSwitcherRowRT.anchorMax = new Vector2(0.96f, 0.26f);
                skinSwitcherRowRT.offsetMin = Vector2.zero;
                skinSwitcherRowRT.offsetMax = Vector2.zero;

                if (prevSkinButton != null)
                {
                    var pRT = prevSkinButton.GetComponent<RectTransform>();
                    if (pRT != null)
                    {
                        pRT.anchorMin = new Vector2(0f, 0.5f);
                        pRT.anchorMax = new Vector2(0f, 0.5f);
                        pRT.pivot = new Vector2(0.5f, 0.5f);
                        pRT.anchoredPosition = new Vector2(20f, 0f);
                        pRT.sizeDelta = new Vector2(30f, 26f);
                    }
                    SetButtonSize(prevSkinButton, 30, 26, 13);
                }

                if (nextSkinButton != null)
                {
                    var nRT = nextSkinButton.GetComponent<RectTransform>();
                    if (nRT != null)
                    {
                        nRT.anchorMin = new Vector2(1f, 0.5f);
                        nRT.anchorMax = new Vector2(1f, 0.5f);
                        nRT.pivot = new Vector2(0.5f, 0.5f);
                        nRT.anchoredPosition = new Vector2(-20f, 0f);
                        nRT.sizeDelta = new Vector2(30f, 26f);
                    }
                    SetButtonSize(nextSkinButton, 30, 26, 13);
                }

                if (skinNameText != null)
                {
                    var snRT = skinNameText.GetComponent<RectTransform>();
                    if (snRT != null)
                    {
                        snRT.anchorMin = new Vector2(0.18f, 0f);
                        snRT.anchorMax = new Vector2(0.82f, 1f);
                        snRT.offsetMin = Vector2.zero;
                        snRT.offsetMax = Vector2.zero;
                    }
                    skinNameText.fontSize = 13;
                    skinNameText.fontStyle = FontStyle.Bold;
                    skinNameText.alignment = TextAnchor.MiddleCenter;
                    skinNameText.resizeTextForBestFit = true;
                    skinNameText.resizeTextMinSize = 10;
                    skinNameText.resizeTextMaxSize = 13;
                }
            }

            // Skin Dots Container
            if (skinDotsContainer != null)
            {
                var dotsRT = skinDotsContainer.GetComponent<RectTransform>();
                var dotsLayout = skinDotsContainer.GetComponent<HorizontalLayoutGroup>();
                if (dotsRT != null)
                {
                    dotsRT.anchorMin = new Vector2(0.04f, 0.02f);
                    dotsRT.anchorMax = new Vector2(0.96f, 0.11f);
                    dotsRT.offsetMin = Vector2.zero;
                    dotsRT.offsetMax = Vector2.zero;
                }
                if (dotsLayout != null) dotsLayout.spacing = 8;
            }

            // Bottom Buttons (Start Game & Back)
            if (backButton != null)
            {
                var bkRT = backButton.GetComponent<RectTransform>();
                if (bkRT != null)
                {
                    bkRT.anchorMin = new Vector2(0.22f, 0.065f);
                    bkRT.anchorMax = new Vector2(0.22f, 0.065f);
                    SetButtonSize(backButton, 106, 42, 13);
                }
            }

            if (startGameButton != null)
            {
                var stRT = startGameButton.GetComponent<RectTransform>();
                if (stRT != null)
                {
                    stRT.anchorMin = new Vector2(0.68f, 0.065f);
                    stRT.anchorMax = new Vector2(0.68f, 0.065f);
                    SetButtonSize(startGameButton, 186, 42, 15);
                }
            }

            // Refresh skin dots sizes
            var currentHero = GetCurrentCharacter();
            if (currentHero != null)
            {
                UpdateSkinDots(currentHero);
            }
        }

        private void SetButtonSize(Button btn, float width, float height, int fontSize)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(width, height);
            }
            var text = btn.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = fontSize;
            }
        }

        private void UpdatePreview(CharacterSkinData skinData)
        {
            if (previewAnchor == null) return;

            // Clean up all existing preview objects inside previewAnchor to prevent duplicates
            for (int i = previewAnchor.childCount - 1; i >= 0; i--)
            {
                var child = previewAnchor.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
            currentPreviewObject = null;

            if (skinData == null) return;

            // Prefer SkeletonGraphic for native UI Canvas rendering
            if (skinData.skeletonDataAsset != null)
            {
                Material graphicMat = null;
#if UNITY_EDITOR
                graphicMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Spine/Runtime/spine-unity/Materials/UI-PMATexture/SkeletonGraphicDefault.mat");
#endif
                if (graphicMat == null)
                {
                    var shader = Shader.Find("Spine/SkeletonGraphic");
                    if (shader != null) graphicMat = new Material(shader);
                }

                var spineComponents = SkeletonGraphic.NewSkeletonGraphicGameObject(skinData.skeletonDataAsset, previewAnchor, graphicMat, true);
                if (spineComponents.skeletonRenderer != null)
                {
                    currentPreviewObject = spineComponents.skeletonRenderer.gameObject;
                    var rt = currentPreviewObject.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = previewScale;

                        // Align the skeleton's true visual center with PreviewAnchor (mid, center)
                        var sg = spineComponents.skeletonRenderer;
                        if (sg.SkeletonDataAsset != null)
                        {
                            var skelData = sg.SkeletonDataAsset.GetSkeletonData(false);
                            if (skelData != null)
                            {
                                float centerX = skelData.X + (skelData.Width * 0.5f);
                                float centerY = skelData.Y + (skelData.Height * 0.5f);
                                rt.anchoredPosition = new Vector2(-centerX * previewScale.x, -centerY * previewScale.y);
                            }
                            else
                            {
                                rt.anchoredPosition = Vector2.zero;
                            }
                        }
                        else
                        {
                            rt.anchoredPosition = Vector2.zero;
                        }
                    }

                    var sgRenderer = spineComponents.skeletonRenderer;
                    if (sgRenderer.SkeletonDataAsset != null)
                    {
                        var skelData = sgRenderer.SkeletonDataAsset.GetSkeletonData(false);
                        if (skelData != null)
                        {
                            var idleAnim = skelData.FindAnimation("Idle") ?? skelData.FindAnimation("idle") ?? (skelData.Animations.Count > 0 ? skelData.Animations.Items[0] : null);
                            if (idleAnim != null && spineComponents.skeletonAnimation != null)
                            {
                                spineComponents.skeletonAnimation.AnimationState.SetAnimation(0, idleAnim.Name, true);
                            }
                        }
                    }
                }
            }
            else if (skinData.characterPrefab != null)
            {
                currentPreviewObject = Instantiate(skinData.characterPrefab, previewAnchor);
                currentPreviewObject.transform.localPosition = Vector3.zero;
                currentPreviewObject.transform.localRotation = Quaternion.identity;
                currentPreviewObject.transform.localScale = previewScale;
            }

            // Punch effect on preview anchor for lively feedback
            StartCoroutine(PunchPreviewRoutine());
        }

        private IEnumerator PunchPreviewRoutine()
        {
            if (previewAnchor == null) yield break;

            Vector3 startScale = previewScale;
            Vector3 targetScale = previewScale * 1.15f;

            float elapsed = 0f;
            float duration = 0.08f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                if (currentPreviewObject != null)
                    currentPreviewObject.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            duration = 0.12f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                if (currentPreviewObject != null)
                    currentPreviewObject.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
                yield return null;
            }

            if (currentPreviewObject != null)
                currentPreviewObject.transform.localScale = startScale;
        }
    }
}
