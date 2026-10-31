using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Digit7s.Gameplay;
using Digit7s.Characters;
using Digit7s.Services;

namespace Digit7s.UI
{
    /// <summary>
    /// Controls the HUD during gameplay: displays HP, DMG, attack-counter combo status,
    /// Room Level badge, Power-Up banners, and the Game Over modal.
    /// Auto-constructs Canvas and UI hierarchy at runtime if not present in the scene.
    /// </summary>
    public class GameplayUIController : MonoBehaviour
    {
        public static GameplayUIController Instance { get; private set; }

        [Header("UI Text References")]
        [SerializeField] private Text playerHpText;
        [SerializeField] private Text playerDmgText;
        [SerializeField] private Text dungeonLevelText;
        public Text roomLevelText => dungeonLevelText;
        [SerializeField] private Text powerUpNoticeText;

        [Header("Containers & Modals")]
        [SerializeField] private GameObject hudContainer;
        [SerializeField] private GameObject dungeonBadgeObj;
        private RectTransform dungeonBadgeRT;
        [SerializeField] private GameObject keyContainer;
        [SerializeField] private Image hpIconImage;
        [SerializeField] private Image dmgIconImage;
        [SerializeField] private Image keyIconImage;
        [SerializeField] private Text keyStatusText;
        [SerializeField] private Outline keyOutline;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverSubText;
        [SerializeField] private GameObject notificationBanner;
        [SerializeField] private GameObject roomBonusReadyBadge;
        [SerializeField] private Text roomBonusReadyText;
        [SerializeField] private GameObject bonusSlotPanel;
        [SerializeField] private RectTransform bonusReelStripRT;
        [SerializeField] private Text bonusSlotSubText;
        [SerializeField] private Text bonusSlotResultText;
        [SerializeField] private Button bonusSpinButton;
        [SerializeField] private Text bonusSpinButtonText;

        [Header("Coin UI")]
        [SerializeField] private GameObject coinContainer;
        [SerializeField] private Image coinIconImage;
        [SerializeField] private Text coinCountText;
        private Sprite coinSprite;
        private RectTransform coinIconRT;
        private RectTransform coinTextRT;
        private bool isFirstFreeSpin = false;
        [SerializeField] private Text bonusSlotTitleText;

        private Sprite keySprite;
        private Sprite lockSprite;
        private Sprite heartIconSprite;
        private Sprite swordIconSprite;
        private Sprite shieldIconSprite;
        private Sprite cloverIconSprite;

        private bool isSlotSpinning = false;
        private bool hasCompletedSlotSpin = false;
        private int lastBonusSpinTriggerFrame = -1;
        private bool isWaitingForBonusToPlayDungeonIntro = false;
        private Coroutine activeDungeonIntroRoutine;
        private readonly System.Collections.Generic.List<BonusSlotEntry> currentSlotPool = new System.Collections.Generic.List<BonusSlotEntry>();
        private readonly System.Collections.Generic.List<RectTransform> spawnedReelCards = new System.Collections.Generic.List<RectTransform>();

        public enum BonusSlotRewardType
        {
            Damage,
            HpSmall,
            HpLarge,
            Shield1,
            Shield2,
            Empty,
            Luck
        }

        [System.Serializable]
        public struct BonusSlotEntry
        {
            public BonusSlotRewardType rewardType;
            public int value;
            public string label;
            public Color accentColor;
        }

