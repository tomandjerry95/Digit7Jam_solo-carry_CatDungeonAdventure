using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Digit7s.Services;

namespace Digit7s.UI
{
    /// <summary>
    /// Controls the Leaderboard UI and the Player Name input popup on the Main Menu.
    /// Can build its UI dynamically at runtime or bind to serialized references.
    /// </summary>
    public class LeaderboardUIController : MonoBehaviour
    {
        public static LeaderboardUIController Instance { get; private set; }

        private enum TabType
        {
            AllTime,
            Weekly
        }

        [Header("Leaderboard Panel References (Optional - auto built if null)")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private Text playerInfoText;
        [SerializeField] private Button tabAllTimeButton;
        [SerializeField] private Button tabWeeklyButton;
        [SerializeField] private Image tabAllTimeIndicator;
        [SerializeField] private Image tabWeeklyIndicator;
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private Text statusText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button refreshButton;

        [Header("Name Input Dialog References (Optional - auto built if null)")]
        [SerializeField] private GameObject nameInputPanel;
        [SerializeField] private InputField nameInputField;
        [SerializeField] private Text deviceSuffixLabel;
        [SerializeField] private Text nameValidationText;
        [SerializeField] private Button confirmNameButton;

        [Header("Cat Class Icons (0: Blaster, 1: Brawler, 2: Guardian)")]
        [SerializeField] private Sprite blasterIcon;
        [SerializeField] private Sprite brawlerIcon;
        [SerializeField] private Sprite guardianIcon;

        private TabType currentTab = TabType.AllTime;
        private bool isFetching = false;
        private Font runtimeFont;
        private Sprite uiSprite;

        private void Awake()
        {
            Instance = this;
            LoadBuiltinResources();
        }

        private void Start()
        {
            EnsureUIBuilt();

            // Check if player hasn't entered a name yet
            if (UGSLeaderboardManager.Instance != null && !UGSLeaderboardManager.Instance.HasSetPlayerName())
            {
                ShowNameInputDialog();
            }
            else
            {
                if (nameInputPanel != null) nameInputPanel.SetActive(false);
            }

            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(false);
            }
        }

        private void LoadBuiltinResources()
        {
            if (runtimeFont == null)
            {
                runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
#if UNITY_EDITOR
            if (uiSprite == null)
            {
                uiSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }
#endif

            if (blasterIcon == null || brawlerIcon == null || guardianIcon == null)
            {
                var db = Resources.Load<Digit7s.Characters.CharacterDatabase>("CharacterDatabase");
                if (db != null)
                {
                    if (blasterIcon == null && db.GetCharacter(0) != null) blasterIcon = db.GetCharacter(0).characterIcon;
                    if (brawlerIcon == null && db.GetCharacter(1) != null) brawlerIcon = db.GetCharacter(1).characterIcon;
                    if (guardianIcon == null && db.GetCharacter(2) != null) guardianIcon = db.GetCharacter(2).characterIcon;
                }
            }
        }

        public Sprite GetCatIcon(int index)
        {
            switch (index)
            {
                case 1: return brawlerIcon;
                case 2: return guardianIcon;
                default: return blasterIcon;
            }
        }

        private Color GetCatBadgeColor(int index)
        {
            switch (index)
            {
                case 0: return new Color(0.9f, 0.7f, 0.15f, 0.45f); // Blaster Warm Gold
                case 1: return new Color(0.9f, 0.25f, 0.25f, 0.45f); // Brawler Crimson
                case 2: return new Color(0.15f, 0.65f, 0.95f, 0.45f); // Guardian Cyan/Blue
                default: return new Color(0.25f, 0.3f, 0.4f, 0.4f);
            }
        }

        #region Public Interface

        public void OpenLeaderboard()
        {
            EnsureUIBuilt();
            UISoundManager.PlayClick();

            if (leaderboardPanel != null)
            {
                leaderboardPanel.transform.SetAsLastSibling();
                leaderboardPanel.SetActive(true);
            }

            UpdatePlayerInfoDisplay();
            SwitchTab(TabType.AllTime);
        }

        public void CloseLeaderboard()
        {
            UISoundManager.PlayBack();
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(false);
            }
        }

        public void ShowNameInputDialog()
        {
            EnsureUIBuilt();
            if (nameInputPanel != null)
            {
                nameInputPanel.transform.SetAsLastSibling();
                nameInputPanel.SetActive(true);

                if (nameInputField != null)
                {
                    nameInputField.text = UGSLeaderboardManager.Instance != null 
                        ? UGSLeaderboardManager.Instance.GetRawPlayerName() 
                        : "";
                    nameInputField.ActivateInputField();
                }

                if (deviceSuffixLabel != null)
                {
                    string suffix = UGSLeaderboardManager.GetDeviceSuffix();
                    deviceSuffixLabel.text = $"<color=#AAAAAA>Unique Device ID Tag:</color> <color=#00E5FF><b>_{suffix}</b></color> (auto-attached)";
                }

                if (nameValidationText != null)
                {
                    nameValidationText.text = "";
                }
            }
        }

