using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Digit7s.EditorScripts
{
    public static class MainMenuSettingsSetup
    {
        [MenuItem("Digit7s/Setup Main Menu Settings")]
        public static void Setup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "mainmenu")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/mainmenu.unity", OpenSceneMode.Single);
            }

            var canvasObj = GameObject.Find("/Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[MainMenuSettingsSetup] Canvas not found!");
                return;
            }

            var menuController = canvasObj.GetComponent<Digit7s.UI.MainMenuController>();
            if (menuController == null)
            {
                Debug.LogError("[MainMenuSettingsSetup] MainMenuController not found!");
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // 1. Create or configure Btn_Setting in ButtonColumn
            var buttonCol = canvasObj.transform.Find("MainMenuPanel/ButtonColumn");
            if (buttonCol == null)
            {
                Debug.LogError("[MainMenuSettingsSetup] ButtonColumn not found!");
                return;
            }

            Transform settingBtnTrans = buttonCol.Find("Btn_Setting");
            GameObject settingBtnObj;
            if (settingBtnTrans == null)
            {
                settingBtnObj = new GameObject("Btn_Setting", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                settingBtnObj.transform.SetParent(buttonCol, false);
            }
            else
            {
                settingBtnObj = settingBtnTrans.gameObject;
            }
            settingBtnObj.transform.SetSiblingIndex(1); // Between NewGame (0) and Quit (2)

            var settingBtnRT = settingBtnObj.GetComponent<RectTransform>();
            settingBtnRT.sizeDelta = new Vector2(270f, 44f);

            var settingBtnImg = settingBtnObj.GetComponent<Image>();
            settingBtnImg.color = new Color(0.18f, 0.52f, 0.82f, 1f);
            settingBtnImg.raycastTarget = true;

            var settingBtn = settingBtnObj.GetComponent<Button>();
            settingBtn.targetGraphic = settingBtnImg;
            var btnColors = settingBtn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(0.85f, 0.92f, 1f, 1f);
            btnColors.pressedColor = new Color(0.65f, 0.75f, 0.9f, 1f);
            settingBtn.colors = btnColors;

            var settingOutline = settingBtnObj.GetComponent<Outline>();
            settingOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            settingOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Transform settingLabelTrans = settingBtnObj.transform.Find("Label");
            GameObject settingLabelObj;
            if (settingLabelTrans == null)
            {
                settingLabelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                settingLabelObj.transform.SetParent(settingBtnObj.transform, false);
            }
            else
            {
                settingLabelObj = settingLabelTrans.gameObject;
            }
            var settingLabelRT = settingLabelObj.GetComponent<RectTransform>();
            settingLabelRT.anchorMin = Vector2.zero;
            settingLabelRT.anchorMax = Vector2.one;
            settingLabelRT.offsetMin = Vector2.zero;
            settingLabelRT.offsetMax = Vector2.zero;

            var settingText = settingLabelObj.GetComponent<Text>();
            settingText.font = font;
            settingText.fontStyle = FontStyle.Bold;
            settingText.fontSize = 16;
            settingText.color = Color.white;
            settingText.alignment = TextAnchor.MiddleCenter;
            settingText.text = "⚙  SETTING";

            // 2. Create or find SettingPanel
            Transform settingPanelTrans = canvasObj.transform.Find("SettingPanel");
            GameObject settingPanelObj;
            if (settingPanelTrans != null)
            {
                GameObject.DestroyImmediate(settingPanelTrans.gameObject);
            }

            settingPanelObj = new GameObject("SettingPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            settingPanelObj.transform.SetParent(canvasObj.transform, false);

            var panelRT = settingPanelObj.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            panelRT.pivot = new Vector2(0.5f, 0.5f);

            var panelImg = settingPanelObj.GetComponent<Image>();
            panelImg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);
            panelImg.raycastTarget = true;

            // 3. Dialog WindowFrame
            var windowFrameObj = new GameObject("WindowFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            windowFrameObj.transform.SetParent(settingPanelObj.transform, false);
            var windowRT = windowFrameObj.GetComponent<RectTransform>();
            windowRT.anchorMin = new Vector2(0.5f, 0.5f);
            windowRT.anchorMax = new Vector2(0.5f, 0.5f);
            windowRT.pivot = new Vector2(0.5f, 0.5f);
            windowRT.anchoredPosition = Vector2.zero;
            windowRT.sizeDelta = new Vector2(310f, 315f);

            var windowImg = windowFrameObj.GetComponent<Image>();
            windowImg.sprite = uiSprite;
            windowImg.type = Image.Type.Sliced;
            windowImg.color = new Color(0.11f, 0.14f, 0.22f, 0.98f);
            windowImg.raycastTarget = true;

            var windowOutline = windowFrameObj.GetComponent<Outline>();
            windowOutline.effectColor = new Color(0.28f, 0.42f, 0.62f, 0.9f);
            windowOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            titleObj.transform.SetParent(windowFrameObj.transform, false);
            var titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(-15f, -14f);
            titleRT.sizeDelta = new Vector2(220f, 32f);

            var titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.fontSize = 22;
            titleTxt.color = new Color(1f, 0.85f, 0.25f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "SETTINGS";

            var titleShadow = titleObj.GetComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            titleShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Close 'X' Button
            var closeXObj = new GameObject("Btn_CloseX", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            closeXObj.transform.SetParent(windowFrameObj.transform, false);
            var closeXRT = closeXObj.GetComponent<RectTransform>();
            closeXRT.anchorMin = new Vector2(1f, 1f);
            closeXRT.anchorMax = new Vector2(1f, 1f);
            closeXRT.pivot = new Vector2(1f, 1f);
            closeXRT.anchoredPosition = new Vector2(-10f, -10f);
            closeXRT.sizeDelta = new Vector2(36f, 36f);

            var closeXImg = closeXObj.GetComponent<Image>();
            closeXImg.sprite = uiSprite;
            closeXImg.type = Image.Type.Sliced;
            closeXImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);
            closeXImg.raycastTarget = true;

            var closeXBtn = closeXObj.GetComponent<Button>();
            closeXBtn.targetGraphic = closeXImg;

            var closeXOutline = closeXObj.GetComponent<Outline>();
            closeXOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
            closeXOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var closeXLabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            closeXLabel.transform.SetParent(closeXObj.transform, false);
            var closeXLabelRT = closeXLabel.GetComponent<RectTransform>();
            closeXLabelRT.anchorMin = Vector2.zero;
            closeXLabelRT.anchorMax = Vector2.one;
            closeXLabelRT.offsetMin = Vector2.zero;
            closeXLabelRT.offsetMax = Vector2.zero;

            var closeXTxt = closeXLabel.GetComponent<Text>();
            closeXTxt.font = font;
            closeXTxt.fontStyle = FontStyle.Bold;
            closeXTxt.fontSize = 20;
            closeXTxt.color = Color.white;
            closeXTxt.alignment = TextAnchor.MiddleCenter;
            closeXTxt.text = "X";

            // Header Divider
            var headerDivObj = new GameObject("HeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            headerDivObj.transform.SetParent(windowFrameObj.transform, false);
            var headerDivRT = headerDivObj.GetComponent<RectTransform>();
            headerDivRT.anchorMin = new Vector2(0.5f, 1f);
            headerDivRT.anchorMax = new Vector2(0.5f, 1f);
            headerDivRT.pivot = new Vector2(0.5f, 1f);
            headerDivRT.anchoredPosition = new Vector2(0f, -54f);
            headerDivRT.sizeDelta = new Vector2(280f, 2f);
            var headerDivImg = headerDivObj.GetComponent<Image>();
            headerDivImg.color = new Color(0.28f, 0.38f, 0.52f, 0.6f);

            // BGM Group
            var bgmGroupObj = new GameObject("BgmGroup", typeof(RectTransform));
            bgmGroupObj.transform.SetParent(windowFrameObj.transform, false);
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

            var bgmMuteObj = new GameObject("Btn_BgmMute", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            bgmMuteObj.transform.SetParent(bgmGroupObj.transform, false);
            var bgmMuteRT = bgmMuteObj.GetComponent<RectTransform>();
            bgmMuteRT.anchorMin = new Vector2(0.5f, 1f);
            bgmMuteRT.anchorMax = new Vector2(0.5f, 1f);
            bgmMuteRT.pivot = new Vector2(0.5f, 1f);
            bgmMuteRT.anchoredPosition = new Vector2(0f, -62f);
            bgmMuteRT.sizeDelta = new Vector2(276f, 32f);

            var bgmMuteImg = bgmMuteObj.GetComponent<Image>();
            bgmMuteImg.sprite = uiSprite;
            bgmMuteImg.type = Image.Type.Sliced;
            bgmMuteImg.color = new Color(0.20f, 0.26f, 0.38f, 1f);
            bgmMuteImg.raycastTarget = true;

            var bgmMuteBtn = bgmMuteObj.GetComponent<Button>();
            bgmMuteBtn.targetGraphic = bgmMuteImg;

            var bgmMuteOutline = bgmMuteObj.GetComponent<Outline>();
            bgmMuteOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            bgmMuteOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var bgmMuteLabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            bgmMuteLabel.transform.SetParent(bgmMuteObj.transform, false);
            var bgmMuteLabelRT = bgmMuteLabel.GetComponent<RectTransform>();
            bgmMuteLabelRT.anchorMin = Vector2.zero;
            bgmMuteLabelRT.anchorMax = Vector2.one;
            bgmMuteLabelRT.offsetMin = Vector2.zero;
            bgmMuteLabelRT.offsetMax = Vector2.zero;

            var bgmMuteTxt = bgmMuteLabel.GetComponent<Text>();
            bgmMuteTxt.font = font;
            bgmMuteTxt.fontStyle = FontStyle.Bold;
            bgmMuteTxt.fontSize = 13;
            bgmMuteTxt.color = Color.white;
            bgmMuteTxt.alignment = TextAnchor.MiddleCenter;
            bgmMuteTxt.text = "MUTE BGM";

            // Mid Divider
            var midDivObj = new GameObject("MidDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            midDivObj.transform.SetParent(windowFrameObj.transform, false);
            var midDivRT = midDivObj.GetComponent<RectTransform>();
            midDivRT.anchorMin = new Vector2(0.5f, 1f);
            midDivRT.anchorMax = new Vector2(0.5f, 1f);
            midDivRT.pivot = new Vector2(0.5f, 1f);
            midDivRT.anchoredPosition = new Vector2(0f, -178f);
            midDivRT.sizeDelta = new Vector2(280f, 2f);
            var midDivImg = midDivObj.GetComponent<Image>();
            midDivImg.color = new Color(0.28f, 0.38f, 0.52f, 0.6f);

            // SFX Group
            var sfxGroupObj = new GameObject("SfxGroup", typeof(RectTransform));
            sfxGroupObj.transform.SetParent(windowFrameObj.transform, false);
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

            var sfxMuteObj = new GameObject("Btn_SfxMute", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            sfxMuteObj.transform.SetParent(sfxGroupObj.transform, false);
            var sfxMuteRT = sfxMuteObj.GetComponent<RectTransform>();
            sfxMuteRT.anchorMin = new Vector2(0.5f, 1f);
            sfxMuteRT.anchorMax = new Vector2(0.5f, 1f);
            sfxMuteRT.pivot = new Vector2(0.5f, 1f);
            sfxMuteRT.anchoredPosition = new Vector2(0f, -62f);
            sfxMuteRT.sizeDelta = new Vector2(276f, 32f);

            var sfxMuteImg = sfxMuteObj.GetComponent<Image>();
            sfxMuteImg.sprite = uiSprite;
            sfxMuteImg.type = Image.Type.Sliced;
            sfxMuteImg.color = new Color(0.20f, 0.26f, 0.38f, 1f);
            sfxMuteImg.raycastTarget = true;

            var sfxMuteBtn = sfxMuteObj.GetComponent<Button>();
            sfxMuteBtn.targetGraphic = sfxMuteImg;

            var sfxMuteOutline = sfxMuteObj.GetComponent<Outline>();
            sfxMuteOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            sfxMuteOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var sfxMuteLabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            sfxMuteLabel.transform.SetParent(sfxMuteObj.transform, false);
            var sfxMuteLabelRT = sfxMuteLabel.GetComponent<RectTransform>();
            sfxMuteLabelRT.anchorMin = Vector2.zero;
            sfxMuteLabelRT.anchorMax = Vector2.one;
            sfxMuteLabelRT.offsetMin = Vector2.zero;
            sfxMuteLabelRT.offsetMax = Vector2.zero;

            var sfxMuteTxt = sfxMuteLabel.GetComponent<Text>();
            sfxMuteTxt.font = font;
            sfxMuteTxt.fontStyle = FontStyle.Bold;
            sfxMuteTxt.fontSize = 13;
            sfxMuteTxt.color = Color.white;
            sfxMuteTxt.alignment = TextAnchor.MiddleCenter;
            sfxMuteTxt.text = "MUTE SFX";

            // Hide SettingPanel initially
            settingPanelObj.SetActive(false);

            // 4. Wire everything into MainMenuController
            var so = new SerializedObject(menuController);
            so.Update();

            so.FindProperty("settingsPanel").objectReferenceValue = settingPanelObj;
            so.FindProperty("settingButton").objectReferenceValue = settingBtn;
            so.FindProperty("closeSettingsButton").objectReferenceValue = closeXBtn;
            so.FindProperty("bottomCloseSettingsButton").objectReferenceValue = null;
            so.FindProperty("bgmVolumeSlider").objectReferenceValue = bgmSlider;
            so.FindProperty("bgmVolumeText").objectReferenceValue = bgmValTxt;
            so.FindProperty("bgmMuteButton").objectReferenceValue = bgmMuteBtn;
            so.FindProperty("bgmMuteButtonText").objectReferenceValue = bgmMuteTxt;
            so.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("sfxVolumeText").objectReferenceValue = sfxValTxt;
            so.FindProperty("sfxMuteButton").objectReferenceValue = sfxMuteBtn;
            so.FindProperty("sfxMuteButtonText").objectReferenceValue = sfxMuteTxt;

            so.ApplyModifiedProperties();

            // Run layout update
            menuController.ApplyAdaptiveLayout(true);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[MainMenuSettingsSetup] Main Menu Settings Setup completed successfully!");
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