        [Header("Game Over Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button characterSelectButton;

        [Header("Pause & Settings Menu")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseSettingButton;
        [SerializeField] private Button pauseCharacterSelectButton;

        [Header("Pause Warning Box")]
        [SerializeField] private GameObject warningConfirmPanel;
        [SerializeField] private Button warningYesButton;
        [SerializeField] private Button warningNoButton;

        [Header("Pause Settings Modal")]
        [SerializeField] private GameObject pauseSettingsPanel;
        [SerializeField] private Slider pauseBgmVolumeSlider;
        [SerializeField] private Text pauseBgmVolumeText;
        [SerializeField] private Button pauseBgmMuteButton;
        [SerializeField] private Text pauseBgmMuteButtonText;
        [SerializeField] private Slider pauseSfxVolumeSlider;
        [SerializeField] private Text pauseSfxVolumeText;
        [SerializeField] private Button pauseSfxMuteButton;
        [SerializeField] private Text pauseSfxMuteButtonText;
        [SerializeField] private Button pauseCloseSettingsButton;
        [SerializeField] private Button pauseBottomCloseSettingsButton;

        [Header("Configuration")]
        [SerializeField] private string mainMenuSceneName = "mainmenu";
        [SerializeField] private Font runtimeFont;

        private Coroutine activeNoticeRoutine;
        private float lastModalTransitionTime = -1f;

        private void Awake()
        {
            Digit7s.FrameRateManager.ApplyFrameRate();
            Instance = this;

            if (runtimeFont == null)
            {
                runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (runtimeFont == null)
                {
                    runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            EnsureUIHierarchy();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            BlockInputController.IsModalBlockingInput = false;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            PlayerInstance.OnHealthChanged += HandlePlayerHealthChanged;
            PlayerInstance.OnKeyChanged += HandlePlayerKeyChanged;
            PlayerInstance.OnCoinsChanged += HandleCoinsChanged;
            PlayerInstance.OnPlayerDied += HandlePlayerDied;
            CatPowerUpController.OnPowerUpNotification += HandlePowerUpNotification;
            LevelLadder.OnLadderUnlocked += HandleLadderUnlocked;
            EnemySpawner.OnRoomAllEnemiesCleared += HandleRoomAllEnemiesCleared;

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (characterSelectButton != null)
                characterSelectButton.onClick.AddListener(OnCharacterSelectClicked);

            if (bonusSpinButton != null)
                bonusSpinButton.onClick.AddListener(OnBonusSpinButtonClicked);

            // Pause & Settings Button Listeners
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OpenPauseMenu);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (pauseCharacterSelectButton != null)
                pauseCharacterSelectButton.onClick.AddListener(OpenWarningBox);

            if (pauseSettingButton != null)
                pauseSettingButton.onClick.AddListener(OpenPauseSettings);

            if (warningYesButton != null)
                warningYesButton.onClick.AddListener(OnWarningYesClicked);

            if (warningNoButton != null)
                warningNoButton.onClick.AddListener(CloseWarningBox);

            if (pauseCloseSettingsButton != null)
                pauseCloseSettingsButton.onClick.AddListener(ClosePauseSettings);

            if (pauseBottomCloseSettingsButton != null)
                pauseBottomCloseSettingsButton.onClick.AddListener(ClosePauseSettings);

            if (pauseBgmVolumeSlider != null)
                pauseBgmVolumeSlider.onValueChanged.AddListener(OnPauseBgmVolumeChanged);

            if (pauseSfxVolumeSlider != null)
                pauseSfxVolumeSlider.onValueChanged.AddListener(OnPauseSfxVolumeChanged);

            if (pauseBgmMuteButton != null)
                pauseBgmMuteButton.onClick.AddListener(OnPauseBgmMuteClicked);

            if (pauseSfxMuteButton != null)
                pauseSfxMuteButton.onClick.AddListener(OnPauseSfxMuteClicked);
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            PlayerInstance.OnHealthChanged -= HandlePlayerHealthChanged;
            PlayerInstance.OnKeyChanged -= HandlePlayerKeyChanged;
            PlayerInstance.OnCoinsChanged -= HandleCoinsChanged;
            PlayerInstance.OnPlayerDied -= HandlePlayerDied;
            CatPowerUpController.OnPowerUpNotification -= HandlePowerUpNotification;
            LevelLadder.OnLadderUnlocked -= HandleLadderUnlocked;
            EnemySpawner.OnRoomAllEnemiesCleared -= HandleRoomAllEnemiesCleared;

            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (characterSelectButton != null)
                characterSelectButton.onClick.RemoveListener(OnCharacterSelectClicked);

            if (bonusSpinButton != null)
                bonusSpinButton.onClick.RemoveListener(OnBonusSpinButtonClicked);

            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(OpenPauseMenu);

            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(ResumeGame);

            if (pauseCharacterSelectButton != null)
                pauseCharacterSelectButton.onClick.RemoveListener(OpenWarningBox);

            if (pauseSettingButton != null)
                pauseSettingButton.onClick.RemoveListener(OpenPauseSettings);

            if (warningYesButton != null)
                warningYesButton.onClick.RemoveListener(OnWarningYesClicked);

            if (warningNoButton != null)
                warningNoButton.onClick.RemoveListener(CloseWarningBox);

            if (pauseCloseSettingsButton != null)
                pauseCloseSettingsButton.onClick.RemoveListener(ClosePauseSettings);

            if (pauseBottomCloseSettingsButton != null)
                pauseBottomCloseSettingsButton.onClick.RemoveListener(ClosePauseSettings);

            if (pauseBgmVolumeSlider != null)
                pauseBgmVolumeSlider.onValueChanged.RemoveListener(OnPauseBgmVolumeChanged);

            if (pauseSfxVolumeSlider != null)
                pauseSfxVolumeSlider.onValueChanged.RemoveListener(OnPauseSfxVolumeChanged);

            if (pauseBgmMuteButton != null)
                pauseBgmMuteButton.onClick.RemoveListener(OnPauseBgmMuteClicked);

            if (pauseSfxMuteButton != null)
                pauseSfxMuteButton.onClick.RemoveListener(OnPauseSfxMuteClicked);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            BlockInputController.IsModalBlockingInput = false;

            if (PlayerInstance.Instance != null)
            {
                UpdatePlayerStatsDisplay(PlayerInstance.Instance.CurrentHealth, PlayerInstance.Instance.Damage);
            }
            UpdateKeyDisplay(PlayerInstance.HasKey);
            UpdateCoinsDisplay(PlayerInstance.Instance != null ? PlayerInstance.Instance.CurrentCoins : PlayerInstance.SavedCoins);
            UpdateDungeonMusicState();

            if (dungeonLevelText != null)
            {
                dungeonLevelText.text = $"<color=#00E5FF><b>DUNGEON</b></color>  {EnemySpawner.CurrentRoomLevel}";
                if (dungeonBadgeRT != null)
                {
                    float dWidth = Mathf.Clamp(dungeonLevelText.preferredWidth + 22f, 110f, 155f);
                    dungeonBadgeRT.sizeDelta = new Vector2(dWidth, 30f);
                }
            }

            if (dungeonBadgeRT != null)
            {
                dungeonBadgeRT.localScale = Vector3.zero;
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (notificationBanner != null)
            {
                notificationBanner.SetActive(false);
            }

            if (roomBonusReadyBadge != null)
            {
                roomBonusReadyBadge.SetActive(EnemySpawner.IsCurrentRoomAllEnemiesCleared);
            }

            // If all enemies were killed in the previous room before entering the ladder, open the Bonus Item modal in the next room!
            // Player clicks SPIN themselves to roll the reel:
            // ("Enemy တွေ့ အကုန်ရှင်းသွားလိုရှိရင် (နောက်တခန်းရောက်ရင် bonus item player ကနေ spin ဆိုပြီးလည့်ရပါမယ်)
            //  coing 5 ခုရှိရင် ဒဲဒီအချိန်မှာ နောက်တခါ လည့်ရပါမယ် (-5 coinနုတ်ပါမယ်)")
            if (EnemySpawner.PendingBonusSlotSpin)
            {
                EnemySpawner.PendingBonusSlotSpin = false;
                isWaitingForBonusToPlayDungeonIntro = true;
                bool isFree = EnemySpawner.WasPreviousRoomAllEnemiesCleared;
                ShowBonusSlotMachine(EnemySpawner.CurrentRoomLevel, forceLuckOverride: null, hasFreeSpin: isFree);
            }
            else
            {
                if (bonusSlotPanel != null)
                {
                    bonusSlotPanel.SetActive(false);
                }
                PlayDungeonIntro();
            }
        }

        private void Update()
        {
            if (PlayerInstance.Instance != null && !PlayerInstance.Instance.IsDead)
            {
                UpdatePlayerStatsDisplay(PlayerInstance.Instance.CurrentHealth, PlayerInstance.Instance.Damage);
            }

            if (dungeonLevelText != null)
            {
                dungeonLevelText.text = $"<color=#00E5FF><b>DUNGEON</b></color>  {EnemySpawner.CurrentRoomLevel}";
                if (dungeonBadgeRT != null)
                {
                    float dWidth = Mathf.Clamp(dungeonLevelText.preferredWidth + 22f, 110f, 155f);
                    dungeonBadgeRT.sizeDelta = new Vector2(dWidth, 30f);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (pauseSettingsPanel != null && pauseSettingsPanel.activeInHierarchy)
                {
                    ClosePauseSettings();
                }
                else if (warningConfirmPanel != null && warningConfirmPanel.activeInHierarchy)
                {
                    CloseWarningBox();
                }
                else if (pauseMenuPanel != null && pauseMenuPanel.activeInHierarchy)
                {
                    ResumeGame();
                }
                else if (gameOverPanel == null || !gameOverPanel.activeInHierarchy)
                {
                    OpenPauseMenu();
                }
            }

            UpdateDungeonMusicState();
            HandleModalDirectClickFallback();
        }

        private void UpdateDungeonMusicState()
        {
            if (PlayerInstance.Instance == null || PlayerInstance.Instance.IsDead)
            {
                return;
            }

            int playerHp = PlayerInstance.Instance.CurrentHealth;
            int totalEnemyDmg = EnemyInstance.TotalActiveEnemyDamage;

            // When total enemy damage on board >= player health (and at least 1 active enemy exists), switch to low-health danger music
            bool isDanger = totalEnemyDmg > 0 && totalEnemyDmg >= playerHp;
            UISoundManager.SetDungeonMusicDangerState(isDanger);
        }

        private void HandleModalDirectClickFallback()
        {
            if (pauseMenuPanel != null && pauseMenuPanel.activeInHierarchy)
            {
                // Pause Menu, Warning Dialog, and Settings Modal are completely handled
                // by Unity EventSystem and GraphicRaycaster with proper layered raycast blocking.
                // Performing raw ScreenPoint checks here causes click-through and double-click bugs across modals.
                return;
            }

            if (Time.unscaledTime - lastModalTransitionTime < 0.35f)
            {
                return;
            }

            if (gameOverPanel != null && gameOverPanel.activeInHierarchy)
            {
                if (CheckDirectButtonClick(restartButton, OnRestartClicked)) return;
                if (CheckDirectButtonClick(characterSelectButton, OnCharacterSelectClicked)) return;
            }

            if (bonusSlotPanel != null && bonusSlotPanel.activeInHierarchy)
            {
                if (CheckDirectButtonClick(bonusSpinButton, OnBonusSpinButtonClicked)) return;
            }
        }

        private bool CheckDirectButtonClick(Button btn, System.Action onClickAction)
        {
            if (btn == null || !btn.interactable || !btn.gameObject.activeInHierarchy) return false;
            var btnRT = btn.GetComponent<RectTransform>();
            if (btnRT == null) return false;

            var canvas = GetComponent<Canvas>();
            Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Ended && RectTransformUtility.RectangleContainsScreenPoint(btnRT, t.position, uiCam))
                    {
                        onClickAction?.Invoke();
                        return true;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(btnRT, Input.mousePosition, uiCam))
                {
                    onClickAction?.Invoke();
                    return true;
                }
            }
            return false;
        }

        private void HandlePlayerHealthChanged(int currentHp, int maxHp)
        {
            int dmg = PlayerInstance.Instance != null ? PlayerInstance.Instance.Damage : 1;
            UpdatePlayerStatsDisplay(currentHp, dmg);
        }

        private void HandlePlayerKeyChanged(bool hasKey)
        {
            UpdateKeyDisplay(hasKey);
        }

        private void HandleCoinsChanged(int currentCoins)
        {
            UpdateCoinsDisplay(currentCoins);
        }

        public void UpdateCoinsDisplay(int coins)
        {
            if (coinCountText == null) return;
            coinCountText.text = $"{coins}";
        }

        private void HandleLadderUnlocked()
        {
            if (keySprite != null && keyIconImage != null)
            {
                keyIconImage.sprite = keySprite;
                keyIconImage.color = Color.white;
            }
            if (keyStatusText != null)
            {
                keyStatusText.text = "<color=#55FF88><b>LADDER OPEN</b></color>";
            }
            if (keyOutline != null)
            {
                keyOutline.effectColor = new Color(0.2f, 1f, 0.5f, 0.9f);
            }
            if (keyContainer != null)
            {
                StartCoroutine(PopInRoutine(keyContainer.transform));
            }
        }

        public void UpdateKeyDisplay(bool hasKey)
        {
            if (keyIconImage == null || keyStatusText == null) return;

            if (hasKey)
            {
                if (keySprite != null) keyIconImage.sprite = keySprite;
                keyIconImage.color = Color.white;
                keyStatusText.text = "<color=#FFD700><b>KEY READY</b></color>";
                if (keyOutline != null)
                {
                    keyOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.85f);
                }
                if (keyContainer != null)
                {
                    StartCoroutine(PopInRoutine(keyContainer.transform));
                }
            }
            else
            {
                if (lockSprite != null) keyIconImage.sprite = lockSprite;
                keyIconImage.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
                keyStatusText.text = "<color=#778899>FIND KEY</color>";
                if (keyOutline != null)
                {
                    keyOutline.effectColor = new Color(0.4f, 0.45f, 0.55f, 0.4f);
                }
            }
        }

        private void UpdatePlayerStatsDisplay(int currentHp, int dmg)
        {
            if (playerHpText != null)
            {
                playerHpText.text = $"{Mathf.Max(0, currentHp)}";
            }

            if (playerDmgText != null)
            {
                string dmgStr = $"{dmg}";

                var activeAlly = NpcAllyInstance.GetActiveAliveAlly();
                if (activeAlly != null)
                {
                    dmgStr += $" <color=#55FF88><b>+{activeAlly.Damage}</b></color>";
                }

                var powerUp = CatPowerUpController.Instance;
                if (powerUp != null)
                {
                    switch (powerUp.CatType)
                    {
                        case CatClassType.Blaster:
                            if (powerUp.IsBlasterCritReady)
                            {
                                dmgStr += " <color=#FFD700><b>(Damage x2)</b></color>";
                                if (powerUp.BlasterFastComboCharges > 0)
                                {
                                    dmgStr += $" <color=#FFAA00>[FAST x{powerUp.BlasterFastComboCharges}]</color>";
                                }
                            }
                            else
                            {
                                dmgStr += $" <color=#AAAAAA>({powerUp.CurrentAttackCount}/{powerUp.AttacksRequired})</color>";
                                if (powerUp.BlasterFastComboCharges > 0)
                                {
                                    string chanceText = powerUp.CurrentAttackCount switch
                                    {
                                        3 => "50%",
                                        2 => "33%",
                                        _ => "25%"
                                    };
                                    dmgStr += $" <color=#FFD700>[FAST x{powerUp.BlasterFastComboCharges} ({chanceText})]</color>";
                                }
                            }
                            break;
                        case CatClassType.Brawler:
                            if (powerUp.CurrentAttackCount >= powerUp.AttacksRequired)
                            {
                                dmgStr += " <color=#FFD700><b>(Damage x1.5)</b></color>";
                            }
                            else
                            {
                                dmgStr += $" <color=#AAAAAA>({powerUp.CurrentAttackCount}/{powerUp.AttacksRequired} Combo)</color>";
                            }
                            break;
                        case CatClassType.Guardian:
                            dmgStr += $" <color=#AAAAAA>({powerUp.CurrentAttackCount}/{powerUp.AttacksRequired} Block)</color>";
                            break;
                    }

                    if (powerUp.HasOneTimeShieldBlock)
                    {
                        string shieldLabel = powerUp.ShieldCharges > 1 ? $"[SHIELD x{powerUp.ShieldCharges}]" : "[SHIELD]";
                        dmgStr += $" <color=#00E5FF><b>{shieldLabel}</b></color>";
                    }
                }

                playerDmgText.text = dmgStr;
            }

            // Fixed size card for HP, Coin and Damage info:
            // ("hp and coin (info ပြတယ် text box) fixed size လေးထားထားပါ")
            if (hudContainer != null)
            {
                var hudRT = hudContainer.GetComponent<RectTransform>();
                if (hudRT != null)
                {
                    hudRT.sizeDelta = new Vector2(215f, 62f);
                }
            }
        }

        private void HandlePowerUpNotification(string text, Color color)
        {
            if (notificationBanner == null || powerUpNoticeText == null) return;

            if (activeNoticeRoutine != null)
            {
                StopCoroutine(activeNoticeRoutine);
            }

            activeNoticeRoutine = StartCoroutine(PowerUpBannerRoutine(text, color));
        }

        private IEnumerator PowerUpBannerRoutine(string text, Color color)
        {
            notificationBanner.SetActive(true);
            powerUpNoticeText.text = text;
            powerUpNoticeText.color = color;

            var cg = notificationBanner.GetComponent<CanvasGroup>();
            var rt = notificationBanner.GetComponent<RectTransform>();
            if (rt != null)
            {
                float prefW = powerUpNoticeText.preferredWidth;
                rt.sizeDelta = new Vector2(Mathf.Clamp(prefW + 24f, 110f, 340f), 26f);
            }
            Vector2 targetPos = new Vector2(0f, 105f);
            Vector2 startPos = new Vector2(0f, 95f);

            // Smooth slide-up & fade-in at bottom
            float inDur = 0.12f;
            float t = 0f;
            while (t < inDur)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / inDur);
                if (cg != null) cg.alpha = progress;
                if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
                yield return null;
            }

            if (cg != null) cg.alpha = 1f;
            if (rt != null) rt.anchoredPosition = targetPos;

            yield return new WaitForSeconds(1.4f);

            // Smooth fade-out
            float outDur = 0.25f;
            t = 0f;
            while (t < outDur)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / outDur);
                if (cg != null) cg.alpha = 1f - progress;
                yield return null;
            }

            notificationBanner.SetActive(false);
            activeNoticeRoutine = null;
        }

        private void HandlePlayerDied()
        {
            UISoundManager.StopDungeonMusic();
            UISoundManager.PlayGameOverMusic();
            ShowGameOver();
        }