        public void CloseNameInputDialog()
        {
            UISoundManager.PlayBack();
            if (nameInputPanel != null)
            {
                nameInputPanel.SetActive(false);
            }
        }

        #endregion

        #region Name Input Logic

        public async void OnConfirmNameClicked()
        {
            if (nameInputField == null) return;

            string entered = nameInputField.text.Trim();
            if (string.IsNullOrEmpty(entered) || entered.Length < 2)
            {
                if (nameValidationText != null)
                {
                    nameValidationText.text = "<color=#FF4444>Please enter at least 2 characters!</color>";
                }
                return;
            }

            UISoundManager.PlayClick();
            if (nameValidationText != null)
            {
                nameValidationText.text = "<color=#00E5FF>Saving & Registering with Cloud...</color>";
            }

            if (UGSLeaderboardManager.Instance != null)
            {
                await UGSLeaderboardManager.Instance.SetPlayerNameAsync(entered);
            }

            UpdatePlayerInfoDisplay();

            if (nameInputPanel != null)
            {
                nameInputPanel.SetActive(false);
            }
        }

        private void UpdatePlayerInfoDisplay()
        {
            if (playerInfoText != null && UGSLeaderboardManager.Instance != null)
            {
                string uniqueName = UGSLeaderboardManager.Instance.GetUniquePlayerName();
                playerInfoText.text = $"Hero: <color=#00E5FF><b>{uniqueName}</b></color>";
            }
        }

        #endregion

        #region Tabs & Fetching

        public void SwitchTabAllTime()
        {
            SwitchTab(TabType.AllTime);
        }

        public void SwitchTabWeekly()
        {
            SwitchTab(TabType.Weekly);
        }

        private void SwitchTab(TabType tab)
        {
            currentTab = tab;
            UISoundManager.PlayClick();

            if (tabAllTimeIndicator != null)
            {
                tabAllTimeIndicator.color = tab == TabType.AllTime 
                    ? new Color(0.18f, 0.65f, 0.95f, 1f) 
                    : new Color(0.2f, 0.25f, 0.35f, 0.8f);
            }
            if (tabWeeklyIndicator != null)
            {
                tabWeeklyIndicator.color = tab == TabType.Weekly 
                    ? new Color(0.18f, 0.65f, 0.95f, 1f) 
                    : new Color(0.2f, 0.25f, 0.35f, 0.8f);
            }

            FetchCurrentLeaderboard();
        }

        public void OnRefreshClicked()
        {
            UISoundManager.PlayClick();
            FetchCurrentLeaderboard();
        }

        private async void FetchCurrentLeaderboard()
        {
            if (isFetching) return;
            isFetching = true;

            ClearEntries();
            SetStatus("Loading Cloud Leaderboard...", true);

            string leaderboardId = currentTab == TabType.AllTime 
                ? UGSLeaderboardManager.Instance.AllTimeLeaderboardId 
                : UGSLeaderboardManager.Instance.WeeklyLeaderboardId;

            List<LeaderboardEntryData> scores = null;
            if (UGSLeaderboardManager.Instance != null)
            {
                scores = await UGSLeaderboardManager.Instance.GetScoresAsync(leaderboardId, 25);
            }

            isFetching = false;

            if (scores == null)
            {
                SetStatus("Unable to connect to Leaderboard.\nPlease check your Internet or UGS Setup.", true);
                return;
            }

            if (scores.Count == 0)
            {
                string tabName = currentTab == TabType.AllTime ? "All-Time" : "Weekly";
                SetStatus($"No scores on {tabName} Leaderboard yet.\nBe the first hero to clear dungeons!", true);
                return;
            }

            SetStatus("", false);
            PopulateEntries(scores);
        }

