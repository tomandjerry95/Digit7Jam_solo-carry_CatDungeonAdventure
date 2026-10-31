using UnityEngine;
using UnityEditor;
using Digit7s.UI;
using Digit7s.Gameplay;

namespace Digit7s.Editor
{
    public static class CharacterTestRunner
    {
        [MenuItem("Digit7s/Tests/Trigger Open Character Select")]
        public static void TriggerOpenCharacterSelect()
        {
            var ctrl = Object.FindFirstObjectByType<MainMenuController>();
            if (ctrl == null)
            {
                Debug.LogError("[TestRunner] MainMenuController not found in scene!");
                return;
            }
            ctrl.OpenCharacterSelect();
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Debug.Log("[TestRunner] Opened Character Select and forced repaint!");
        }

        [MenuItem("Digit7s/Tests/Trigger Next Skin")]
        public static void TriggerNextSkin()
        {
            var ctrl = Object.FindFirstObjectByType<MainMenuController>();
            if (ctrl != null)
            {
                ctrl.NextSkin();
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                Debug.Log("[TestRunner] Called NextSkin() and forced repaint!");
            }
        }

        [MenuItem("Digit7s/Tests/Trigger Next Character")]
        public static void TriggerNextCharacter()
        {
            var ctrl = Object.FindFirstObjectByType<MainMenuController>();
            if (ctrl != null)
            {
                ctrl.NextCharacter();
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                Debug.Log("[TestRunner] Called NextCharacter() and forced repaint!");
            }
        }

        [MenuItem("Digit7s/Tests/Trigger Start Game")]
        public static void TriggerStartGame()
        {
            var ctrl = Object.FindFirstObjectByType<MainMenuController>();
            if (ctrl != null)
            {
                ctrl.OnStartGameClicked();
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                Debug.Log("[TestRunner] Called OnStartGameClicked() and forced repaint!");
            }
        }

        [MenuItem("Digit7s/Tests/Render Camera To PNG")]
        public static void RenderCameraToPNG()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[TestRunner] No Camera.main found!");
                return;
            }
            var rt = new RenderTexture(1920, 1080, 24);
            var prevRT = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevRT;

            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] bytes = ImageConversion.EncodeToPNG(tex);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);

            if (!System.IO.Directory.Exists("Assets/Screenshots"))
            {
                System.IO.Directory.CreateDirectory("Assets/Screenshots");
            }
            string path = "Assets/Screenshots/charselect_camera_render.png";
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log($"[TestRunner] Saved screenshot to {path} ({bytes.Length} bytes)");
        }
    }
}
