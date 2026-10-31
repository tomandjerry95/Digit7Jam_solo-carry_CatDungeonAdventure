using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Digit7s.EditorScripts
{
    public static class LevelPauseMenuSetup
    {
        [MenuItem("Digit7s/Setup Level Pause Menu")]
        public static void Setup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "level")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/level.unity", OpenSceneMode.Single);
            }

            var canvasObj = GameObject.Find("/Canvas_GameplayUI");
            if (canvasObj == null)
            {
                Debug.LogError("[LevelPauseMenuSetup] Canvas_GameplayUI not found in scene!");
                return;
            }

            var uiController = canvasObj.GetComponent<Digit7s.UI.GameplayUIController>();
            if (uiController == null)
            {
                Debug.LogError("[LevelPauseMenuSetup] GameplayUIController not found!");
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Icon_PictoIcons/128/Pictoicon_Control_Pause.Png");

            // 1. Create or Find PauseButton (Top-Right)
            Transform pauseBtnTrans = canvasObj.transform.Find("PauseButton");
            GameObject pauseBtnObj;
            if (pauseBtnTrans == null)
            {
                pauseBtnObj = new GameObject("PauseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                pauseBtnObj.transform.SetParent(canvasObj.transform, false);
            }
            else
            {
                pauseBtnObj = pauseBtnTrans.gameObject;
            }

            var pauseBtnRT = pauseBtnObj.GetComponent<RectTransform>();
            pauseBtnRT.anchorMin = new Vector2(1f, 1f);
            pauseBtnRT.anchorMax = new Vector2(1f, 1f);
            pauseBtnRT.pivot = new Vector2(1f, 1f);
            pauseBtnRT.anchoredPosition = new Vector2(-12f, -12f);
            pauseBtnRT.sizeDelta = new Vector2(38f, 38f);

            var pauseBtnImg = pauseBtnObj.GetComponent<Image>();
            pauseBtnImg.sprite = uiSprite;
            pauseBtnImg.type = Image.Type.Sliced;
            pauseBtnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            pauseBtnImg.raycastTarget = true;

            var pauseBtn = pauseBtnObj.GetComponent<Button>();
            pauseBtn.targetGraphic = pauseBtnImg;
            var pbColors = pauseBtn.colors;
            pbColors.normalColor = Color.white;
            pbColors.highlightedColor = new Color(0.85f, 0.92f, 1f, 1f);
            pbColors.pressedColor = new Color(0.65f, 0.75f, 0.9f, 1f);
            pauseBtn.colors = pbColors;

            var pauseOutline = pauseBtnObj.GetComponent<Outline>();
            pauseOutline.effectColor = new Color(0.25f, 0.55f, 0.9f, 0.8f);
            pauseOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Icon / Text on Pause Button
            Transform iconTrans = pauseBtnObj.transform.Find("Icon");
            GameObject iconObj;
            if (iconTrans == null)
            {
                iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer));
                iconObj.transform.SetParent(pauseBtnObj.transform, false);
            }
            else
            {
                iconObj = iconTrans.gameObject;
            }

            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;

            if (pauseIcon != null)
            {
                var iconImg = iconObj.GetComponent<Image>();
                if (iconImg == null) iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = pauseIcon;
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
                iconRT.offsetMin = new Vector2(8f, 8f);
                iconRT.offsetMax = new Vector2(-8f, -8f);
            }
            else
            {
                var iconTxt = iconObj.GetComponent<Text>();
                if (iconTxt == null) iconTxt = iconObj.AddComponent<Text>();
                iconTxt.font = font;
                iconTxt.fontStyle = FontStyle.Bold;
                iconTxt.fontSize = 17;
                iconTxt.color = Color.white;
                iconTxt.alignment = TextAnchor.MiddleCenter;
                iconTxt.text = "❚❚";
                iconTxt.raycastTarget = false;
            }

            // 2. Create or Reset PauseMenuPanel
            Transform oldPausePanel = canvasObj.transform.Find("PauseMenuPanel");
            if (oldPausePanel != null) GameObject.DestroyImmediate(oldPausePanel.gameObject);

            var pauseMenuPanelObj = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pauseMenuPanelObj.transform.SetParent(canvasObj.transform, false);
            var pausePanelRT = pauseMenuPanelObj.GetComponent<RectTransform>();
            pausePanelRT.anchorMin = Vector2.zero;
            pausePanelRT.anchorMax = Vector2.one;
            pausePanelRT.offsetMin = Vector2.zero;
            pausePanelRT.offsetMax = Vector2.zero;
            pausePanelRT.pivot = new Vector2(0.5f, 0.5f);

            var pausePanelImg = pauseMenuPanelObj.GetComponent<Image>();
            pausePanelImg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);
            pausePanelImg.raycastTarget = true;

            // Pause Dialog Frame
            var pWindowObj = new GameObject("WindowFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            pWindowObj.transform.SetParent(pauseMenuPanelObj.transform, false);
            var pWindowRT = pWindowObj.GetComponent<RectTransform>();
            pWindowRT.anchorMin = new Vector2(0.5f, 0.5f);
            pWindowRT.anchorMax = new Vector2(0.5f, 0.5f);
            pWindowRT.pivot = new Vector2(0.5f, 0.5f);
            pWindowRT.anchoredPosition = Vector2.zero;
            pWindowRT.sizeDelta = new Vector2(280f, 260f);

            var pWindowImg = pWindowObj.GetComponent<Image>();
            pWindowImg.sprite = uiSprite;
            pWindowImg.type = Image.Type.Sliced;
            pWindowImg.color = new Color(0.11f, 0.14f, 0.22f, 0.98f);
            pWindowImg.raycastTarget = true;

            var pWindowOutline = pWindowObj.GetComponent<Outline>();
            pWindowOutline.effectColor = new Color(0.28f, 0.42f, 0.62f, 0.9f);
            pWindowOutline.effectDistance = new Vector2(2f, -2f);

            // Pause Title
            var pTitleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            pTitleObj.transform.SetParent(pWindowObj.transform, false);
            var pTitleRT = pTitleObj.GetComponent<RectTransform>();
            pTitleRT.anchorMin = new Vector2(0.5f, 1f);
            pTitleRT.anchorMax = new Vector2(0.5f, 1f);
            pTitleRT.pivot = new Vector2(0.5f, 1f);
            pTitleRT.anchoredPosition = new Vector2(0f, -16f);
            pTitleRT.sizeDelta = new Vector2(220f, 32f);

            var pTitleTxt = pTitleObj.GetComponent<Text>();
            pTitleTxt.font = font;
            pTitleTxt.fontStyle = FontStyle.Bold;
            pTitleTxt.fontSize = 22;
            pTitleTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            pTitleTxt.alignment = TextAnchor.MiddleCenter;
            pTitleTxt.text = "PAUSED";

            var pTitleShadow = pTitleObj.GetComponent<Shadow>();
            pTitleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            pTitleShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Divider
            var pDivObj = new GameObject("HeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pDivObj.transform.SetParent(pWindowObj.transform, false);
            var pDivRT = pDivObj.GetComponent<RectTransform>();
            pDivRT.anchorMin = new Vector2(0.5f, 1f);
            pDivRT.anchorMax = new Vector2(0.5f, 1f);
            pDivRT.pivot = new Vector2(0.5f, 1f);
            pDivRT.anchoredPosition = new Vector2(0f, -54f);
            pDivRT.sizeDelta = new Vector2(240f, 2f);
            var pDivImg = pDivObj.GetComponent<Image>();
            pDivImg.color = new Color(0.28f, 0.38f, 0.52f, 0.6f);

            // Button Column
            var pBtnColObj = new GameObject("ButtonColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            pBtnColObj.transform.SetParent(pWindowObj.transform, false);
            var pBtnColRT = pBtnColObj.GetComponent<RectTransform>();
            pBtnColRT.anchorMin = new Vector2(0.5f, 0.5f);
            pBtnColRT.anchorMax = new Vector2(0.5f, 0.5f);
            pBtnColRT.pivot = new Vector2(0.5f, 0.5f);
            pBtnColRT.anchoredPosition = new Vector2(0f, -22f);
            pBtnColRT.sizeDelta = new Vector2(240f, 156f);

            var vGroup = pBtnColObj.GetComponent<VerticalLayoutGroup>();
            vGroup.spacing = 10f;
            vGroup.childAlignment = TextAnchor.MiddleCenter;
            vGroup.childControlWidth = false;
            vGroup.childControlHeight = false;

            // 1) Resume Button
            var resumeBtn = CreateButton(pBtnColObj.transform, "Btn_Resume", "▶  RESUME", font, 16, new Color(0.16f, 0.72f, 0.38f, 1f), uiSprite, new Vector2(240f, 44f));

            // 2) Setting Button
            var pauseSettingBtn = CreateButton(pBtnColObj.transform, "Btn_Setting", "⚙  SETTING", font, 16, new Color(0.18f, 0.52f, 0.82f, 1f), uiSprite, new Vector2(240f, 44f));

            // 3) Reset Character Button
            var resetCharBtn = CreateButton(pBtnColObj.transform, "Btn_ResetCharacter", "↺  RESET CHARACTER", font, 15, new Color(0.85f, 0.32f, 0.25f, 1f), uiSprite, new Vector2(240f, 44f));

            pauseMenuPanelObj.SetActive(false);

            // 3. Create or Reset WarningConfirmPanel
            Transform oldWarnPanel = canvasObj.transform.Find("WarningConfirmPanel");
            if (oldWarnPanel != null) GameObject.DestroyImmediate(oldWarnPanel.gameObject);

            var warnPanelObj = new GameObject("WarningConfirmPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            warnPanelObj.transform.SetParent(canvasObj.transform, false);
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
            wWindowImg.sprite = uiSprite;
            wWindowImg.type = Image.Type.Sliced;
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
            wTitleTxt.font = font;
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
            wMsgTxt.font = font;
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

            var warnNoBtn = CreateButton(wBtnRowObj.transform, "Btn_WarningNo", "NO", font, 15, new Color(0.24f, 0.32f, 0.45f, 1f), uiSprite, new Vector2(115f, 40f));
            var warnYesBtn = CreateButton(wBtnRowObj.transform, "Btn_WarningYes", "YES", font, 15, new Color(0.85f, 0.30f, 0.25f, 1f), uiSprite, new Vector2(115f, 40f));

            warnPanelObj.SetActive(false);

            // 4. Create or Reset PauseSettingsPanel (Identical to MainMenu settings)
            Transform oldSettingsPanel = canvasObj.transform.Find("PauseSettingsPanel");
            if (oldSettingsPanel != null) GameObject.DestroyImmediate(oldSettingsPanel.gameObject);

            var settingsPanelObj = new GameObject("PauseSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            settingsPanelObj.transform.SetParent(canvasObj.transform, false);
            var sPanelRT = settingsPanelObj.GetComponent<RectTransform>();
            sPanelRT.anchorMin = Vector2.zero;
            sPanelRT.anchorMax = Vector2.one;
            sPanelRT.offsetMin = Vector2.zero;
            sPanelRT.offsetMax = Vector2.zero;

            var sPanelImg = settingsPanelObj.GetComponent<Image>();
            sPanelImg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);
            sPanelImg.raycastTarget = true;

            var sWindowObj = new GameObject("WindowFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            sWindowObj.transform.SetParent(settingsPanelObj.transform, false);
            var sWindowRT = sWindowObj.GetComponent<RectTransform>();
            sWindowRT.anchorMin = new Vector2(0.5f, 0.5f);
            sWindowRT.anchorMax = new Vector2(0.5f, 0.5f);
            sWindowRT.pivot = new Vector2(0.5f, 0.5f);
            sWindowRT.anchoredPosition = Vector2.zero;
            sWindowRT.sizeDelta = new Vector2(310f, 315f);

            var sWindowImg = sWindowObj.GetComponent<Image>();
            sWindowImg.sprite = uiSprite;
            sWindowImg.type = Image.Type.Sliced;
            sWindowImg.color = new Color(0.11f, 0.14f, 0.22f, 0.98f);
            sWindowImg.raycastTarget = true;

            var sWindowOutline = sWindowObj.GetComponent<Outline>();
            sWindowOutline.effectColor = new Color(0.28f, 0.42f, 0.62f, 0.9f);
            sWindowOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            var sTitleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            sTitleObj.transform.SetParent(sWindowObj.transform, false);
            var sTitleRT = sTitleObj.GetComponent<RectTransform>();
            sTitleRT.anchorMin = new Vector2(0.5f, 1f);
            sTitleRT.anchorMax = new Vector2(0.5f, 1f);
            sTitleRT.pivot = new Vector2(0.5f, 1f);
            sTitleRT.anchoredPosition = new Vector2(-15f, -14f);
            sTitleRT.sizeDelta = new Vector2(220f, 32f);

            var sTitleTxt = sTitleObj.GetComponent<Text>();
            sTitleTxt.font = font;
            sTitleTxt.fontStyle = FontStyle.Bold;
            sTitleTxt.fontSize = 22;
            sTitleTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            sTitleTxt.alignment = TextAnchor.MiddleCenter;
            sTitleTxt.text = "SETTINGS";

            var sTitleShadow = sTitleObj.GetComponent<Shadow>();
            sTitleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            sTitleShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Close 'X' Button
            var closeXBtn = CreateButton(sWindowObj.transform, "Btn_CloseX", "X", font, 20, new Color(0.85f, 0.25f, 0.25f, 1f), uiSprite, new Vector2(36f, 36f));
            var closeXRT = closeXBtn.GetComponent<RectTransform>();
            closeXRT.anchorMin = new Vector2(1f, 1f);
            closeXRT.anchorMax = new Vector2(1f, 1f);
            closeXRT.pivot = new Vector2(1f, 1f);
            closeXRT.anchoredPosition = new Vector2(-10f, -10f);

            // Header Divider
            var sHeaderDiv = new GameObject("HeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sHeaderDiv.transform.SetParent(sWindowObj.transform, false);
            var sHeaderDivRT = sHeaderDiv.GetComponent<RectTransform>();
            sHeaderDivRT.anchorMin = new Vector2(0.5f, 1f);
            sHeaderDivRT.anchorMax = new Vector2(0.5f, 1f);
            sHeaderDivRT.pivot = new Vector2(0.5f, 1f);
            sHeaderDivRT.anchoredPosition = new Vector2(0f, -54f);
            sHeaderDivRT.sizeDelta = new Vector2(280f, 2f);
            sHeaderDiv.GetComponent<Image>().color = new Color(0.28f, 0.38f, 0.52f, 0.6f);

            // BGM Group
            var bgmGroupObj = new GameObject("BgmGroup", typeof(RectTransform));
            bgmGroupObj.transform.SetParent(sWindowObj.transform, false);
            var bgmGroupRT = bgmGroupObj.GetComponent<RectTransform>();
            bgmGroupRT.anchorMin = new Vector2(0.5f, 1f);
            bgmGroupRT.anchorMax = new Vector2(0.5f, 1f);
            bgmGroupRT.pivot = new Vector2(0.5f, 1f);
            bgmGroupRT.anchoredPosition = new Vector2(0f, -66f);
            bgmGroupRT.sizeDelta = new Vector2(280f, 100f);

            var bgmTitleObj = new GameObject("BgmTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            bgmTitleObj.transform.SetParent(bgmGroupObj.transform, false);
            var bgmTitleRT = bgmTitleObj.GetComponent<RectTransform>();
            bgmTitleRT.anchorMin = new Vector2(0f, 1f);
            bgmTitleRT.anchorMax = new Vector2(0f, 1f);
            bgmTitleRT.pivot = new Vector2(0f, 1f);
            bgmTitleRT.anchoredPosition = new Vector2(2f, 0f);
            bgmTitleRT.sizeDelta = new Vector2(190f, 22f);
            var bgmTitleTxt = bgmTitleObj.GetComponent<Text>();
            bgmTitleTxt.font = font;
            bgmTitleTxt.fontStyle = FontStyle.Bold;
            bgmTitleTxt.fontSize = 13;
            bgmTitleTxt.color = new Color(0.85f, 0.92f, 1f, 1f);
            bgmTitleTxt.alignment = TextAnchor.MiddleLeft;
            bgmTitleTxt.text = "BACKGROUND MUSIC";

            var bgmValObj = new GameObject("BgmValueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            bgmValObj.transform.SetParent(bgmGroupObj.transform, false);
            var bgmValRT = bgmValObj.GetComponent<RectTransform>();
            bgmValRT.anchorMin = new Vector2(1f, 1f);
            bgmValRT.anchorMax = new Vector2(1f, 1f);
            bgmValRT.pivot = new Vector2(1f, 1f);
            bgmValRT.anchoredPosition = new Vector2(-2f, 0f);
            bgmValRT.sizeDelta = new Vector2(80f, 22f);
            var bgmValTxt = bgmValObj.GetComponent<Text>();
            bgmValTxt.font = font;
            bgmValTxt.fontStyle = FontStyle.Bold;
            bgmValTxt.fontSize = 13;
            bgmValTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            bgmValTxt.alignment = TextAnchor.MiddleRight;
            bgmValTxt.text = "80%";

            var bgmSlider = CreateSlider(bgmGroupObj.transform, "BgmSlider", bgSprite, uiSprite, knobSprite, new Color(0.20f, 0.75f, 0.40f, 1f));
            var bgmSliderRT = bgmSlider.GetComponent<RectTransform>();
            bgmSliderRT.anchorMin = new Vector2(0.5f, 1f);
            bgmSliderRT.anchorMax = new Vector2(0.5f, 1f);
            bgmSliderRT.pivot = new Vector2(0.5f, 1f);
            bgmSliderRT.anchoredPosition = new Vector2(0f, -28f);
            bgmSliderRT.sizeDelta = new Vector2(276f, 24f);

            var bgmMuteBtn = CreateButton(bgmGroupObj.transform, "Btn_BgmMute", "MUTE BGM", font, 13, new Color(0.20f, 0.26f, 0.38f, 1f), uiSprite, new Vector2(276f, 32f));
            var bgmMuteRT = bgmMuteBtn.GetComponent<RectTransform>();
            bgmMuteRT.anchorMin = new Vector2(0.5f, 1f);
            bgmMuteRT.anchorMax = new Vector2(0.5f, 1f);
            bgmMuteRT.pivot = new Vector2(0.5f, 1f);
            bgmMuteRT.anchoredPosition = new Vector2(0f, -62f);

            // Mid Divider
            var sMidDiv = new GameObject("MidDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sMidDiv.transform.SetParent(sWindowObj.transform, false);
            var sMidDivRT = sMidDiv.GetComponent<RectTransform>();
            sMidDivRT.anchorMin = new Vector2(0.5f, 1f);
            sMidDivRT.anchorMax = new Vector2(0.5f, 1f);
            sMidDivRT.pivot = new Vector2(0.5f, 1f);
            sMidDivRT.anchoredPosition = new Vector2(0f, -178f);
            sMidDivRT.sizeDelta = new Vector2(280f, 2f);
            sMidDiv.GetComponent<Image>().color = new Color(0.28f, 0.38f, 0.52f, 0.6f);

            // SFX Group
            var sfxGroupObj = new GameObject("SfxGroup", typeof(RectTransform));
            sfxGroupObj.transform.SetParent(sWindowObj.transform, false);
            var sfxGroupRT = sfxGroupObj.GetComponent<RectTransform>();
            sfxGroupRT.anchorMin = new Vector2(0.5f, 1f);
            sfxGroupRT.anchorMax = new Vector2(0.5f, 1f);
            sfxGroupRT.pivot = new Vector2(0.5f, 1f);
            sfxGroupRT.anchoredPosition = new Vector2(0f, -190f);
            sfxGroupRT.sizeDelta = new Vector2(280f, 100f);

            var sfxTitleObj = new GameObject("SfxTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            sfxTitleObj.transform.SetParent(sfxGroupObj.transform, false);
            var sfxTitleRT = sfxTitleObj.GetComponent<RectTransform>();
            sfxTitleRT.anchorMin = new Vector2(0f, 1f);
            sfxTitleRT.anchorMax = new Vector2(0f, 1f);
            sfxTitleRT.pivot = new Vector2(0f, 1f);
            sfxTitleRT.anchoredPosition = new Vector2(2f, 0f);
            sfxTitleRT.sizeDelta = new Vector2(190f, 22f);
            var sfxTitleTxt = sfxTitleObj.GetComponent<Text>();
            sfxTitleTxt.font = font;
            sfxTitleTxt.fontStyle = FontStyle.Bold;
            sfxTitleTxt.fontSize = 13;
            sfxTitleTxt.color = new Color(0.85f, 0.92f, 1f, 1f);
            sfxTitleTxt.alignment = TextAnchor.MiddleLeft;
            sfxTitleTxt.text = "SOUND EFFECTS (SFX)";

            var sfxValObj = new GameObject("SfxValueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            sfxValObj.transform.SetParent(sfxGroupObj.transform, false);
            var sfxValRT = sfxValObj.GetComponent<RectTransform>();
            sfxValRT.anchorMin = new Vector2(1f, 1f);
            sfxValRT.anchorMax = new Vector2(1f, 1f);
            sfxValRT.pivot = new Vector2(1f, 1f);
            sfxValRT.anchoredPosition = new Vector2(-2f, 0f);
            sfxValRT.sizeDelta = new Vector2(80f, 22f);
            var sfxValTxt = sfxValObj.GetComponent<Text>();
            sfxValTxt.font = font;
            sfxValTxt.fontStyle = FontStyle.Bold;
            sfxValTxt.fontSize = 13;
            sfxValTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            sfxValTxt.alignment = TextAnchor.MiddleRight;
            sfxValTxt.text = "90%";

            var sfxSlider = CreateSlider(sfxGroupObj.transform, "SfxSlider", bgSprite, uiSprite, knobSprite, new Color(0.95f, 0.70f, 0.20f, 1f));
            var sfxSliderRT = sfxSlider.GetComponent<RectTransform>();
            sfxSliderRT.anchorMin = new Vector2(0.5f, 1f);
            sfxSliderRT.anchorMax = new Vector2(0.5f, 1f);
            sfxSliderRT.pivot = new Vector2(0.5f, 1f);
            sfxSliderRT.anchoredPosition = new Vector2(0f, -28f);
            sfxSliderRT.sizeDelta = new Vector2(276f, 24f);

            var sfxMuteBtn = CreateButton(sfxGroupObj.transform, "Btn_SfxMute", "MUTE SFX", font, 13, new Color(0.20f, 0.26f, 0.38f, 1f), uiSprite, new Vector2(276f, 32f));
            var sfxMuteRT = sfxMuteBtn.GetComponent<RectTransform>();
            sfxMuteRT.anchorMin = new Vector2(0.5f, 1f);
            sfxMuteRT.anchorMax = new Vector2(0.5f, 1f);
            sfxMuteRT.pivot = new Vector2(0.5f, 1f);
            sfxMuteRT.anchoredPosition = new Vector2(0f, -62f);

            settingsPanelObj.SetActive(false);

            // 5. Wire SerializedObject properties
            var so = new SerializedObject(uiController);
            so.Update();

            so.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            so.FindProperty("pauseMenuPanel").objectReferenceValue = pauseMenuPanelObj;
            so.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            so.FindProperty("pauseSettingButton").objectReferenceValue = pauseSettingBtn;
            so.FindProperty("pauseCharacterSelectButton").objectReferenceValue = resetCharBtn;

            so.FindProperty("warningConfirmPanel").objectReferenceValue = warnPanelObj;
            so.FindProperty("warningYesButton").objectReferenceValue = warnYesBtn;
            so.FindProperty("warningNoButton").objectReferenceValue = warnNoBtn;

            so.FindProperty("pauseSettingsPanel").objectReferenceValue = settingsPanelObj;
            so.FindProperty("pauseCloseSettingsButton").objectReferenceValue = closeXBtn;
            so.FindProperty("pauseBottomCloseSettingsButton").objectReferenceValue = null;
            so.FindProperty("pauseBgmVolumeSlider").objectReferenceValue = bgmSlider;
            so.FindProperty("pauseBgmVolumeText").objectReferenceValue = bgmValTxt;
            so.FindProperty("pauseBgmMuteButton").objectReferenceValue = bgmMuteBtn;
            so.FindProperty("pauseBgmMuteButtonText").objectReferenceValue = bgmMuteBtn.GetComponentInChildren<Text>();
            so.FindProperty("pauseSfxVolumeSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("pauseSfxVolumeText").objectReferenceValue = sfxValTxt;
            so.FindProperty("pauseSfxMuteButton").objectReferenceValue = sfxMuteBtn;
            so.FindProperty("pauseSfxMuteButtonText").objectReferenceValue = sfxMuteBtn.GetComponentInChildren<Text>();

            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[LevelPauseMenuSetup] Level Pause Menu Setup completed successfully!");
        }

        private static Button CreateButton(Transform parent, string name, string labelText, Font font, int fontSize, Color color, Sprite sprite, Vector2 size)
        {
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            btnObj.transform.SetParent(parent, false);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = btnObj.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
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
            txt.font = font;
            txt.fontStyle = FontStyle.Bold;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = labelText;

            return btn;
        }

        private static Slider CreateSlider(Transform parent, string name, Sprite bgSprite, Sprite fillSprite, Sprite knobSprite, Color fillColor)
        {
            var sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);

            // Background
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.25f);
            bgRT.anchorMax = new Vector2(1f, 0.75f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 1f);

            // Fill Area
            var fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var fillAreaRT = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5f, 0f);
            fillAreaRT.offsetMax = new Vector2(-5f, 0f);

            // Fill
            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var fillImg = fillObj.GetComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = fillColor;

            // Handle Slide Area
            var handleSlideAreaObj = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideAreaObj.transform.SetParent(sliderObj.transform, false);
            var handleSlideAreaRT = handleSlideAreaObj.GetComponent<RectTransform>();
            handleSlideAreaRT.anchorMin = Vector2.zero;
            handleSlideAreaRT.anchorMax = Vector2.one;
            handleSlideAreaRT.offsetMin = new Vector2(10f, 0f);
            handleSlideAreaRT.offsetMax = new Vector2(-10f, 0f);

            // Handle
            var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleObj.transform.SetParent(handleSlideAreaObj.transform, false);
            var handleRT = handleObj.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(24f, 24f);
            var handleImg = handleObj.GetComponent<Image>();
            handleImg.sprite = knobSprite;
            handleImg.color = Color.white;

            var slider = sliderObj.GetComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }
    }
}