        private void SetStatus(string message, bool isVisible)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.gameObject.SetActive(isVisible);
            }
        }

        private void ClearEntries()
        {
            if (entriesContainer == null) return;
            for (int i = entriesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(entriesContainer.GetChild(i).gameObject);
            }
        }

        private void PopulateEntries(List<LeaderboardEntryData> scores)
        {
            if (entriesContainer == null) return;

            foreach (var entry in scores)
            {
                CreateEntryRow(entry);
            }
        }

        private void CreateEntryRow(LeaderboardEntryData entry)
        {
            var rowObj = new GameObject($"Row_{entry.rank}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObj.transform.SetParent(entriesContainer, false);

            var rowRT = rowObj.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(310f, 34f);

            var rowImg = rowObj.GetComponent<Image>();
            if (uiSprite != null)
            {
                rowImg.sprite = uiSprite;
                rowImg.type = Image.Type.Sliced;
            }

            if (entry.isCurrentPlayer)
            {
                rowImg.color = new Color(0.10f, 0.38f, 0.28f, 0.95f); // Rich emerald green for current player
                var rowOutline = rowObj.AddComponent<Outline>();
                rowOutline.effectColor = new Color(0.15f, 0.95f, 0.60f, 0.85f);
                rowOutline.effectDistance = new Vector2(1f, -1f);
            }
            else
            {
                rowImg.color = (entry.rank % 2 == 0) 
                    ? new Color(0.12f, 0.16f, 0.24f, 0.85f) 
                    : new Color(0.08f, 0.11f, 0.18f, 0.85f);
            }

            // 1. Rank Text
            var rankObj = new GameObject("Rank", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            rankObj.transform.SetParent(rowObj.transform, false);
            var rankRT = rankObj.GetComponent<RectTransform>();
            rankRT.anchorMin = new Vector2(0f, 0f);
            rankRT.anchorMax = new Vector2(0.14f, 1f);
            rankRT.offsetMin = Vector2.zero;
            rankRT.offsetMax = Vector2.zero;

            var rankTxt = rankObj.GetComponent<Text>();
            rankTxt.font = runtimeFont;
            rankTxt.fontStyle = FontStyle.Bold;
            rankTxt.fontSize = 12;
            rankTxt.alignment = TextAnchor.MiddleCenter;

            if (entry.rank == 1)
            {
                rankTxt.color = new Color(1f, 0.84f, 0f, 1f); // Gold
                rankTxt.text = "★ 1";
            }
            else if (entry.rank == 2)
            {
                rankTxt.color = new Color(0.85f, 0.85f, 0.9f, 1f); // Silver
                rankTxt.text = "★ 2";
            }
            else if (entry.rank == 3)
            {
                rankTxt.color = new Color(0.85f, 0.55f, 0.35f, 1f); // Bronze
                rankTxt.text = "★ 3";
            }
            else
            {
                rankTxt.color = new Color(0.7f, 0.75f, 0.85f, 1f);
                rankTxt.text = $"#{entry.rank}";
            }

            // 2. Cat Class Icon Badge (Blaster, Brawler, Guardian)
            var catBadgeObj = new GameObject("CatBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            catBadgeObj.transform.SetParent(rowObj.transform, false);
            var badgeRT = catBadgeObj.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0.195f, 0.5f);
            badgeRT.anchorMax = new Vector2(0.195f, 0.5f);
            badgeRT.pivot = new Vector2(0.5f, 0.5f);
            badgeRT.anchoredPosition = Vector2.zero;
            badgeRT.sizeDelta = new Vector2(24f, 24f);

            var badgeImg = catBadgeObj.GetComponent<Image>();
            if (uiSprite != null)
            {
                badgeImg.sprite = uiSprite;
                badgeImg.type = Image.Type.Sliced;
            }
            badgeImg.color = GetCatBadgeColor(entry.catClassIndex);

            var catSprite = GetCatIcon(entry.catClassIndex);
            if (catSprite != null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObj.transform.SetParent(catBadgeObj.transform, false);
                var iconRT = iconObj.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.12f, 0.12f);
                iconRT.anchorMax = new Vector2(0.88f, 0.88f);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;

                var iconImg = iconObj.GetComponent<Image>();
                iconImg.sprite = catSprite;
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
            }

            // 3. Player Name Text (Single-line, strictly clamped height to prevent wrap)
            var nameObj = new GameObject("PlayerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            nameObj.transform.SetParent(rowObj.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.25f, 0.5f);
            nameRT.anchorMax = new Vector2(0.76f, 0.5f);
            nameRT.pivot = new Vector2(0f, 0.5f);
            nameRT.anchoredPosition = new Vector2(6f, 0f);
            nameRT.sizeDelta = new Vector2(-6f, 20f);

            var nameTxt = nameObj.GetComponent<Text>();
            nameTxt.font = runtimeFont;
            nameTxt.fontSize = 12;
            nameTxt.alignment = TextAnchor.MiddleLeft;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTxt.verticalOverflow = VerticalWrapMode.Truncate;
            nameTxt.resizeTextForBestFit = true;
            nameTxt.resizeTextMinSize = 9;
            nameTxt.resizeTextMaxSize = 12;

            string displayName = entry.playerName;
            // Clean up UGS discriminator suffix (e.g. tomjerrytas_FB4B#4154 -> tomjerrytas_FB4B)
            int hashIdx = displayName.IndexOf('#');
            if (hashIdx > 0)
            {
                displayName = displayName.Substring(0, hashIdx);
            }

            if (entry.isCurrentPlayer)
            {
                nameTxt.color = new Color(0.25f, 1f, 0.65f, 1f);
                nameTxt.fontStyle = FontStyle.Bold;
                nameTxt.text = $"{displayName} <color=#FFD700>(YOU)</color>";
            }
            else
            {
                nameTxt.color = Color.white;
                nameTxt.text = displayName;
            }

            // 4. Dungeon Level Text
            var scoreObj = new GameObject("Score", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            scoreObj.transform.SetParent(rowObj.transform, false);
            var scoreRT = scoreObj.GetComponent<RectTransform>();
            scoreRT.anchorMin = new Vector2(0.76f, 0f);
            scoreRT.anchorMax = new Vector2(1f, 1f);
            scoreRT.offsetMin = Vector2.zero;
            scoreRT.offsetMax = new Vector2(-8f, 0f);

            var scoreTxt = scoreObj.GetComponent<Text>();
            scoreTxt.font = runtimeFont;
            scoreTxt.fontStyle = FontStyle.Bold;
            scoreTxt.fontSize = 13;
            scoreTxt.alignment = TextAnchor.MiddleRight;
            scoreTxt.color = new Color(1f, 0.85f, 0.3f, 1f);
            scoreTxt.text = $"Lv. {entry.score}";
        }

        #endregion

        #region Dynamic UI Builder

        private void EnsureUIBuilt()
        {
            LoadBuiltinResources();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            if (canvas == null) return;

            if (nameInputPanel == null)
            {
                BuildNameInputPanel(canvas.transform);
            }

            if (leaderboardPanel == null)
            {
                BuildLeaderboardPanel(canvas.transform);
            }
        }

        private void BuildNameInputPanel(Transform root)
        {
            nameInputPanel = new GameObject("NameInputDialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nameInputPanel.transform.SetParent(root, false);

            var panelRT = nameInputPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var overlay = nameInputPanel.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.75f);
            overlay.raycastTarget = true;

            // Card Frame
            var cardObj = new GameObject("CardFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            cardObj.transform.SetParent(nameInputPanel.transform, false);

            var cardRT = cardObj.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(310f, 230f);

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.sprite = uiSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.11f, 0.14f, 0.22f, 0.98f);

            var cardOutline = cardObj.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.28f, 0.45f, 0.75f, 0.9f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -14f);
            titleRT.sizeDelta = new Vector2(280f, 26f);

            var titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = runtimeFont;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.fontSize = 18;
            titleTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "ENTER HERO NAME";

            // Description
            var descObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            descObj.transform.SetParent(cardObj.transform, false);
            var descRT = descObj.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.5f, 1f);
            descRT.anchorMax = new Vector2(0.5f, 1f);
            descRT.pivot = new Vector2(0.5f, 1f);
            descRT.anchoredPosition = new Vector2(0f, -44f);
            descRT.sizeDelta = new Vector2(270f, 32f);

            var descTxt = descObj.GetComponent<Text>();
            descTxt.font = runtimeFont;
            descTxt.fontSize = 12;
            descTxt.color = new Color(0.8f, 0.85f, 0.95f, 1f);
            descTxt.alignment = TextAnchor.MiddleCenter;
            descTxt.text = "Choose your hero name for the Leaderboard.";

            // Input Field Background
            var inputBgObj = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            inputBgObj.transform.SetParent(cardObj.transform, false);
            var inputBgRT = inputBgObj.GetComponent<RectTransform>();
            inputBgRT.anchorMin = new Vector2(0.5f, 1f);
            inputBgRT.anchorMax = new Vector2(0.5f, 1f);
            inputBgRT.pivot = new Vector2(0.5f, 1f);
            inputBgRT.anchoredPosition = new Vector2(0f, -84f);
            inputBgRT.sizeDelta = new Vector2(260f, 38f);

            var inputBgImg = inputBgObj.GetComponent<Image>();
            inputBgImg.sprite = uiSprite;
            inputBgImg.type = Image.Type.Sliced;
            inputBgImg.color = new Color(0.06f, 0.08f, 0.13f, 1f);

            nameInputField = inputBgObj.GetComponent<InputField>();
            nameInputField.characterLimit = 12;

            // Placeholder Text
            var phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            phObj.transform.SetParent(inputBgObj.transform, false);
            var phRT = phObj.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(10f, 0f);
            phRT.offsetMax = new Vector2(-10f, 0f);

            var phTxt = phObj.GetComponent<Text>();
            phTxt.font = runtimeFont;
            phTxt.fontStyle = FontStyle.Italic;
            phTxt.fontSize = 14;
            phTxt.color = new Color(0.5f, 0.55f, 0.65f, 0.8f);
            phTxt.alignment = TextAnchor.MiddleLeft;
            phTxt.text = "Enter name (2-12 letters)...";

            // Value Text
            var valObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            valObj.transform.SetParent(inputBgObj.transform, false);
            var valRT = valObj.GetComponent<RectTransform>();
            valRT.anchorMin = Vector2.zero;
            valRT.anchorMax = Vector2.one;
            valRT.offsetMin = new Vector2(10f, 0f);
            valRT.offsetMax = new Vector2(-10f, 0f);

            var valTxt = valObj.GetComponent<Text>();
            valTxt.font = runtimeFont;
            valTxt.fontSize = 15;
            valTxt.color = Color.white;
            valTxt.alignment = TextAnchor.MiddleLeft;

            nameInputField.textComponent = valTxt;
            nameInputField.placeholder = phTxt;

            // Device Suffix Label
            var suffixObj = new GameObject("DeviceSuffixLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            suffixObj.transform.SetParent(cardObj.transform, false);
            var suffixRT = suffixObj.GetComponent<RectTransform>();
            suffixRT.anchorMin = new Vector2(0.5f, 1f);
            suffixRT.anchorMax = new Vector2(0.5f, 1f);
            suffixRT.pivot = new Vector2(0.5f, 1f);
            suffixRT.anchoredPosition = new Vector2(0f, -128f);
            suffixRT.sizeDelta = new Vector2(260f, 20f);

            deviceSuffixLabel = suffixObj.GetComponent<Text>();
            deviceSuffixLabel.font = runtimeFont;
            deviceSuffixLabel.fontSize = 11;
            deviceSuffixLabel.alignment = TextAnchor.MiddleCenter;

            // Validation Text
            var valMsgObj = new GameObject("ValidationText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            valMsgObj.transform.SetParent(cardObj.transform, false);
            var valMsgRT = valMsgObj.GetComponent<RectTransform>();
            valMsgRT.anchorMin = new Vector2(0.5f, 1f);
            valMsgRT.anchorMax = new Vector2(0.5f, 1f);
            valMsgRT.pivot = new Vector2(0.5f, 1f);
            valMsgRT.anchoredPosition = new Vector2(0f, -148f);
            valMsgRT.sizeDelta = new Vector2(260f, 18f);

            nameValidationText = valMsgObj.GetComponent<Text>();
            nameValidationText.font = runtimeFont;
            nameValidationText.fontSize = 11;
            nameValidationText.alignment = TextAnchor.MiddleCenter;

            // Confirm Button
            var confirmBtnObj = new GameObject("Btn_Confirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            confirmBtnObj.transform.SetParent(cardObj.transform, false);
            var confirmRT = confirmBtnObj.GetComponent<RectTransform>();
            confirmRT.anchorMin = new Vector2(0.5f, 0f);
            confirmRT.anchorMax = new Vector2(0.5f, 0f);
            confirmRT.pivot = new Vector2(0.5f, 0f);
            confirmRT.anchoredPosition = new Vector2(0f, 14f);
            confirmRT.sizeDelta = new Vector2(220f, 38f);

            var confirmImg = confirmBtnObj.GetComponent<Image>();
            confirmImg.sprite = uiSprite;
            confirmImg.type = Image.Type.Sliced;
            confirmImg.color = new Color(0.18f, 0.65f, 0.35f, 1f);

            var confirmOutline = confirmBtnObj.GetComponent<Outline>();
            confirmOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            confirmOutline.effectDistance = new Vector2(1.5f, -1.5f);

            confirmNameButton = confirmBtnObj.GetComponent<Button>();
            confirmNameButton.onClick.AddListener(OnConfirmNameClicked);

            var btnTxtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            btnTxtObj.transform.SetParent(confirmBtnObj.transform, false);
            var btnTxtRT = btnTxtObj.GetComponent<RectTransform>();
            btnTxtRT.anchorMin = Vector2.zero;
            btnTxtRT.anchorMax = Vector2.one;
            btnTxtRT.offsetMin = Vector2.zero;
            btnTxtRT.offsetMax = Vector2.zero;

            var btnTxt = btnTxtObj.GetComponent<Text>();
            btnTxt.font = runtimeFont;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.fontSize = 15;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.text = "CONFIRM & PLAY";

            nameInputPanel.SetActive(false);
        }

        private void BuildLeaderboardPanel(Transform root)
        {
            leaderboardPanel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            leaderboardPanel.transform.SetParent(root, false);

            var panelRT = leaderboardPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var overlay = leaderboardPanel.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.8f);
            overlay.raycastTarget = true;

            // Card Frame
            var cardObj = new GameObject("CardFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            cardObj.transform.SetParent(leaderboardPanel.transform, false);

            var cardRT = cardObj.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(340f, 530f);

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.sprite = uiSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.10f, 0.13f, 0.20f, 0.98f);

            var cardOutline = cardObj.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.28f, 0.45f, 0.75f, 0.9f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -16f);
            titleRT.sizeDelta = new Vector2(240f, 28f);

            var titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = runtimeFont;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.fontSize = 20;
            titleTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "LEADERBOARD";

            // Close 'X' Button
            var closeXObj = new GameObject("Btn_CloseX", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeXObj.transform.SetParent(cardObj.transform, false);
            var closeXRT = closeXObj.GetComponent<RectTransform>();
            closeXRT.anchorMin = new Vector2(1f, 1f);
            closeXRT.anchorMax = new Vector2(1f, 1f);
            closeXRT.pivot = new Vector2(1f, 1f);
            closeXRT.anchoredPosition = new Vector2(-12f, -12f);
            closeXRT.sizeDelta = new Vector2(30f, 30f);

            var closeXImg = closeXObj.GetComponent<Image>();
            closeXImg.sprite = uiSprite;
            closeXImg.type = Image.Type.Sliced;
            closeXImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);

            closeButton = closeXObj.GetComponent<Button>();
            closeButton.onClick.AddListener(CloseLeaderboard);

            var closeXTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            closeXTxtObj.transform.SetParent(closeXObj.transform, false);
            var closeXTxtRT = closeXTxtObj.GetComponent<RectTransform>();
            closeXTxtRT.anchorMin = Vector2.zero;
            closeXTxtRT.anchorMax = Vector2.one;
            closeXTxtRT.offsetMin = Vector2.zero;
            closeXTxtRT.offsetMax = Vector2.zero;

            var closeXTxt = closeXTxtObj.GetComponent<Text>();
            closeXTxt.font = runtimeFont;
            closeXTxt.fontStyle = FontStyle.Bold;
            closeXTxt.fontSize = 18;
            closeXTxt.color = Color.white;
            closeXTxt.alignment = TextAnchor.MiddleCenter;
            closeXTxt.text = "X";

            // Player Info Bar
            var pInfoObj = new GameObject("PlayerInfo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            pInfoObj.transform.SetParent(cardObj.transform, false);
            var pInfoRT = pInfoObj.GetComponent<RectTransform>();
            pInfoRT.anchorMin = new Vector2(0.5f, 1f);
            pInfoRT.anchorMax = new Vector2(0.5f, 1f);
            pInfoRT.pivot = new Vector2(0.5f, 1f);
            pInfoRT.anchoredPosition = new Vector2(-22f, -48f);
            pInfoRT.sizeDelta = new Vector2(230f, 20f);

            playerInfoText = pInfoObj.GetComponent<Text>();
            playerInfoText.font = runtimeFont;
            playerInfoText.fontSize = 12;
            playerInfoText.color = Color.white;
            playerInfoText.alignment = TextAnchor.MiddleCenter;

            // Edit Name Button
            var editNameBtnObj = new GameObject("Btn_EditName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            editNameBtnObj.transform.SetParent(cardObj.transform, false);
            var editRT = editNameBtnObj.GetComponent<RectTransform>();
            editRT.anchorMin = new Vector2(0.5f, 1f);
            editRT.anchorMax = new Vector2(0.5f, 1f);
            editRT.pivot = new Vector2(0.5f, 1f);
            editRT.anchoredPosition = new Vector2(115f, -48f);
            editRT.sizeDelta = new Vector2(44f, 20f);

            var editImg = editNameBtnObj.GetComponent<Image>();
            editImg.sprite = uiSprite;
            editImg.type = Image.Type.Sliced;
            editImg.color = new Color(0.2f, 0.35f, 0.55f, 0.9f);

            var editBtn = editNameBtnObj.GetComponent<Button>();
            editBtn.onClick.AddListener(ShowNameInputDialog);

            var editTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            editTxtObj.transform.SetParent(editNameBtnObj.transform, false);
            var editTxtRT = editTxtObj.GetComponent<RectTransform>();
            editTxtRT.anchorMin = Vector2.zero;
            editTxtRT.anchorMax = Vector2.one;
            editTxtRT.offsetMin = Vector2.zero;
            editTxtRT.offsetMax = Vector2.zero;

            var editTxt = editTxtObj.GetComponent<Text>();
            editTxt.font = runtimeFont;
            editTxt.fontSize = 10;
            editTxt.color = Color.white;
            editTxt.alignment = TextAnchor.MiddleCenter;
            editTxt.text = "EDIT";

            // Tab Buttons Row
            var tabsRow = new GameObject("TabsRow", typeof(RectTransform));
            tabsRow.transform.SetParent(cardObj.transform, false);
            var tabsRT = tabsRow.GetComponent<RectTransform>();
            tabsRT.anchorMin = new Vector2(0.5f, 1f);
            tabsRT.anchorMax = new Vector2(0.5f, 1f);
            tabsRT.pivot = new Vector2(0.5f, 1f);
            tabsRT.anchoredPosition = new Vector2(0f, -74f);
            tabsRT.sizeDelta = new Vector2(310f, 32f);

            // Tab: All-Time
            var tabAllObj = new GameObject("Tab_AllTime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tabAllObj.transform.SetParent(tabsRow.transform, false);
            var tabAllRT = tabAllObj.GetComponent<RectTransform>();
            tabAllRT.anchorMin = new Vector2(0f, 0f);
            tabAllRT.anchorMax = new Vector2(0.49f, 1f);
            tabAllRT.offsetMin = Vector2.zero;
            tabAllRT.offsetMax = Vector2.zero;

            tabAllTimeIndicator = tabAllObj.GetComponent<Image>();
            tabAllTimeIndicator.sprite = uiSprite;
            tabAllTimeIndicator.type = Image.Type.Sliced;
            tabAllTimeIndicator.color = new Color(0.18f, 0.65f, 0.95f, 1f);

            tabAllTimeButton = tabAllObj.GetComponent<Button>();
            tabAllTimeButton.onClick.AddListener(SwitchTabAllTime);

            var tabAllTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            tabAllTxtObj.transform.SetParent(tabAllObj.transform, false);
            var tabAllTxtRT = tabAllTxtObj.GetComponent<RectTransform>();
            tabAllTxtRT.anchorMin = Vector2.zero;
            tabAllTxtRT.anchorMax = Vector2.one;
            tabAllTxtRT.offsetMin = Vector2.zero;
            tabAllTxtRT.offsetMax = Vector2.zero;

            var tabAllTxt = tabAllTxtObj.GetComponent<Text>();
            tabAllTxt.font = runtimeFont;
            tabAllTxt.fontStyle = FontStyle.Bold;
            tabAllTxt.fontSize = 13;
            tabAllTxt.color = Color.white;
            tabAllTxt.alignment = TextAnchor.MiddleCenter;
            tabAllTxt.text = "ALL-TIME";

            // Tab: Weekly
            var tabWeekObj = new GameObject("Tab_Weekly", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tabWeekObj.transform.SetParent(tabsRow.transform, false);
            var tabWeekRT = tabWeekObj.GetComponent<RectTransform>();
            tabWeekRT.anchorMin = new Vector2(0.51f, 0f);
            tabWeekRT.anchorMax = new Vector2(1f, 1f);
            tabWeekRT.offsetMin = Vector2.zero;
            tabWeekRT.offsetMax = Vector2.zero;

            tabWeeklyIndicator = tabWeekObj.GetComponent<Image>();
            tabWeeklyIndicator.sprite = uiSprite;
            tabWeeklyIndicator.type = Image.Type.Sliced;
            tabWeeklyIndicator.color = new Color(0.2f, 0.25f, 0.35f, 0.8f);

            tabWeeklyButton = tabWeekObj.GetComponent<Button>();
            tabWeeklyButton.onClick.AddListener(SwitchTabWeekly);

            var tabWeekTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            tabWeekTxtObj.transform.SetParent(tabWeekObj.transform, false);
            var tabWeekTxtRT = tabWeekTxtObj.GetComponent<RectTransform>();
            tabWeekTxtRT.anchorMin = Vector2.zero;
            tabWeekTxtRT.anchorMax = Vector2.one;
            tabWeekTxtRT.offsetMin = Vector2.zero;
            tabWeekTxtRT.offsetMax = Vector2.zero;

            var tabWeekTxt = tabWeekTxtObj.GetComponent<Text>();
            tabWeekTxt.font = runtimeFont;
            tabWeekTxt.fontStyle = FontStyle.Bold;
            tabWeekTxt.fontSize = 13;
            tabWeekTxt.color = Color.white;
            tabWeekTxt.alignment = TextAnchor.MiddleCenter;
            tabWeekTxt.text = "WEEKLY";

            // Table Header Row
            var thObj = new GameObject("TableHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            thObj.transform.SetParent(cardObj.transform, false);
            var thRT = thObj.GetComponent<RectTransform>();
            thRT.anchorMin = new Vector2(0.5f, 1f);
            thRT.anchorMax = new Vector2(0.5f, 1f);
            thRT.pivot = new Vector2(0.5f, 1f);
            thRT.anchoredPosition = new Vector2(0f, -114f);
            thRT.sizeDelta = new Vector2(314f, 24f);

            var thImg = thObj.GetComponent<Image>();
            thImg.color = new Color(0.08f, 0.11f, 0.17f, 0.9f);

            CreateHeaderLabel(thObj.transform, 0f, 0.14f, "RANK", TextAnchor.MiddleCenter);
            CreateHeaderLabel(thObj.transform, 0.14f, 0.25f, "CAT", TextAnchor.MiddleCenter);
            CreateHeaderLabel(thObj.transform, 0.25f, 0.76f, "PLAYER", TextAnchor.MiddleLeft);
            CreateHeaderLabel(thObj.transform, 0.76f, 1f, "DUNGEON", TextAnchor.MiddleRight);

            // Scroll View Area (Top-anchored below TableHeader to completely prevent overlap)
            var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect));
            scrollObj.transform.SetParent(cardObj.transform, false);
            var scrollRT = scrollObj.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0.5f, 1f);
            scrollRT.anchorMax = new Vector2(0.5f, 1f);
            scrollRT.pivot = new Vector2(0.5f, 1f);
            scrollRT.anchoredPosition = new Vector2(0f, -140f);
            scrollRT.sizeDelta = new Vector2(314f, 326f);

            var scrollRect = scrollObj.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
            vpObj.transform.SetParent(scrollObj.transform, false);
            var vpRT = vpObj.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;

            // Content
            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(vpObj.transform, false);
            var contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 3f;
            vlg.padding = new RectOffset(2, 2, 2, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRT;
            entriesContainer = contentObj.transform;

            // Status Text (Inside Viewport)
            var stObj = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            stObj.transform.SetParent(vpObj.transform, false);
            var stRT = stObj.GetComponent<RectTransform>();
            stRT.anchorMin = Vector2.zero;
            stRT.anchorMax = Vector2.one;
            stRT.offsetMin = new Vector2(20f, 20f);
            stRT.offsetMax = new Vector2(-20f, -20f);

            statusText = stObj.GetComponent<Text>();
            statusText.font = runtimeFont;
            statusText.fontSize = 13;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(0.75f, 0.8f, 0.9f, 0.9f);
            statusText.text = "Loading...";

            // Footer Buttons: Refresh & Back
            var refreshBtnObj = new GameObject("Btn_Refresh", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            refreshBtnObj.transform.SetParent(cardObj.transform, false);
            var refRT = refreshBtnObj.GetComponent<RectTransform>();
            refRT.anchorMin = new Vector2(0.5f, 0f);
            refRT.anchorMax = new Vector2(0.5f, 0f);
            refRT.pivot = new Vector2(0.5f, 0f);
            refRT.anchoredPosition = new Vector2(-70f, 14f);
            refRT.sizeDelta = new Vector2(124f, 36f);

            var refImg = refreshBtnObj.GetComponent<Image>();
            refImg.sprite = uiSprite;
            refImg.type = Image.Type.Sliced;
            refImg.color = new Color(0.2f, 0.45f, 0.75f, 1f);

            refreshButton = refreshBtnObj.GetComponent<Button>();
            refreshButton.onClick.AddListener(OnRefreshClicked);

            var refTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            refTxtObj.transform.SetParent(refreshBtnObj.transform, false);
            var refTxtRT = refTxtObj.GetComponent<RectTransform>();
            refTxtRT.anchorMin = Vector2.zero;
            refTxtRT.anchorMax = Vector2.one;
            refTxtRT.offsetMin = Vector2.zero;
            refTxtRT.offsetMax = Vector2.zero;

            var refTxt = refTxtObj.GetComponent<Text>();
            refTxt.font = runtimeFont;
            refTxt.fontStyle = FontStyle.Bold;
            refTxt.fontSize = 13;
            refTxt.color = Color.white;
            refTxt.alignment = TextAnchor.MiddleCenter;
            refTxt.text = "REFRESH";

            var backBtnObj = new GameObject("Btn_Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backBtnObj.transform.SetParent(cardObj.transform, false);
            var backRT = backBtnObj.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0f);
            backRT.anchorMax = new Vector2(0.5f, 0f);
            backRT.pivot = new Vector2(0.5f, 0f);
            backRT.anchoredPosition = new Vector2(70f, 14f);
            backRT.sizeDelta = new Vector2(124f, 36f);

            var backImg = backBtnObj.GetComponent<Image>();
            backImg.sprite = uiSprite;
            backImg.type = Image.Type.Sliced;
            backImg.color = new Color(0.4f, 0.45f, 0.55f, 1f);

            var backBtn = backBtnObj.GetComponent<Button>();
            backBtn.onClick.AddListener(CloseLeaderboard);

            var backTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            backTxtObj.transform.SetParent(backBtnObj.transform, false);
            var backTxtRT = backTxtObj.GetComponent<RectTransform>();
            backTxtRT.anchorMin = Vector2.zero;
            backTxtRT.anchorMax = Vector2.one;
            backTxtRT.offsetMin = Vector2.zero;
            backTxtRT.offsetMax = Vector2.zero;

            var backTxt = backTxtObj.GetComponent<Text>();
            backTxt.font = runtimeFont;
            backTxt.fontStyle = FontStyle.Bold;
            backTxt.fontSize = 13;
            backTxt.color = Color.white;
            backTxt.alignment = TextAnchor.MiddleCenter;
            backTxt.text = "BACK";

            leaderboardPanel.SetActive(false);
        }

        private void CreateHeaderLabel(Transform parent, float minX, float maxX, string text, TextAnchor alignment)
        {
            var lblObj = new GameObject($"H_{text}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            lblObj.transform.SetParent(parent, false);
            var lblRT = lblObj.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(minX, 0f);
            lblRT.anchorMax = new Vector2(maxX, 1f);
            lblRT.offsetMin = (alignment == TextAnchor.MiddleLeft) ? new Vector2(6f, 0f) : Vector2.zero;
            lblRT.offsetMax = (alignment == TextAnchor.MiddleRight) ? new Vector2(-8f, 0f) : Vector2.zero;

            var lblTxt = lblObj.GetComponent<Text>();
            lblTxt.font = runtimeFont;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.fontSize = 11;
            lblTxt.color = new Color(0.6f, 0.7f, 0.85f, 0.8f);
            lblTxt.alignment = alignment;
            lblTxt.text = text;
        }

        #endregion
    }
}