        public void ShowGameOver()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (warningConfirmPanel != null) warningConfirmPanel.SetActive(false);
            if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);

            if (gameOverPanel != null)
            {
                EnsureEventSystem();
                BlockInputController.IsModalBlockingInput = true;

                int runDungeonLevel = Mathf.Max(1, EnemySpawner.CurrentRoomLevel);
                int bestDungeonLevel = (UGSLeaderboardManager.Instance != null) ? UGSLeaderboardManager.Instance.GetLocalBestScore() : 1;
                bool isNewBest = runDungeonLevel >= bestDungeonLevel && runDungeonLevel > 1;
                string newBestBadge = isNewBest ? "\n<color=#FFD700><b>★ NEW BEST DUNGEON! ★</b></color>" : "";

                if (gameOverSubText != null)
                {
                    gameOverSubText.text = $"You fell in Dungeon {runDungeonLevel}!\n" +
                                           $"Dungeon Level: <color=#00E5FF><b>Lv. {runDungeonLevel}</b></color> (Best: Lv. {Mathf.Max(bestDungeonLevel, runDungeonLevel)}){newBestBadge}\n" +
                                           $"<color=#AAAAAA>Submitting to Cloud Leaderboard...</color>";
                }

                if (UGSLeaderboardManager.Instance != null)
                {
                    int catClass = CharacterSelectionState.SelectedCharacterIndex;
                    StartCoroutine(SubmitLeaderboardRoutine(runDungeonLevel, catClass));
                }

                gameOverPanel.transform.SetAsLastSibling();
                gameOverPanel.SetActive(true);

                if (restartButton != null)
                {
                    restartButton.onClick.RemoveAllListeners();
                    restartButton.onClick.AddListener(OnRestartClicked);
                }

                if (characterSelectButton != null)
                {
                    characterSelectButton.onClick.RemoveAllListeners();
                    characterSelectButton.onClick.AddListener(OnCharacterSelectClicked);
                }

                if (gameOverPanel.transform.childCount > 0)
                {
                    StartCoroutine(PopInRoutine(gameOverPanel.transform.GetChild(0)));
                }
                else
                {
                    StartCoroutine(PopInRoutine(gameOverPanel.transform));
                }
            }
        }

        private IEnumerator PopInRoutine(Transform target)
        {
            if (target == null) yield break;
            target.localScale = new Vector3(0.7f, 0.7f, 1f);
            float elapsed = 0f;
            float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - progress, 3f);
                target.localScale = Vector3.Lerp(new Vector3(0.7f, 0.7f, 1f), Vector3.one, ease);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private IEnumerator SubmitLeaderboardRoutine(int dungeonLevel, int catClass)
        {
            var task = UGSLeaderboardManager.Instance.SubmitScoreAsync(dungeonLevel, catClass);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (gameOverSubText != null)
            {
                int best = UGSLeaderboardManager.Instance.GetLocalBestScore();
                bool isNewBest = dungeonLevel >= best && dungeonLevel > 1;
                string newBestBadge = isNewBest ? "\n<color=#FFD700><b>★ NEW BEST DUNGEON! ★</b></color>" : "";
                string statusText = task.Result 
                    ? "<color=#55FF55>Cloud Leaderboard Synced!</color>" 
                    : "<color=#FFAA55>Offline (Saved Locally)</color>";

                gameOverSubText.text = $"You fell in Dungeon {EnemySpawner.CurrentRoomLevel}!\n" +
                                       $"Dungeon Level: <color=#00E5FF><b>Lv. {dungeonLevel}</b></color> (Best: Lv. {best}){newBestBadge}\n" +
                                       $"{statusText}";
            }
        }

        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            UISoundManager.PlayClick();
            UISoundManager.StopGameOverMusic();
            UISoundManager.StopDungeonMusic();
            BlockInputController.IsModalBlockingInput = false;

            EnemySpawner.ResetStaticData();
            PlayerInstance.ResetRunStats(25, 2);
            DestructibleBlock.ResetClearedPositions();

            if (Application.isPlaying)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void OnCharacterSelectClicked()
        {
            UISoundManager.PlayClick();
            UISoundManager.StopGameOverMusic();
            UISoundManager.StopDungeonMusic();
            BlockInputController.IsModalBlockingInput = false;

            EnemySpawner.ResetStaticData();
            PlayerInstance.ResetRunStats(25, 2);
            DestructibleBlock.ResetClearedPositions();

            MainMenuController.OpenCharacterSelectOnLoad = true;

            if (Application.isPlaying)
            {
                if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
                {
                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else if (Application.CanStreamedLevelBeLoaded("mainmenu"))
                {
                    SceneManager.LoadScene("mainmenu");
                }
                else
                {
                    SceneManager.LoadScene(0);
                }
            }
        }

        private void EnsureUIHierarchy()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(360, 640);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();

            if (hudContainer == null)
            {
                BuildHUD(transform);
            }

            if (roomBonusReadyBadge == null)
            {
                BuildRoomBonusReadyBadge(transform);
            }

            if (notificationBanner == null)
            {
                BuildNotificationBanner(transform);
            }

            if (bonusSlotPanel == null)
            {
                BuildBonusSlotMachinePanel(transform);
            }

            if (gameOverPanel == null)
            {
                BuildGameOverPanel(transform);
            }

            EnsurePauseSystem(transform);
        }

        public void OpenPauseMenu()
        {
            if (gameOverPanel != null && gameOverPanel.activeInHierarchy) return;
            if (Time.unscaledTime - lastModalTransitionTime < 0.25f) return;

            Time.timeScale = 0f;
            BlockInputController.IsModalBlockingInput = true;
            UISoundManager.PlayClick();
            lastModalTransitionTime = Time.unscaledTime;

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.transform.SetAsLastSibling();
                pauseMenuPanel.SetActive(true);
            }
            if (warningConfirmPanel != null) warningConfirmPanel.SetActive(false);
            if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        }

        public void ResumeGame()
        {
            if (Time.unscaledTime - lastModalTransitionTime < 0.2f) return;

            Time.timeScale = 1f;
            BlockInputController.IsModalBlockingInput = false;
            UISoundManager.PlayBack();
            lastModalTransitionTime = Time.unscaledTime;

            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (warningConfirmPanel != null) warningConfirmPanel.SetActive(false);
            if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        }

        public void OpenWarningBox()
        {
            if (Time.unscaledTime - lastModalTransitionTime < 0.25f) return;

            UISoundManager.PlayClick();
            lastModalTransitionTime = Time.unscaledTime;
            if (warningConfirmPanel != null)
            {
                warningConfirmPanel.transform.SetAsLastSibling();
                warningConfirmPanel.SetActive(true);
            }
        }

        public void CloseWarningBox()
        {
            if (Time.unscaledTime - lastModalTransitionTime < 0.2f) return;

            UISoundManager.PlayBack();
            lastModalTransitionTime = Time.unscaledTime;
            if (warningConfirmPanel != null)
            {
                warningConfirmPanel.SetActive(false);
            }
        }

        public void OnWarningYesClicked()
        {
            if (Time.unscaledTime - lastModalTransitionTime < 0.35f)
            {
                // Ignore accidental click-through when warning modal opens
                return;
            }

            UISoundManager.PlayClick();
            UISoundManager.StopDungeonMusic();
            UISoundManager.StopGameOverMusic();
            Time.timeScale = 1f;
            BlockInputController.IsModalBlockingInput = false;
            lastModalTransitionTime = Time.unscaledTime;

            EnemySpawner.ResetStaticData();
            PlayerInstance.ResetRunStats(30, 3);
            DestructibleBlock.ResetClearedPositions();

            MainMenuController.OpenCharacterSelectOnLoad = true;

            if (Application.isPlaying)
            {
                if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
                {
                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else if (Application.CanStreamedLevelBeLoaded("mainmenu"))
                {
                    SceneManager.LoadScene("mainmenu");
                }
                else
                {
                    SceneManager.LoadScene(0);
                }
            }
        }

        public void OpenPauseSettings()
        {
            if (Time.unscaledTime - lastModalTransitionTime < 0.35f)
            {
                // Prevent click-through from WarningConfirmPanel or rapid double-clicks
                return;
            }

            UISoundManager.PlayClick();
            lastModalTransitionTime = Time.unscaledTime;
            if (pauseSettingsPanel != null)
            {
                pauseSettingsPanel.transform.SetAsLastSibling();
                pauseSettingsPanel.SetActive(true);
            }
            UpdatePauseSettingsUI();
        }

        public void ClosePauseSettings()
        {
            if (Time.unscaledTime - lastModalTransitionTime < 0.2f) return;

            UISoundManager.PlayBack();
            lastModalTransitionTime = Time.unscaledTime;
            if (pauseSettingsPanel != null)
            {
                pauseSettingsPanel.SetActive(false);
            }
        }

        private void UpdatePauseSettingsUI()
        {
            float bgmVol = UISoundManager.BgmVolume;
            bool bgmMuted = UISoundManager.IsBgmMuted;

            if (pauseBgmVolumeSlider != null)
            {
                pauseBgmVolumeSlider.SetValueWithoutNotify(bgmVol);
                pauseBgmVolumeSlider.interactable = !bgmMuted;
            }

            if (pauseBgmVolumeText != null)
            {
                pauseBgmVolumeText.text = bgmMuted ? "MUTED" : $"{Mathf.RoundToInt(bgmVol * 100f)}%";
            }

            if (pauseBgmMuteButtonText != null)
            {
                pauseBgmMuteButtonText.text = bgmMuted ? "UNMUTE BGM" : "MUTE BGM";
            }
            if (pauseBgmMuteButton != null)
            {
                var img = pauseBgmMuteButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = bgmMuted ? new Color(0.75f, 0.22f, 0.22f, 1f) : new Color(0.20f, 0.26f, 0.38f, 1f);
                }
            }

            float sfxVol = UISoundManager.SfxVolume;
            bool sfxMuted = UISoundManager.IsSfxMuted;

            if (pauseSfxVolumeSlider != null)
            {
                pauseSfxVolumeSlider.SetValueWithoutNotify(sfxVol);
                pauseSfxVolumeSlider.interactable = !sfxMuted;
            }

            if (pauseSfxVolumeText != null)
            {
                pauseSfxVolumeText.text = sfxMuted ? "MUTED" : $"{Mathf.RoundToInt(sfxVol * 100f)}%";
            }

            if (pauseSfxMuteButtonText != null)
            {
                pauseSfxMuteButtonText.text = sfxMuted ? "UNMUTE SFX" : "MUTE SFX";
            }
            if (pauseSfxMuteButton != null)
            {
                var img = pauseSfxMuteButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = sfxMuted ? new Color(0.75f, 0.22f, 0.22f, 1f) : new Color(0.20f, 0.26f, 0.38f, 1f);
                }
            }
        }

        private void OnPauseBgmVolumeChanged(float val)
        {
            UISoundManager.SetBgmVolume(val);
            if (pauseBgmVolumeText != null)
            {
                pauseBgmVolumeText.text = UISoundManager.IsBgmMuted ? "MUTED" : $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void OnPauseBgmMuteClicked()
        {
            UISoundManager.ToggleBgmMute();
            UISoundManager.PlayClick();
            UpdatePauseSettingsUI();
        }

        private void OnPauseSfxVolumeChanged(float val)
        {
            UISoundManager.SetSfxVolume(val);
            if (pauseSfxVolumeText != null)
            {
                pauseSfxVolumeText.text = UISoundManager.IsSfxMuted ? "MUTED" : $"{Mathf.RoundToInt(val * 100f)}%";
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

        private void OnPauseSfxMuteClicked()
        {
            UISoundManager.ToggleSfxMute();
            if (!UISoundManager.IsSfxMuted)
            {
                UISoundManager.PlayClick();
            }
            UpdatePauseSettingsUI();
        }

        private void EnsurePauseSystem(Transform root)
        {
            if (pauseButton == null)
            {
                var t = root.Find("PauseButton");
                if (t != null) pauseButton = t.GetComponent<Button>();
            }
            if (pauseMenuPanel == null)
            {
                var t = root.Find("PauseMenuPanel");
                if (t != null) pauseMenuPanel = t.gameObject;
            }
            if (warningConfirmPanel == null)
            {
                var t = root.Find("WarningConfirmPanel");
                if (t != null) warningConfirmPanel = t.gameObject;
            }
            if (pauseSettingsPanel == null)
            {
                var t = root.Find("PauseSettingsPanel");
                if (t != null) pauseSettingsPanel = t.gameObject;
            }

            if (pauseMenuPanel != null)
            {
                if (resumeButton == null)
                    resumeButton = pauseMenuPanel.transform.Find("WindowFrame/ButtonColumn/Btn_Resume")?.GetComponent<Button>();
                if (pauseSettingButton == null)
                    pauseSettingButton = pauseMenuPanel.transform.Find("WindowFrame/ButtonColumn/Btn_Setting")?.GetComponent<Button>();
                if (pauseCharacterSelectButton == null)
                {
                    pauseCharacterSelectButton = pauseMenuPanel.transform.Find("WindowFrame/ButtonColumn/Btn_ResetCharacter")?.GetComponent<Button>()
                        ?? pauseMenuPanel.transform.Find("WindowFrame/ButtonColumn/Btn_CharacterSelect")?.GetComponent<Button>()
                        ?? pauseMenuPanel.transform.Find("WindowFrame/ButtonColumn/Btn_RestCharacter")?.GetComponent<Button>();
                }

                if (pauseCharacterSelectButton == null)
                {
                    var btnCol = pauseMenuPanel.transform.Find("WindowFrame/ButtonColumn");
                    if (btnCol != null)
                    {
                        var btnObj = new GameObject("Btn_ResetCharacter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                        btnObj.transform.SetParent(btnCol, false);
                        var rt = btnObj.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(240f, 44f);
                        var img = btnObj.GetComponent<Image>();
                        img.color = new Color(0.85f, 0.32f, 0.25f, 1f);
                        var outline = btnObj.GetComponent<Outline>();
                        outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
                        outline.effectDistance = new Vector2(1.5f, -1.5f);
                        var btn = btnObj.GetComponent<Button>();
                        btn.targetGraphic = img;

                        var lblObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                        lblObj.transform.SetParent(btnObj.transform, false);
                        var lRT = lblObj.GetComponent<RectTransform>();
                        lRT.anchorMin = Vector2.zero;
                        lRT.anchorMax = Vector2.one;
                        lRT.offsetMin = Vector2.zero;
                        lRT.offsetMax = Vector2.zero;
                        var txt = lblObj.GetComponent<Text>();
                        txt.font = runtimeFont;
                        txt.fontStyle = FontStyle.Bold;
                        txt.fontSize = 15;
                        txt.color = Color.white;
                        txt.alignment = TextAnchor.MiddleCenter;
                        txt.text = "↺  RESET CHARACTER";

                        pauseCharacterSelectButton = btn;

                        var colRT = btnCol.GetComponent<RectTransform>();
                        if (colRT != null && colRT.sizeDelta.y < 150f)
                        {
                            colRT.sizeDelta = new Vector2(240f, 156f);
                            colRT.anchoredPosition = new Vector2(0f, -22f);
                        }
                        var winRT = pauseMenuPanel.transform.Find("WindowFrame")?.GetComponent<RectTransform>();
                        if (winRT != null && winRT.sizeDelta.y < 250f)
                        {
                            winRT.sizeDelta = new Vector2(280f, 260f);
                        }
                    }
                }
            }

            if (warningConfirmPanel == null)
            {
                BuildWarningConfirmPanel(root);
            }
            else
            {
                if (warningYesButton == null)
                    warningYesButton = warningConfirmPanel.transform.Find("WindowFrame/ButtonRow/Btn_WarningYes")?.GetComponent<Button>();
                if (warningNoButton == null)
                    warningNoButton = warningConfirmPanel.transform.Find("WindowFrame/ButtonRow/Btn_WarningNo")?.GetComponent<Button>();
            }

            if (pauseCharacterSelectButton != null)
            {
                pauseCharacterSelectButton.onClick.RemoveListener(OpenWarningBox);
                pauseCharacterSelectButton.onClick.AddListener(OpenWarningBox);
            }
            if (warningYesButton != null)
            {
                warningYesButton.onClick.RemoveListener(OnWarningYesClicked);
                warningYesButton.onClick.AddListener(OnWarningYesClicked);
            }
            if (warningNoButton != null)
            {
                warningNoButton.onClick.RemoveListener(CloseWarningBox);
                warningNoButton.onClick.AddListener(CloseWarningBox);
            }

            if (pauseSettingsPanel != null)
            {
                if (pauseCloseSettingsButton == null)
                    pauseCloseSettingsButton = pauseSettingsPanel.transform.Find("WindowFrame/Btn_CloseX")?.GetComponent<Button>();
                if (pauseBgmVolumeSlider == null)
                    pauseBgmVolumeSlider = pauseSettingsPanel.transform.Find("WindowFrame/BgmGroup/BgmSlider")?.GetComponent<Slider>();
                if (pauseBgmVolumeText == null)
                    pauseBgmVolumeText = pauseSettingsPanel.transform.Find("WindowFrame/BgmGroup/BgmValueText")?.GetComponent<Text>();
                if (pauseBgmMuteButton == null)
                    pauseBgmMuteButton = pauseSettingsPanel.transform.Find("WindowFrame/BgmGroup/Btn_BgmMute")?.GetComponent<Button>();
                if (pauseBgmMuteButtonText == null)
                    pauseBgmMuteButtonText = pauseBgmMuteButton?.GetComponentInChildren<Text>();
                if (pauseSfxVolumeSlider == null)
                    pauseSfxVolumeSlider = pauseSettingsPanel.transform.Find("WindowFrame/SfxGroup/SfxSlider")?.GetComponent<Slider>();
                if (pauseSfxVolumeText == null)
                    pauseSfxVolumeText = pauseSettingsPanel.transform.Find("WindowFrame/SfxGroup/SfxValueText")?.GetComponent<Text>();
                if (pauseSfxMuteButton == null)
                    pauseSfxMuteButton = pauseSettingsPanel.transform.Find("WindowFrame/SfxGroup/Btn_SfxMute")?.GetComponent<Button>();
                if (pauseSfxMuteButtonText == null)
                    pauseSfxMuteButtonText = pauseSfxMuteButton?.GetComponentInChildren<Text>();
            }

            // Keep modals hidden until opened
            if (pauseMenuPanel != null && !pauseMenuPanel.activeInHierarchy) pauseMenuPanel.SetActive(false);
            if (warningConfirmPanel != null && !warningConfirmPanel.activeInHierarchy) warningConfirmPanel.SetActive(false);
            if (pauseSettingsPanel != null && !pauseSettingsPanel.activeInHierarchy) pauseSettingsPanel.SetActive(false);
        }

        private void BuildWarningConfirmPanel(Transform root)
        {
            var warnPanelObj = new GameObject("WarningConfirmPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            warnPanelObj.transform.SetParent(root, false);
            var wPanelRT = warnPanelObj.GetComponent<RectTransform>();
            wPanelRT.anchorMin = Vector2.zero;
            wPanelRT.anchorMax = Vector2.one;
            wPanelRT.offsetMin = Vector2.zero;
            wPanelRT.offsetMax = Vector2.zero;

            var wPanelImg = warnPanelObj.GetComponent<Image>();
            wPanelImg.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);
            wPanelImg.raycastTarget = true;

            var wWindowObj = new GameObject("WindowFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            wWindowObj.transform.SetParent(warnPanelObj.transform, false);
            var wWindowRT = wWindowObj.GetComponent<RectTransform>();
            wWindowRT.anchorMin = new Vector2(0.5f, 0.5f);
            wWindowRT.anchorMax = new Vector2(0.5f, 0.5f);
            wWindowRT.pivot = new Vector2(0.5f, 0.5f);
            wWindowRT.anchoredPosition = new Vector2(0f, 35f);
            wWindowRT.sizeDelta = new Vector2(290f, 210f);

            var wWindowImg = wWindowObj.GetComponent<Image>();
            wWindowImg.color = new Color(0.11f, 0.14f, 0.22f, 0.98f);
            wWindowImg.raycastTarget = true;

            var wWindowOutline = wWindowObj.GetComponent<Outline>();
            wWindowOutline.effectColor = new Color(0.85f, 0.35f, 0.25f, 0.9f);
            wWindowOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            var wTitleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            wTitleObj.transform.SetParent(wWindowObj.transform, false);
            var wTitleRT = wTitleObj.GetComponent<RectTransform>();
            wTitleRT.anchorMin = new Vector2(0.5f, 1f);
            wTitleRT.anchorMax = new Vector2(0.5f, 1f);
            wTitleRT.pivot = new Vector2(0.5f, 1f);
            wTitleRT.anchoredPosition = new Vector2(0f, -14f);
            wTitleRT.sizeDelta = new Vector2(250f, 30f);

            var wTitleTxt = wTitleObj.GetComponent<Text>();
            wTitleTxt.font = runtimeFont;
            wTitleTxt.fontStyle = FontStyle.Bold;
            wTitleTxt.fontSize = 20;
            wTitleTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            wTitleTxt.alignment = TextAnchor.MiddleCenter;
            wTitleTxt.text = "RESET RUN?";

            var wTitleShadow = wTitleObj.GetComponent<Shadow>();
            wTitleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            wTitleShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Divider
            var wDivObj = new GameObject("HeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            wDivObj.transform.SetParent(wWindowObj.transform, false);
            var wDivRT = wDivObj.GetComponent<RectTransform>();
            wDivRT.anchorMin = new Vector2(0.5f, 1f);
            wDivRT.anchorMax = new Vector2(0.5f, 1f);
            wDivRT.pivot = new Vector2(0.5f, 1f);
            wDivRT.anchoredPosition = new Vector2(0f, -48f);
            wDivRT.sizeDelta = new Vector2(250f, 2f);
            wDivObj.GetComponent<Image>().color = new Color(0.28f, 0.38f, 0.52f, 0.6f);

            // Message Text
            var wMsgObj = new GameObject("MessageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            wMsgObj.transform.SetParent(wWindowObj.transform, false);
            var wMsgRT = wMsgObj.GetComponent<RectTransform>();
            wMsgRT.anchorMin = new Vector2(0.5f, 0.5f);
            wMsgRT.anchorMax = new Vector2(0.5f, 0.5f);
            wMsgRT.pivot = new Vector2(0.5f, 0.5f);
            wMsgRT.anchoredPosition = new Vector2(0f, -8f);
            wMsgRT.sizeDelta = new Vector2(250f, 70f);

            var wMsgTxt = wMsgObj.GetComponent<Text>();
            wMsgTxt.font = runtimeFont;
            wMsgTxt.fontStyle = FontStyle.Normal;
            wMsgTxt.fontSize = 14;
            wMsgTxt.lineSpacing = 1.15f;
            wMsgTxt.color = new Color(0.88f, 0.92f, 0.98f, 1f);
            wMsgTxt.alignment = TextAnchor.MiddleCenter;
            wMsgTxt.text = "Current run will restart from Dungeon 1!\n\nProceed to Character Selection?";

            // Button Row
            var wBtnRowObj = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            wBtnRowObj.transform.SetParent(wWindowObj.transform, false);
            var wBtnRowRT = wBtnRowObj.GetComponent<RectTransform>();
            wBtnRowRT.anchorMin = new Vector2(0.5f, 0f);
            wBtnRowRT.anchorMax = new Vector2(0.5f, 0f);
            wBtnRowRT.pivot = new Vector2(0.5f, 0f);
            wBtnRowRT.anchoredPosition = new Vector2(0f, 16f);
            wBtnRowRT.sizeDelta = new Vector2(250f, 42f);

            var hGroup = wBtnRowObj.GetComponent<HorizontalLayoutGroup>();
            hGroup.spacing = 14f;
            hGroup.childAlignment = TextAnchor.MiddleCenter;
            hGroup.childControlWidth = false;
            hGroup.childControlHeight = false;

            warningNoButton = CreateRuntimeButton(wBtnRowObj.transform, "Btn_WarningNo", "NO", new Color(0.24f, 0.32f, 0.45f, 1f), new Vector2(115f, 40f));
            warningYesButton = CreateRuntimeButton(wBtnRowObj.transform, "Btn_WarningYes", "YES", new Color(0.85f, 0.30f, 0.25f, 1f), new Vector2(115f, 40f));

            warningConfirmPanel = warnPanelObj;
            warningConfirmPanel.SetActive(false);
        }

        private Button CreateRuntimeButton(Transform parent, string name, string labelText, Color color, Vector2 size)
        {
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            btnObj.transform.SetParent(parent, false);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = btnObj.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            var outline = btnObj.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObj.transform.SetParent(btnObj.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var txt = labelObj.GetComponent<Text>();
            txt.font = runtimeFont;
            txt.fontStyle = FontStyle.Bold;
            txt.fontSize = 15;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = labelText;

            return btn;
        }

        private void EnsureEventSystem()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            }

            if (es == null)
            {
                var esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            else if (es.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void EnsureSlotIcons()
        {
            if (heartIconSprite == null)
                heartIconSprite = LoadIconSprite("Icons/Icon_Heart", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Heart.png");
            if (swordIconSprite == null)
                swordIconSprite = LoadIconSprite("Icons/Icon_Sword", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Sword.png");
            if (shieldIconSprite == null)
                shieldIconSprite = LoadIconSprite("Icons/Icon_Shield", "Assets/Resources/Icons/Icon_Shield.png");
            if (cloverIconSprite == null)
                cloverIconSprite = LoadIconSprite("Icons/Icon_Reward_Pass_clover", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/IconMisc/Icon_Reward_Pass_clover.png");
        }

        private static Sprite LoadIconSprite(string resourceName, string fallbackPath)
        {
            var sp = Resources.Load<Sprite>(resourceName);
            if (sp != null) return sp;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(fallbackPath))
            {
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath);
            }
#endif
            return sp;
        }

        private void BuildHUD(Transform root)
        {
            hudContainer = new GameObject("PlayerHUD");
            hudContainer.transform.SetParent(root, false);
            var hudRT = hudContainer.AddComponent<RectTransform>();
            hudRT.anchorMin = new Vector2(1f, 0f);
            hudRT.anchorMax = new Vector2(1f, 0f);
            hudRT.pivot = new Vector2(1f, 0f);
            hudRT.anchoredPosition = new Vector2(-12f, 15f);
            // Fixed size card for HP and Coin info:
            // ("hp and coin (info ပြတယ် text box) fixed size လေးထားထားပါ")
            hudRT.sizeDelta = new Vector2(215f, 62f);

            // Background Card
            var bg = hudContainer.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.90f);
            var outline = hudContainer.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.6f, 1f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var heartSprite = LoadIconSprite("Icons/Icon_Heart", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Heart.png");
            var swordSprite = LoadIconSprite("Icons/Icon_Sword", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Sword.png");
            keySprite = LoadIconSprite("Icons/Icon_Key_Gold", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Key_Gold.png");
            lockSprite = LoadIconSprite("Icons/Icon_Lock", "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_ItemIcons/128/Icon_Lock.png");

            // HP Row (Top Half)
            var hpRowObj = new GameObject("HP_Row");
            hpRowObj.transform.SetParent(hudContainer.transform, false);
            var hpRowRT = hpRowObj.AddComponent<RectTransform>();
            hpRowRT.anchorMin = new Vector2(0f, 0.5f);
            hpRowRT.anchorMax = new Vector2(1f, 1f);
            hpRowRT.offsetMin = new Vector2(10f, 0f);
            hpRowRT.offsetMax = new Vector2(-10f, 0f);

            // Heart Icon
            var hpIconObj = new GameObject("HpIcon");
            hpIconObj.transform.SetParent(hpRowObj.transform, false);
            var hpIconRT = hpIconObj.AddComponent<RectTransform>();
            hpIconRT.anchorMin = new Vector2(0f, 0.5f);
            hpIconRT.anchorMax = new Vector2(0f, 0.5f);
            hpIconRT.pivot = new Vector2(0f, 0.5f);
            hpIconRT.anchoredPosition = new Vector2(0f, 0f);
            hpIconRT.sizeDelta = new Vector2(20f, 20f);

            hpIconImage = hpIconObj.AddComponent<Image>();
            hpIconImage.sprite = heartSprite;
            hpIconImage.preserveAspect = true;
            hpIconImage.raycastTarget = false;

            // HP Text
            var hpTextObj = new GameObject("HpText");
            hpTextObj.transform.SetParent(hpRowObj.transform, false);
            var hpTextRT = hpTextObj.AddComponent<RectTransform>();
            hpTextRT.anchorMin = new Vector2(0f, 0.5f);
            hpTextRT.anchorMax = new Vector2(0f, 0.5f);
            hpTextRT.pivot = new Vector2(0f, 0.5f);
            hpTextRT.anchoredPosition = new Vector2(24f, 0f);
            hpTextRT.sizeDelta = new Vector2(40f, 24f);

            playerHpText = hpTextObj.AddComponent<Text>();
            playerHpText.font = runtimeFont;
            playerHpText.fontSize = 17;
            playerHpText.fontStyle = FontStyle.Bold;
            playerHpText.alignment = TextAnchor.MiddleLeft;
            playerHpText.color = Color.white;
            playerHpText.text = "25";
            var hpShadow = hpTextObj.AddComponent<Shadow>();
            hpShadow.effectColor = Color.black;
            hpShadow.effectDistance = new Vector2(1f, -1f);

            // Coin Icon (Beside Heart Text in HUD)
            // ("Coin total (text) (max coing 5 ကိုယ်လဲဖြတ်ပေးပါ) ရတယ်ဟာကိုယ် Heart count ရဲဘေးမှာ ထားပေးပါ")
            if (coinSprite == null)
            {
                coinSprite = LoadIconSprite("Icons/Icon_Coin", "Assets/Resources/Icons/Icon_Coin.png");
            }
            var coinIconObj = new GameObject("CoinIcon");
            coinIconObj.transform.SetParent(hpRowObj.transform, false);
            coinIconRT = coinIconObj.AddComponent<RectTransform>();
            coinIconRT.anchorMin = new Vector2(0f, 0.5f);
            coinIconRT.anchorMax = new Vector2(0f, 0.5f);
            coinIconRT.pivot = new Vector2(0f, 0.5f);
            coinIconRT.anchoredPosition = new Vector2(78f, 0f);
            coinIconRT.sizeDelta = new Vector2(18f, 18f);

            coinIconImage = coinIconObj.AddComponent<Image>();
            coinIconImage.sprite = coinSprite;
            coinIconImage.preserveAspect = true;
            coinIconImage.raycastTarget = false;

            // Coin Text (Beside Coin Icon in HUD)
            var coinTextObj = new GameObject("CoinText");
            coinTextObj.transform.SetParent(hpRowObj.transform, false);
            coinTextRT = coinTextObj.AddComponent<RectTransform>();
            coinTextRT.anchorMin = new Vector2(0f, 0.5f);
            coinTextRT.anchorMax = new Vector2(0f, 0.5f);
            coinTextRT.pivot = new Vector2(0f, 0.5f);
            coinTextRT.anchoredPosition = new Vector2(100f, 0f);
            coinTextRT.sizeDelta = new Vector2(90f, 24f);

            coinCountText = coinTextObj.AddComponent<Text>();
            coinCountText.font = runtimeFont;
            coinCountText.fontSize = 16;
            coinCountText.fontStyle = FontStyle.Bold;
            coinCountText.alignment = TextAnchor.MiddleLeft;
            coinCountText.color = new Color(1f, 0.85f, 0.2f, 1f);
            coinCountText.text = $"{PlayerInstance.SavedCoins}";
            var coinShadow = coinTextObj.AddComponent<Shadow>();
            coinShadow.effectColor = Color.black;
            coinShadow.effectDistance = new Vector2(1f, -1f);

            // DMG Row (Bottom Half)
            var dmgRowObj = new GameObject("DMG_Row");
            dmgRowObj.transform.SetParent(hudContainer.transform, false);
            var dmgRowRT = dmgRowObj.AddComponent<RectTransform>();
            dmgRowRT.anchorMin = new Vector2(0f, 0f);
            dmgRowRT.anchorMax = new Vector2(1f, 0.5f);
            dmgRowRT.offsetMin = new Vector2(10f, 0f);
            dmgRowRT.offsetMax = new Vector2(-10f, 0f);

            // Sword Icon
            var dmgIconObj = new GameObject("DmgIcon");
            dmgIconObj.transform.SetParent(dmgRowObj.transform, false);
            var dmgIconRT = dmgIconObj.AddComponent<RectTransform>();
            dmgIconRT.anchorMin = new Vector2(0f, 0.5f);
            dmgIconRT.anchorMax = new Vector2(0f, 0.5f);
            dmgIconRT.pivot = new Vector2(0f, 0.5f);
            dmgIconRT.anchoredPosition = new Vector2(0f, 0f);
            dmgIconRT.sizeDelta = new Vector2(20f, 20f);

            dmgIconImage = dmgIconObj.AddComponent<Image>();
            dmgIconImage.sprite = swordSprite;
            dmgIconImage.preserveAspect = true;
            dmgIconImage.raycastTarget = false;

            // DMG Text
            var dmgTextObj = new GameObject("DmgText");
            dmgTextObj.transform.SetParent(dmgRowObj.transform, false);
            var dmgTextRT = dmgTextObj.AddComponent<RectTransform>();
            dmgTextRT.anchorMin = new Vector2(0f, 0f);
            dmgTextRT.anchorMax = new Vector2(1f, 1f);
            dmgTextRT.offsetMin = new Vector2(28f, 0f);
            dmgTextRT.offsetMax = Vector2.zero;

            playerDmgText = dmgTextObj.AddComponent<Text>();
            playerDmgText.font = runtimeFont;
            playerDmgText.fontSize = 15;
            playerDmgText.fontStyle = FontStyle.Bold;
            playerDmgText.alignment = TextAnchor.MiddleLeft;
            playerDmgText.color = new Color(1f, 0.78f, 0.25f, 1f);
            playerDmgText.text = "2";
            playerDmgText.resizeTextForBestFit = true;
            playerDmgText.resizeTextMinSize = 11;
            playerDmgText.resizeTextMaxSize = 15;
            var dmgShadow = dmgTextObj.AddComponent<Shadow>();
            dmgShadow.effectColor = Color.black;
            dmgShadow.effectDistance = new Vector2(1f, -1f);

            // Dungeon Level Badge (Starts in Middle Center, glides under player)
            dungeonBadgeObj = new GameObject("DungeonLevelBadge");
            dungeonBadgeObj.transform.SetParent(root, false);
            dungeonBadgeRT = dungeonBadgeObj.AddComponent<RectTransform>();
            dungeonBadgeRT.anchorMin = new Vector2(0.5f, 0.5f);
            dungeonBadgeRT.anchorMax = new Vector2(0.5f, 0.5f);
            dungeonBadgeRT.pivot = new Vector2(0.5f, 0.5f);
            dungeonBadgeRT.anchoredPosition = Vector2.zero; // Start at Middle Center
            dungeonBadgeRT.sizeDelta = new Vector2(130f, 30f);
            dungeonBadgeRT.localScale = Vector3.zero; // Start hidden until intro animation plays

            var dungeonBg = dungeonBadgeObj.AddComponent<Image>();
            dungeonBg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);
            var dungeonOutline = dungeonBadgeObj.AddComponent<Outline>();
            dungeonOutline.effectColor = new Color(0f, 0.85f, 1f, 0.65f);
            dungeonOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var dungeonTextObj = new GameObject("Text");
            dungeonTextObj.transform.SetParent(dungeonBadgeObj.transform, false);
            var dungeonTextRT = dungeonTextObj.AddComponent<RectTransform>();
            dungeonTextRT.anchorMin = Vector2.zero;
            dungeonTextRT.anchorMax = Vector2.one;
            dungeonTextRT.offsetMin = Vector2.zero;
            dungeonTextRT.offsetMax = Vector2.zero;

            dungeonLevelText = dungeonTextObj.AddComponent<Text>();
            dungeonLevelText.font = runtimeFont;
            dungeonLevelText.fontSize = 15;
            dungeonLevelText.fontStyle = FontStyle.Bold;
            dungeonLevelText.alignment = TextAnchor.MiddleCenter;
            dungeonLevelText.color = Color.white;
            dungeonLevelText.text = $"<color=#00E5FF><b>DUNGEON</b></color>  {EnemySpawner.CurrentRoomLevel}";

            var dungeonShadow = dungeonTextObj.AddComponent<Shadow>();
            dungeonShadow.effectColor = Color.black;
            dungeonShadow.effectDistance = new Vector2(1.2f, -1.2f);
        }

        public void PlayDungeonIntro()
        {
            if (activeDungeonIntroRoutine != null)
            {
                StopCoroutine(activeDungeonIntroRoutine);
            }
            activeDungeonIntroRoutine = StartCoroutine(DungeonIntroRoutine());
        }

        private IEnumerator DungeonIntroRoutine()
        {
            if (dungeonBadgeRT == null)
            {
                activeDungeonIntroRoutine = null;
                yield break;
            }

            // Start in Middle Center (center of screen)
            dungeonBadgeRT.anchoredPosition = Vector2.zero;
            dungeonBadgeRT.localScale = Vector3.zero;

            if (dungeonLevelText != null)
            {
                dungeonLevelText.text = $"<color=#00E5FF><b>DUNGEON</b></color>  {EnemySpawner.CurrentRoomLevel}";
                if (dungeonBadgeRT != null)
                {
                    float dWidth = Mathf.Clamp(dungeonLevelText.preferredWidth + 22f, 110f, 155f);
                    dungeonBadgeRT.sizeDelta = new Vector2(dWidth, 30f);
                }
            }

            // 1. Pop-in scale up in Middle Center
            float elapsed = 0f;
            float popDuration = 0.28f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float scale = Mathf.Lerp(0f, 1.25f, Mathf.Sin(t * Mathf.PI * 0.5f));
                dungeonBadgeRT.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            dungeonBadgeRT.localScale = new Vector3(1.15f, 1.15f, 1f);

            // 2. Pause in Middle Center
            yield return new WaitForSeconds(0.65f);

            // 3. Smooth glide down from Middle Center to below player character
            Vector2 startPos = dungeonBadgeRT.anchoredPosition;
            Vector3 startScale = dungeonBadgeRT.localScale;
            Vector3 targetScale = Vector3.one;

            elapsed = 0f;
            float glideDuration = 0.65f;
            while (elapsed < glideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / glideDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector2 targetCanvasPos = CalculateUnderPlayerCanvasPos();
                dungeonBadgeRT.anchoredPosition = Vector2.Lerp(startPos, targetCanvasPos, smoothT);
                dungeonBadgeRT.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                yield return null;
            }

            Vector2 finalCanvasPos = CalculateUnderPlayerCanvasPos();
            dungeonBadgeRT.anchoredPosition = finalCanvasPos;
            dungeonBadgeRT.localScale = Vector3.one;

            // 4. Gentle settle bounce on landing
            elapsed = 0f;
            float bounceDuration = 0.16f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                dungeonBadgeRT.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            dungeonBadgeRT.localScale = Vector3.one;
            activeDungeonIntroRoutine = null;
        }

        private Vector2 CalculateUnderPlayerCanvasPos()
        {
            var player = PlayerInstance.Instance;
            Vector3 playerWorldPos = player != null ? player.transform.position : new Vector3(-3.2f, -8.2f, 0f);

            // Position below the player character, shifted toward the left side:
            // ("Dungeon(Text left side နည်းနည်းတိုးပေးပါ)")
            Vector3 targetWorld = playerWorldPos + new Vector3(-0.35f, -1.35f, 0f);

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 screenPt = cam.WorldToScreenPoint(targetWorld);
                var rootCanvas = GetComponent<Canvas>();
                var canvasRT = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
                Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                    ? (rootCanvas.worldCamera != null ? rootCanvas.worldCamera : cam) 
                    : null;
                if (canvasRT != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPt, uiCam, out Vector2 localPoint))
                {
                    localPoint.x = Mathf.Clamp(localPoint.x - 15f, -180f + 65f, 180f - 65f);
                    localPoint.y = Mathf.Clamp(localPoint.y, -320f + 25f, 320f - 25f);
                    return localPoint;
                }
            }

            return new Vector2(-110f, -280f);
        }

        private void BuildNotificationBanner(Transform root)
        {
            notificationBanner = new GameObject("NotificationBanner");
            notificationBanner.transform.SetParent(root, false);
            var rt = notificationBanner.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 85f);
            rt.sizeDelta = new Vector2(240f, 26f);

            var bg = notificationBanner.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.72f);
            bg.raycastTarget = false;

            notificationBanner.AddComponent<CanvasGroup>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(notificationBanner.transform, false);
            var textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10f, 0f);
            textRT.offsetMax = new Vector2(-10f, 0f);

            powerUpNoticeText = textObj.AddComponent<Text>();
            powerUpNoticeText.font = runtimeFont;
            powerUpNoticeText.fontSize = 13;
            powerUpNoticeText.fontStyle = FontStyle.Bold;
            powerUpNoticeText.alignment = TextAnchor.MiddleCenter;
            powerUpNoticeText.color = Color.yellow;
            powerUpNoticeText.text = "POWER UP!";
            powerUpNoticeText.supportRichText = true;
            powerUpNoticeText.resizeTextForBestFit = true;
            powerUpNoticeText.resizeTextMinSize = 10;
            powerUpNoticeText.resizeTextMaxSize = 13;
            powerUpNoticeText.raycastTarget = false;

            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);

            notificationBanner.SetActive(false);
        }

        private void BuildGameOverPanel(Transform root)
        {
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(root, false);
            var panelRT = gameOverPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            // Fullscreen dark crimson defeat overlay
            var overlay = gameOverPanel.AddComponent<Image>();
            overlay.color = new Color(0.12f, 0.02f, 0.04f, 0.90f);

            // Dialog Card
            var card = new GameObject("Card");
            card.transform.SetParent(gameOverPanel.transform, false);
            var cardRT = card.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(320f, 350f);

            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.22f, 0.25f, 0.85f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Title "☠ GAME OVER ☠"
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(card.transform, false);
            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.04f, 0.85f);
            titleRT.anchorMax = new Vector2(0.96f, 0.96f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.font = runtimeFont;
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.32f, 0.32f, 1f);
            titleText.text = "☠ GAME OVER ☠";
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 18;
            titleText.resizeTextMaxSize = 24;
            var titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = Color.black;
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // Subtitle
            var subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(card.transform, false);
            var subRT = subObj.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.05f, 0.47f);
            subRT.anchorMax = new Vector2(0.95f, 0.83f);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;

            gameOverSubText = subObj.AddComponent<Text>();
            gameOverSubText.font = runtimeFont;
            gameOverSubText.fontSize = 13;
            gameOverSubText.alignment = TextAnchor.MiddleCenter;
            gameOverSubText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            gameOverSubText.text = "You were defeated in combat!";

            // Restart Button
            var restartBtnObj = new GameObject("Btn_Restart");
            restartBtnObj.transform.SetParent(card.transform, false);
            var restartRT = restartBtnObj.AddComponent<RectTransform>();
            restartRT.anchorMin = new Vector2(0.10f, 0.26f);
            restartRT.anchorMax = new Vector2(0.90f, 0.41f);
            restartRT.offsetMin = Vector2.zero;
            restartRT.offsetMax = Vector2.zero;

            var restartImg = restartBtnObj.AddComponent<Image>();
            restartImg.color = new Color(0.85f, 0.22f, 0.22f, 1f);
            var rOutline = restartBtnObj.AddComponent<Outline>();
            rOutline.effectColor = new Color(1f, 0.45f, 0.45f, 0.75f);
            rOutline.effectDistance = new Vector2(1.5f, -1.5f);

            restartButton = restartBtnObj.AddComponent<Button>();
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);

            var restartTextObj = new GameObject("Text");
            restartTextObj.transform.SetParent(restartBtnObj.transform, false);
            var restartTextRT = restartTextObj.AddComponent<RectTransform>();
            restartTextRT.anchorMin = Vector2.zero;
            restartTextRT.anchorMax = Vector2.one;
            restartTextRT.offsetMin = Vector2.zero;
            restartTextRT.offsetMax = Vector2.zero;

            var rText = restartTextObj.AddComponent<Text>();
            rText.font = runtimeFont;
            rText.fontSize = 15;
            rText.fontStyle = FontStyle.Bold;
            rText.alignment = TextAnchor.MiddleCenter;
            rText.color = Color.white;
            rText.text = "RESTART RUN (LEVEL 1)";
            rText.resizeTextForBestFit = true;
            rText.resizeTextMinSize = 11;
            rText.resizeTextMaxSize = 15;
            rText.raycastTarget = false;

            // Character Select Button
            var selectBtnObj = new GameObject("Btn_CharacterSelect");
            selectBtnObj.transform.SetParent(card.transform, false);
            var selectRT = selectBtnObj.AddComponent<RectTransform>();
            selectRT.anchorMin = new Vector2(0.10f, 0.07f);
            selectRT.anchorMax = new Vector2(0.90f, 0.22f);
            selectRT.offsetMin = Vector2.zero;
            selectRT.offsetMax = Vector2.zero;

            var selectImg = selectBtnObj.AddComponent<Image>();
            selectImg.color = new Color(0.18f, 0.48f, 0.88f, 1f);
            var sOutline = selectBtnObj.AddComponent<Outline>();
            sOutline.effectColor = new Color(0.4f, 0.75f, 1f, 0.75f);
            sOutline.effectDistance = new Vector2(1.5f, -1.5f);

            characterSelectButton = selectBtnObj.AddComponent<Button>();
            characterSelectButton.onClick.RemoveAllListeners();
            characterSelectButton.onClick.AddListener(OnCharacterSelectClicked);

            var selectTextObj = new GameObject("Text");
            selectTextObj.transform.SetParent(selectBtnObj.transform, false);
            var selectTextRT = selectTextObj.AddComponent<RectTransform>();
            selectTextRT.anchorMin = Vector2.zero;
            selectTextRT.anchorMax = Vector2.one;
            selectTextRT.offsetMin = Vector2.zero;
            selectTextRT.offsetMax = Vector2.zero;

            var sText = selectTextObj.AddComponent<Text>();
            sText.font = runtimeFont;
            sText.fontSize = 15;
            sText.fontStyle = FontStyle.Bold;
            sText.alignment = TextAnchor.MiddleCenter;
            sText.color = Color.white;
            sText.text = "CHARACTER SELECT";
            sText.resizeTextForBestFit = true;
            sText.resizeTextMinSize = 11;
            sText.resizeTextMaxSize = 15;
            sText.raycastTarget = false;

            gameOverPanel.SetActive(false);
        }

        private void HandleRoomAllEnemiesCleared()
        {
            if (roomBonusReadyBadge != null)
            {
                roomBonusReadyBadge.SetActive(false);
            }

            UISoundManager.PlayRoomAllEnemiesCleared();
            // Show clean short message near bottom
            HandlePowerUpNotification("<color=#FFE040><b>★ ALL ENEMIES KILLED! BONUS READY ★</b></color>", new Color(1f, 0.88f, 0.25f));
        }

        private void BuildRoomBonusReadyBadge(Transform root)
        {
            // Replaced by bottom short message notification
            roomBonusReadyBadge = null;
        }

        /// <summary>
        /// Builds the Horizontal Slot Machine modal UI ("အလျားလိုက် slot machine").
        /// </summary>
        private void BuildBonusSlotMachinePanel(Transform root)
        {
            bonusSlotPanel = new GameObject("BonusSlotMachinePanel");
            bonusSlotPanel.transform.SetParent(root, false);
            var panelRT = bonusSlotPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var overlay = bonusSlotPanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.84f);

            // Main Dialog Card
            var card = new GameObject("SlotCardModal");
            card.transform.SetParent(bonusSlotPanel.transform, false);
            var cardRT = card.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(340f, 360f);

            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.10f, 0.17f, 0.98f);
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.82f, 0.22f, 0.9f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Header Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(card.transform, false);
            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.05f, 0.83f);
            titleRT.anchorMax = new Vector2(0.95f, 0.96f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.font = runtimeFont;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.86f, 0.25f, 1f);
            titleText.text = "★ BONUS ITEM ★";
            titleText.raycastTarget = false;
            bonusSlotTitleText = titleText;
            var titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = Color.black;
            titleShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Subtitle
            var subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(card.transform, false);
            var subRT = subObj.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.05f, 0.73f);
            subRT.anchorMax = new Vector2(0.95f, 0.83f);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;

            bonusSlotSubText = subObj.AddComponent<Text>();
            bonusSlotSubText.font = runtimeFont;
            bonusSlotSubText.fontSize = 11;
            bonusSlotSubText.fontStyle = FontStyle.Bold;
            bonusSlotSubText.alignment = TextAnchor.MiddleCenter;
            bonusSlotSubText.color = new Color(0.75f, 0.92f, 1f, 1f);
            bonusSlotSubText.text = "ALL ENEMIES CLEARED! SPIN FOR NEXT ROOM BONUS";
            bonusSlotSubText.raycastTarget = false;

            // Horizontal Slot Window (Viewport with RectMask2D)
            var viewportObj = new GameObject("HorizontalReelViewport");
            viewportObj.transform.SetParent(card.transform, false);
            var viewportRT = viewportObj.AddComponent<RectTransform>();
            viewportRT.anchorMin = new Vector2(0.5f, 0.5f);
            viewportRT.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRT.pivot = new Vector2(0.5f, 0.5f);
            viewportRT.anchoredPosition = new Vector2(0f, 8f);
            viewportRT.sizeDelta = new Vector2(310f, 132f);

            var viewportBg = viewportObj.AddComponent<Image>();
            viewportBg.color = new Color(0.04f, 0.05f, 0.09f, 1f);
            viewportBg.raycastTarget = false;
            viewportObj.AddComponent<RectMask2D>();

            var viewportOutline = viewportObj.AddComponent<Outline>();
            viewportOutline.effectColor = new Color(0.2f, 0.65f, 1f, 0.75f);
            viewportOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Horizontal Reel Strip inside Viewport
            var stripObj = new GameObject("ReelStrip");
            stripObj.transform.SetParent(viewportObj.transform, false);
            bonusReelStripRT = stripObj.AddComponent<RectTransform>();
            bonusReelStripRT.anchorMin = new Vector2(0.5f, 0.5f);
            bonusReelStripRT.anchorMax = new Vector2(0.5f, 0.5f);
            bonusReelStripRT.pivot = new Vector2(0.5f, 0.5f);
            bonusReelStripRT.anchoredPosition = Vector2.zero;
            bonusReelStripRT.sizeDelta = new Vector2(2000f, 120f);

            // Center Highlight Frame (indicates the winning slot in the middle)
            var selectorObj = new GameObject("CenterSelectorFrame");
            selectorObj.transform.SetParent(card.transform, false);
            var selectorRT = selectorObj.AddComponent<RectTransform>();
            selectorRT.anchorMin = new Vector2(0.5f, 0.5f);
            selectorRT.anchorMax = new Vector2(0.5f, 0.5f);
            selectorRT.pivot = new Vector2(0.5f, 0.5f);
            selectorRT.anchoredPosition = new Vector2(0f, 8f);
            selectorRT.sizeDelta = new Vector2(114f, 138f);

            var selectorImg = selectorObj.AddComponent<Image>();
            selectorImg.color = new Color(1f, 0.88f, 0.2f, 0.10f);
            selectorImg.raycastTarget = false;
            var selectorOutline = selectorObj.AddComponent<Outline>();
            selectorOutline.effectColor = new Color(1f, 0.88f, 0.2f, 0.98f);
            selectorOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // Top & Bottom Pointer Arrows on Center Selector
            var pointerTopObj = new GameObject("TopPointer");
            pointerTopObj.transform.SetParent(selectorObj.transform, false);
            var ptRT = pointerTopObj.AddComponent<RectTransform>();
            ptRT.anchorMin = new Vector2(0.5f, 1f);
            ptRT.anchorMax = new Vector2(0.5f, 1f);
            ptRT.pivot = new Vector2(0.5f, 0.5f);
            ptRT.anchoredPosition = new Vector2(0f, 4f);
            ptRT.sizeDelta = new Vector2(40f, 22f);
            var ptTxt = pointerTopObj.AddComponent<Text>();
            ptTxt.font = runtimeFont;
            ptTxt.fontSize = 16;
            ptTxt.fontStyle = FontStyle.Bold;
            ptTxt.alignment = TextAnchor.MiddleCenter;
            ptTxt.color = new Color(1f, 0.88f, 0.2f, 1f);
            ptTxt.text = "▼";
            ptTxt.raycastTarget = false;

            // Result Text
            var resultObj = new GameObject("ResultText");
            resultObj.transform.SetParent(card.transform, false);
            var resultRT = resultObj.AddComponent<RectTransform>();
            resultRT.anchorMin = new Vector2(0.05f, 0.21f);
            resultRT.anchorMax = new Vector2(0.95f, 0.31f);
            resultRT.offsetMin = Vector2.zero;
            resultRT.offsetMax = Vector2.zero;

            bonusSlotResultText = resultObj.AddComponent<Text>();
            bonusSlotResultText.font = runtimeFont;
            bonusSlotResultText.fontSize = 13;
            bonusSlotResultText.fontStyle = FontStyle.Bold;
            bonusSlotResultText.alignment = TextAnchor.MiddleCenter;
            bonusSlotResultText.color = Color.white;
            bonusSlotResultText.text = "PRESS SPIN TO ROLL YOUR BONUS!";
            bonusSlotResultText.resizeTextForBestFit = true;
            bonusSlotResultText.resizeTextMinSize = 10;
            bonusSlotResultText.resizeTextMaxSize = 13;
            bonusSlotResultText.raycastTarget = false;

            // SPIN Button
            var spinBtnObj = new GameObject("Btn_SpinBonus");
            spinBtnObj.transform.SetParent(card.transform, false);
            var spinRT = spinBtnObj.AddComponent<RectTransform>();
            spinRT.anchorMin = new Vector2(0.14f, 0.04f);
            spinRT.anchorMax = new Vector2(0.86f, 0.18f);
            spinRT.offsetMin = Vector2.zero;
            spinRT.offsetMax = Vector2.zero;

            var spinImg = spinBtnObj.AddComponent<Image>();
            spinImg.color = new Color(0.95f, 0.68f, 0.12f, 1f);
            spinImg.raycastTarget = true;
            bonusSpinButton = spinBtnObj.AddComponent<Button>();
            bonusSpinButton.targetGraphic = spinImg;
            bonusSpinButton.onClick.RemoveListener(OnBonusSpinButtonClicked);
            bonusSpinButton.onClick.AddListener(OnBonusSpinButtonClicked);

            var spinTxtObj = new GameObject("Text");
            spinTxtObj.transform.SetParent(spinBtnObj.transform, false);
            var spinTxtRT = spinTxtObj.AddComponent<RectTransform>();
            spinTxtRT.anchorMin = Vector2.zero;
            spinTxtRT.anchorMax = Vector2.one;
            spinTxtRT.offsetMin = Vector2.zero;
            spinTxtRT.offsetMax = Vector2.zero;

            bonusSpinButtonText = spinTxtObj.AddComponent<Text>();
            bonusSpinButtonText.font = runtimeFont;
            bonusSpinButtonText.fontSize = 15;
            bonusSpinButtonText.fontStyle = FontStyle.Bold;
            bonusSpinButtonText.alignment = TextAnchor.MiddleCenter;
            bonusSpinButtonText.color = new Color(0.08f, 0.08f, 0.12f, 1f);
            bonusSpinButtonText.text = "SPIN BONUS!";
            bonusSpinButtonText.resizeTextForBestFit = true;
            bonusSpinButtonText.resizeTextMinSize = 11;
            bonusSpinButtonText.resizeTextMaxSize = 15;
            bonusSpinButtonText.raycastTarget = false;

            bonusSlotPanel.SetActive(false);
        }

        /// <summary>
        /// Calculates the scaled slot machine pool for the given room level:
        /// - Damage +1 (+1 per 5 room levels)
        /// - HP +1 (+1 per 5 room levels)
        /// - HP +2 (+1 per 5 room levels)
        /// - Shield +1 (for this next level)
        /// - Shield x2 stacked (for this next level)
        /// - Empty / ဗလာ (x2 slots)
        /// - Luck (50% chance to appear in the pool, uses Icon_Reward_Pass_clover, +1~3 item drops)
        /// </summary>
        public static System.Collections.Generic.List<BonusSlotEntry> CreateBonusSlotPool(int roomLevel, bool? forceLuckOverride = null)
        {
            int stepBonus = Mathf.Max(0, roomLevel / 5);
            int dmgVal = 1 + stepBonus;
            int hpSmallVal = 1 + stepBonus;
            int hpLargeVal = 2 + stepBonus;

            var pool = new System.Collections.Generic.List<BonusSlotEntry>
            {
                new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.Damage,
                    value = dmgVal,
                    label = $"DMG +{dmgVal}",
                    accentColor = new Color(1f, 0.78f, 0.22f, 1f)
                },
                new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.HpSmall,
                    value = hpSmallVal,
                    label = $"HP +{hpSmallVal}",
                    accentColor = new Color(0.3f, 0.95f, 0.5f, 1f)
                },
                new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.HpLarge,
                    value = hpLargeVal,
                    label = $"HP +{hpLargeVal}",
                    accentColor = new Color(0.2f, 1f, 0.65f, 1f)
                },
                new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.Shield1,
                    value = 1,
                    label = "SHIELD +1",
                    accentColor = new Color(0.2f, 0.85f, 1f, 1f)
                },
                new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.Shield2,
                    value = 2,
                    label = "SHIELD x2",
                    accentColor = new Color(0f, 0.95f, 1f, 1f)
                },
                new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.Empty,
                    value = 0,
                    label = "EMPTY (ဗလာ)",
                    accentColor = new Color(0.5f, 0.52f, 0.58f, 1f)
                },
                new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.Empty,
                    value = 0,
                    label = "EMPTY (ဗလာ)",
                    accentColor = new Color(0.5f, 0.52f, 0.58f, 1f)
                }
            };

            // 50% chance for Luck (Reward_Pass_clover) to be included in the slot machine
            bool includeLuck = forceLuckOverride ?? (Random.value < 0.5f);
            if (includeLuck)
            {
                pool.Add(new BonusSlotEntry
                {
                    rewardType = BonusSlotRewardType.Luck,
                    value = 1,
                    label = "★ LUCK ★",
                    accentColor = new Color(0.45f, 1f, 0.35f, 1f)
                });
            }

            return pool;
        }

        public void ShowBonusSlotMachine(int roomLevel, bool? forceLuckOverride = null, bool hasFreeSpin = true)
        {
            isFirstFreeSpin = hasFreeSpin;
            isSlotSpinning = false;
            hasCompletedSlotSpin = false;

            if (bonusSlotPanel == null)
            {
                BuildBonusSlotMachinePanel(transform);
            }

            EnsureSlotIcons();
            BlockInputController.IsModalBlockingInput = true;

            if (dungeonBadgeRT != null && (isWaitingForBonusToPlayDungeonIntro || dungeonBadgeRT.localScale == Vector3.zero))
            {
                dungeonBadgeRT.localScale = Vector3.zero;
            }

            currentSlotPool.Clear();
            currentSlotPool.AddRange(CreateBonusSlotPool(roomLevel, forceLuckOverride));

            bool hasLuckInPool = currentSlotPool.Exists(e => e.rewardType == BonusSlotRewardType.Luck);

            if (bonusSlotTitleText != null)
            {
                bonusSlotTitleText.text = "★ BONUS ITEM ★";
            }

            if (bonusSlotSubText != null)
            {
                if (hasFreeSpin)
                {
                    bonusSlotSubText.text = hasLuckInPool
                        ? "ALL ENEMIES CLEARED! CLICK SPIN FOR FREE REWARD • LUCK CLOVER ACTIVE!"
                        : "ALL ENEMIES CLEARED! CLICK SPIN FOR FREE BONUS REWARD!";
                }
                else
                {
                    bonusSlotSubText.text = hasLuckInPool
                        ? "5 COINS AVAILABLE! CLICK SPIN TO ROLL • LUCK CLOVER ACTIVE!"
                        : "5 COINS AVAILABLE! CLICK SPIN TO ROLL FOR -5 COINS!";
                }
            }

            if (bonusSlotResultText != null)
            {
                bonusSlotResultText.color = Color.white;
                bonusSlotResultText.text = "CLICK SPIN TO ROLL YOUR BONUS!";
            }

            if (bonusSpinButton != null)
            {
                bonusSpinButton.interactable = true;
                bonusSpinButton.onClick.RemoveListener(OnBonusSpinButtonClicked);
                bonusSpinButton.onClick.AddListener(OnBonusSpinButtonClicked);
            }
            if (bonusSpinButtonText != null)
            {
                bonusSpinButtonText.text = hasFreeSpin ? "SPIN (FREE)" : "SPIN (-5 COINS)";
            }

            EnsureEventSystem();
            PopulateHorizontalReelStrip(currentSlotPool);

            bonusSlotPanel.transform.SetAsLastSibling();
            bonusSlotPanel.SetActive(true);
            StartCoroutine(PopInRoutine(bonusSlotPanel.transform.GetChild(0)));
        }

        private void PopulateHorizontalReelStrip(System.Collections.Generic.List<BonusSlotEntry> pool)
        {
            if (bonusReelStripRT == null) return;

            for (int i = bonusReelStripRT.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(bonusReelStripRT.GetChild(i).gameObject);
                else
                    DestroyImmediate(bonusReelStripRT.GetChild(i).gameObject);
            }
            spawnedReelCards.Clear();

            if (pool == null || pool.Count == 0) return;

            const int totalCards = 32;
            const float cardStep = 116f;

            for (int i = 0; i < totalCards; i++)
            {
                var entry = pool[i % pool.Count];
                var cardRT = CreateSingleSlotCard(bonusReelStripRT, entry, i);
                cardRT.anchoredPosition = new Vector2(i * cardStep, 0f);
                spawnedReelCards.Add(cardRT);
            }

            // Center on card index 2 initially
            bonusReelStripRT.anchoredPosition = new Vector2(-2f * cardStep, 0f);
        }

        private RectTransform CreateSingleSlotCard(Transform parent, BonusSlotEntry entry, int index)
        {
            var cardObj = new GameObject($"SlotCard_{index}_{entry.rewardType}");
            cardObj.transform.SetParent(parent, false);
            var rt = cardObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(104f, 114f);

            var bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.11f, 0.14f, 0.22f, 0.96f);
            bg.raycastTarget = false;

            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = new Color(entry.accentColor.r, entry.accentColor.g, entry.accentColor.b, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Icon area
            if (entry.rewardType == BonusSlotRewardType.Shield2)
            {
                // Stacked 2x Shield icons ("Shield x2 ထပ်နေတယ်ဟာပါမယ်")
                var iconBackObj = new GameObject("ShieldBack");
                iconBackObj.transform.SetParent(cardObj.transform, false);
                var backRT = iconBackObj.AddComponent<RectTransform>();
                backRT.anchorMin = new Vector2(0.5f, 0.62f);
                backRT.anchorMax = new Vector2(0.5f, 0.62f);
                backRT.pivot = new Vector2(0.5f, 0.5f);
                backRT.anchoredPosition = new Vector2(-10f, -3f);
                backRT.sizeDelta = new Vector2(44f, 44f);
                var backImg = iconBackObj.AddComponent<Image>();
                backImg.sprite = shieldIconSprite;
                backImg.color = new Color(0.75f, 0.92f, 1f, 0.85f);
                backImg.preserveAspect = true;
                backImg.raycastTarget = false;

                var iconFrontObj = new GameObject("ShieldFront");
                iconFrontObj.transform.SetParent(cardObj.transform, false);
                var frontRT = iconFrontObj.AddComponent<RectTransform>();
                frontRT.anchorMin = new Vector2(0.5f, 0.62f);
                frontRT.anchorMax = new Vector2(0.5f, 0.62f);
                frontRT.pivot = new Vector2(0.5f, 0.5f);
                frontRT.anchoredPosition = new Vector2(10f, 4f);
                frontRT.sizeDelta = new Vector2(48f, 48f);
                var frontImg = iconFrontObj.AddComponent<Image>();
                frontImg.sprite = shieldIconSprite;
                frontImg.color = Color.white;
                frontImg.preserveAspect = true;
                frontImg.raycastTarget = false;
            }
            else if (entry.rewardType == BonusSlotRewardType.Empty)
            {
                var emptyTxtObj = new GameObject("EmptySymbol");
                emptyTxtObj.transform.SetParent(cardObj.transform, false);
                var emptyRT = emptyTxtObj.AddComponent<RectTransform>();
                emptyRT.anchorMin = new Vector2(0f, 0.38f);
                emptyRT.anchorMax = new Vector2(1f, 0.90f);
                emptyRT.offsetMin = Vector2.zero;
                emptyRT.offsetMax = Vector2.zero;
                var emptyTxt = emptyTxtObj.AddComponent<Text>();
                emptyTxt.font = runtimeFont;
                emptyTxt.fontSize = 28;
                emptyTxt.fontStyle = FontStyle.Bold;
                emptyTxt.alignment = TextAnchor.MiddleCenter;
                emptyTxt.color = new Color(0.55f, 0.58f, 0.65f, 0.85f);
                emptyTxt.text = "—";
                emptyTxt.raycastTarget = false;
            }
            else
            {
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(cardObj.transform, false);
                var iconRT = iconObj.AddComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.5f, 0.62f);
                iconRT.anchorMax = new Vector2(0.5f, 0.62f);
                iconRT.pivot = new Vector2(0.5f, 0.5f);
                iconRT.anchoredPosition = Vector2.zero;
                iconRT.sizeDelta = new Vector2(48f, 48f);

                var img = iconObj.AddComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;

                switch (entry.rewardType)
                {
                    case BonusSlotRewardType.Damage:
                        img.sprite = swordIconSprite;
                        break;
                    case BonusSlotRewardType.HpSmall:
                    case BonusSlotRewardType.HpLarge:
                        img.sprite = heartIconSprite;
                        break;
                    case BonusSlotRewardType.Shield1:
                        img.sprite = shieldIconSprite;
                        break;
                    case BonusSlotRewardType.Luck:
                        img.sprite = cloverIconSprite;
                        break;
                }
            }

            // Label at bottom of card
            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(cardObj.transform, false);
            var lblRT = lblObj.AddComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0.04f, 0.06f);
            lblRT.anchorMax = new Vector2(0.96f, 0.34f);
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;

            var lbl = lblObj.AddComponent<Text>();
            lbl.font = runtimeFont;
            lbl.fontSize = 13;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = entry.accentColor;
            lbl.text = entry.label;
            lbl.raycastTarget = false;

            return rt;
        }

        public void OnBonusSpinButtonClicked()
        {
            if (Time.frameCount == lastBonusSpinTriggerFrame) return;
            lastBonusSpinTriggerFrame = Time.frameCount;

            if (hasCompletedSlotSpin)
            {
                var curPlayer = PlayerInstance.Instance;
                int curCoins = curPlayer != null ? curPlayer.CurrentCoins : PlayerInstance.SavedCoins;

                if (curCoins >= 5)
                {
                    // Player has >= 5 coins and clicked SPIN AGAIN!
                    // Deduct 5 coins and spin again:
                    // ("coing 5 ခုရှိရင် ဒဲဒီအချိန်မှာ နောက်တခါ လည့်ရပါမယ် (-5 coinနုတ်ပါမယ်)")
                    bool consumed = curPlayer != null && curPlayer.TryConsumeCoins(5);
                    if (consumed)
                    {
                        Debug.Log($"<color=gold>[BonusSlotMachine] Consumed 5 coins for extra spin! Remaining coins: {curPlayer.CurrentCoins}</color>");
                        hasCompletedSlotSpin = false;
                        isFirstFreeSpin = false;
                        isSlotSpinning = true;

                        if (bonusSpinButton != null) bonusSpinButton.interactable = false;
                        if (bonusSpinButtonText != null) bonusSpinButtonText.text = "SPINNING...";
                        if (bonusSlotSubText != null) bonusSlotSubText.text = "SPINNING REEL FOR -5 COINS...";

                        UISoundManager.PlaySpinBonus();
                        if (currentSlotPool.Count == 0)
                        {
                            currentSlotPool.AddRange(CreateBonusSlotPool(EnemySpawner.CurrentRoomLevel));
                        }

                        int chosenPoolIdx = Random.Range(0, currentSlotPool.Count);
                        BonusSlotEntry winningEntry = currentSlotPool[chosenPoolIdx];
                        StartCoroutine(SpinHorizontalSlotRoutine(winningEntry));
                        return;
                    }
                }

                // If < 5 coins or continue clicked, close modal and start dungeon
                UISoundManager.PlayClick();
                CloseBonusSlotMachine();
                return;
            }

            if (isSlotSpinning) return;

            // 1st Spin:
            if (!isFirstFreeSpin)
            {
                var curPlayer = PlayerInstance.Instance;
                if (curPlayer != null && !curPlayer.TryConsumeCoins(5))
                {
                    CatPowerUpController.TriggerNotification("NOT ENOUGH COINS (NEED 5)!", Color.yellow);
                    CloseBonusSlotMachine();
                    return;
                }
            }

            isFirstFreeSpin = false;
            isSlotSpinning = true;
            if (bonusSpinButton != null) bonusSpinButton.interactable = false;
            if (bonusSpinButtonText != null) bonusSpinButtonText.text = "SPINNING...";

            UISoundManager.PlaySpinBonus();
            if (currentSlotPool.Count == 0)
            {
                currentSlotPool.AddRange(CreateBonusSlotPool(EnemySpawner.CurrentRoomLevel));
            }

            int poolIdx = Random.Range(0, currentSlotPool.Count);
            BonusSlotEntry winEntry = currentSlotPool[poolIdx];
            StartCoroutine(SpinHorizontalSlotRoutine(winEntry));
        }

        private IEnumerator SpinHorizontalSlotRoutine(BonusSlotEntry winningEntry)
        {
            isSlotSpinning = true;
            if (bonusSpinButton != null) bonusSpinButton.interactable = false;
            if (bonusSpinButtonText != null) bonusSpinButtonText.text = "SPINNING...";
            if (bonusSlotResultText != null)
            {
                bonusSlotResultText.color = new Color(1f, 0.9f, 0.3f, 1f);
                bonusSlotResultText.text = "ROLLING...";
            }

            const float cardStep = 116f;
            const int targetCardIndex = 24;

            // Replace card at targetCardIndex so it matches winningEntry
            if (bonusReelStripRT != null && targetCardIndex < spawnedReelCards.Count)
            {
                if (spawnedReelCards[targetCardIndex] != null)
                {
                    Destroy(spawnedReelCards[targetCardIndex].gameObject);
                }
                var winCardRT = CreateSingleSlotCard(bonusReelStripRT, winningEntry, targetCardIndex);
                winCardRT.anchoredPosition = new Vector2(targetCardIndex * cardStep, 0f);
                spawnedReelCards[targetCardIndex] = winCardRT;
            }

            float startX = -2f * cardStep;
            float endX = -targetCardIndex * cardStep;
            float duration = 2.0f;
            float elapsed = 0f;
            int lastReelTickIndex = -2;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Cubic ease-out for realistic slot reel deceleration
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                float currentX = Mathf.Lerp(startX, endX, easeOut);
                if (bonusReelStripRT != null)
                {
                    bonusReelStripRT.anchoredPosition = new Vector2(currentX, 0f);
                }

                // Audio: tick whenever items scroll past the center pointer
                int currentReelIndex = Mathf.RoundToInt(-currentX / cardStep);
                if (currentReelIndex != lastReelTickIndex)
                {
                    lastReelTickIndex = currentReelIndex;
                    float tickPitch = Mathf.Lerp(1.25f, 0.9f, easeOut);
                    UISoundManager.PlaySlotReelTick(tickPitch);
                }

                yield return null;
            }

            if (bonusReelStripRT != null)
            {
                bonusReelStripRT.anchoredPosition = new Vector2(endX, 0f);
            }

            // Audio: triumphant fanfare when winning item lands and is revealed
            UISoundManager.PlaySlotItemReveal();

            // Apply the won reward!
            ApplyBonusSlotReward(winningEntry);

            // Punch winning card
            if (targetCardIndex < spawnedReelCards.Count && spawnedReelCards[targetCardIndex] != null)
            {
                StartCoroutine(PopInRoutine(spawnedReelCards[targetCardIndex]));
            }

            isSlotSpinning = false;
            hasCompletedSlotSpin = true;

            var endPlayer = PlayerInstance.Instance;
            int endCoins = endPlayer != null ? endPlayer.CurrentCoins : PlayerInstance.SavedCoins;

            if (endCoins >= 5)
            {
                // Player has 5 or more coins -> can spin again for -5 coins!
                // ("coing 5 ခုရှိရင် ဒဲဒီအချိန်မှာ နောက်တခါ လည့်ရပါမယ် (-5 coinနုတ်ပါမယ်)")
                if (bonusSpinButton != null) bonusSpinButton.interactable = true;
                if (bonusSpinButtonText != null) bonusSpinButtonText.text = "SPIN AGAIN (-5 COINS)";
                if (bonusSlotSubText != null)
                {
                    bonusSlotSubText.text = $"<color=#FFD700><b>{endCoins} COINS REMAINING!</b></color> CLICK SPIN AGAIN (-5 COINS) OR ✕ TO ENTER";
                }
            }
            else
            {
                // Coins < 5 -> show CONTINUE button
                if (bonusSpinButton != null) bonusSpinButton.interactable = true;
                if (bonusSpinButtonText != null) bonusSpinButtonText.text = "CONTINUE";
                if (bonusSlotSubText != null)
                {
                    bonusSlotSubText.text = "BONUS COMPLETE! CLICK CONTINUE TO ENTER THE DUNGEON!";
                }
            }
        }

        public void ApplyBonusSlotReward(BonusSlotEntry winningEntry)
        {
            var player = PlayerInstance.Instance;
            var powerUp = CatPowerUpController.Instance;
            if (powerUp == null && player != null)
            {
                powerUp = player.GetComponent<CatPowerUpController>();
            }

            string resultMsg = "";
            Color resultColor = winningEntry.accentColor;

            switch (winningEntry.rewardType)
            {
                case BonusSlotRewardType.Damage:
                    if (player != null) player.IncreaseDamage(winningEntry.value);
                    resultMsg = $"WON: DAMAGE +{winningEntry.value}!";
                    CatPowerUpController.TriggerNotification($"BONUS: +{winningEntry.value} DMG!", resultColor);
                    break;

                case BonusSlotRewardType.HpSmall:
                case BonusSlotRewardType.HpLarge:
                    if (player != null) player.Heal(winningEntry.value);
                    resultMsg = $"WON: HP +{winningEntry.value}!";
                    CatPowerUpController.TriggerNotification($"BONUS: +{winningEntry.value} HP!", resultColor);
                    break;

                case BonusSlotRewardType.Shield1:
                case BonusSlotRewardType.Shield2:
                    if (powerUp != null)
                    {
                        powerUp.GrantOneTimeShield(winningEntry.value);
                    }
                    int curShields = powerUp != null ? powerUp.ShieldCharges : winningEntry.value;
                    resultMsg = $"WON: SHIELD +{winningEntry.value} ({curShields}/{CatPowerUpController.MaxShieldCharges})!";
                    CatPowerUpController.TriggerNotification($"BONUS: SHIELD +{winningEntry.value} ({curShields}/{CatPowerUpController.MaxShieldCharges})", resultColor);
                    break;

                case BonusSlotRewardType.Luck:
                    int spawnedExtras = EnemySpawner.ActivateLuckBonus();
                    resultMsg = "WON: LUCK! (DMG, HP, SHIELD, COIN & CAT ALLY BOOSTED!)";
                    CatPowerUpController.TriggerNotification("LUCK BONUS! ALL DROPS & CAT ALLY BOOSTED!", resultColor);
                    break;

                case BonusSlotRewardType.Empty:
                default:
                    resultMsg = "EMPTY (ဗလာ) — NO BONUS THIS TIME!";
                    CatPowerUpController.TriggerNotification("SLOT: EMPTY (ဗလာ)", resultColor);
                    break;
            }

            if (bonusSlotResultText != null)
            {
                bonusSlotResultText.color = resultColor;
                bonusSlotResultText.text = resultMsg;
            }

            if (player != null)
            {
                UpdatePlayerStatsDisplay(player.CurrentHealth, player.Damage);
            }

            Debug.Log($"<color=yellow>[BonusSlotMachine] Reward Applied: {winningEntry.rewardType} (Value: {winningEntry.value}) -> {resultMsg}</color>");
        }

        public void CloseBonusSlotMachine()
        {
            if (bonusSlotPanel != null)
            {
                bonusSlotPanel.SetActive(false);
            }
            isSlotSpinning = false;
            BlockInputController.IsModalBlockingInput = false;

            if (isWaitingForBonusToPlayDungeonIntro)
            {
                isWaitingForBonusToPlayDungeonIntro = false;
                PlayDungeonIntro();
            }
        }
    }
}
