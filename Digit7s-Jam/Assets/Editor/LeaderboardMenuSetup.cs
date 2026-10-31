using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Digit7s.UI;

namespace Digit7s.EditorScripts
{
    public static class LeaderboardMenuSetup
    {
        [MenuItem("Digit7s/Setup Leaderboard UI")]
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
                Debug.LogError("[LeaderboardMenuSetup] Canvas not found!");
                return;
            }

            var menuController = canvasObj.GetComponent<MainMenuController>();
            if (menuController == null)
            {
                Debug.LogError("[LeaderboardMenuSetup] MainMenuController not found!");
                return;
            }

            // Ensure LeaderboardUIController exists on Canvas
            var lbController = canvasObj.GetComponent<LeaderboardUIController>();
            if (lbController == null)
            {
                lbController = canvasObj.AddComponent<LeaderboardUIController>();
                Debug.Log("[LeaderboardMenuSetup] Added LeaderboardUIController to Canvas.");
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // 1. Setup Btn_Leaderboard in ButtonColumn
            var buttonCol = canvasObj.transform.Find("MainMenuPanel/ButtonColumn");
            if (buttonCol == null)
            {
                Debug.LogError("[LeaderboardMenuSetup] ButtonColumn not found!");
                return;
            }

            var colRT = buttonCol.GetComponent<RectTransform>();
            if (colRT != null)
            {
                colRT.sizeDelta = new Vector2(270f, 224f);
            }

            Transform lbBtnTrans = buttonCol.Find("Btn_Leaderboard");
            GameObject lbBtnObj;
            if (lbBtnTrans == null)
            {
                lbBtnObj = new GameObject("Btn_Leaderboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                lbBtnObj.transform.SetParent(buttonCol, false);
            }
            else
            {
                lbBtnObj = lbBtnTrans.gameObject;
            }
            // Sibling order: NewGame (0), Leaderboard (1), Setting (2), Quit (3)
            lbBtnObj.transform.SetSiblingIndex(1);

            var lbBtnRT = lbBtnObj.GetComponent<RectTransform>();
            lbBtnRT.sizeDelta = new Vector2(270f, 44f);

            var lbBtnImg = lbBtnObj.GetComponent<Image>();
            if (uiSprite != null)
            {
                lbBtnImg.sprite = uiSprite;
                lbBtnImg.type = Image.Type.Sliced;
            }
            lbBtnImg.color = new Color(0.85f, 0.65f, 0.15f, 1f); // Warm gold accent
            lbBtnImg.raycastTarget = true;

            var lbBtn = lbBtnObj.GetComponent<Button>();
            lbBtn.targetGraphic = lbBtnImg;
            var btnColors = lbBtn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(1f, 0.95f, 0.7f, 1f);
            btnColors.pressedColor = new Color(0.7f, 0.5f, 0.1f, 1f);
            lbBtn.colors = btnColors;

            var lbOutline = lbBtnObj.GetComponent<Outline>();
            lbOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            lbOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Transform labelTrans = lbBtnObj.transform.Find("Label");
            GameObject labelObj;
            if (labelTrans == null)
            {
                labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelObj.transform.SetParent(lbBtnObj.transform, false);
            }
            else
            {
                labelObj = labelTrans.gameObject;
            }
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var labelText = labelObj.GetComponent<Text>();
            labelText.font = font;
            labelText.fontStyle = FontStyle.Bold;
            labelText.fontSize = 16;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = "HIGH SCORES";

            // Wire serialized field on MainMenuController via SerializedObject
            var serializedMenu = new SerializedObject(menuController);
            var lbProp = serializedMenu.FindProperty("leaderboardButton");
            if (lbProp != null)
            {
                lbProp.objectReferenceValue = lbBtn;
                serializedMenu.ApplyModifiedProperties();
                Debug.Log("[LeaderboardMenuSetup] Assigned leaderboardButton to MainMenuController.");
            }

            EditorUtility.SetDirty(canvasObj);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[LeaderboardMenuSetup] Successfully set up Leaderboard Button & Controller in mainmenu scene!</color>");
        }
    }
}

