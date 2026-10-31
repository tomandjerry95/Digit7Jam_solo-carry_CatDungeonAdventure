using UnityEngine;
using UnityEngine.UI;

namespace Digit7s.Gameplay
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraAspectAdapter : MonoBehaviour
    {
        public enum AspectMode
        {
            AdaptiveFitWidth,
            Strict9x16Letterbox
        }

        [Header("Target Aspect Ratio")]
        [Tooltip("Target aspect ratio width (default: 9)")]
        [SerializeField]
        private float targetAspectWidth = 9f;

        [Tooltip("Target aspect ratio height (default: 16)")]
        [SerializeField]
        private float targetAspectHeight = 16f;

        [Header("Camera Settings")]
        [Tooltip("Base Orthographic Size designed for 9:16 screen (e.g., 10.2 for Level scene, 5.0 for Main Menu).")]
        [SerializeField]
        private float baseOrthographicSize = 10.2f;

        [Tooltip("Aspect adaptation mode.")]
        [SerializeField]
        private AspectMode aspectMode = AspectMode.AdaptiveFitWidth;

        [Tooltip("Show clean black bars on left/right when running on wide screens like PC or Web.")]
        [SerializeField]
        private bool pillarboxWideScreens = true;

        [Header("UI Scaling")]
        [Tooltip("Automatically adjust all CanvasScaler components in the scene to Match Width.")]
        [SerializeField]
        private bool autoAdjustCanvasScaler = true;

        private Camera targetCamera;
        private static Camera letterboxBackgroundCamera;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private bool isInitialized;

        public float TargetAspect
        {
            get
            {
                if (targetAspectWidth <= 0f || targetAspectHeight <= 0f)
                {
                    return 0.5625f; // 9 / 16
                }
                return targetAspectWidth / targetAspectHeight;
            }
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            ApplyAspect(false);
            if (autoAdjustCanvasScaler)
            {
                AdjustCanvasScalers();
            }
        }

        private void Start()
        {
            isInitialized = true;
            ApplyAspect(true);
            if (autoAdjustCanvasScaler)
            {
                AdjustCanvasScalers();
            }
        }

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            ApplyAspect(isInitialized);
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                ApplyAspect(isInitialized);
                if (autoAdjustCanvasScaler)
                {
                    AdjustCanvasScalers();
                }
            }
        }

        private void OnValidate()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
            ApplyAspect(false);
        }

        [ContextMenu("Apply Aspect Now")]
        public void ApplyAspect()
        {
            ApplyAspect(isInitialized);
        }

        private void ApplyAspect(bool createBgCamera)
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
                if (targetCamera == null) return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            float currentAspect = (float)Screen.width / (float)Screen.height;
            float targetAspect = TargetAspect;

            if (!targetCamera.orthographic) return;

            if (aspectMode == AspectMode.Strict9x16Letterbox)
            {
                targetCamera.orthographicSize = baseOrthographicSize;
                ApplyStrictLetterbox(currentAspect, targetAspect, createBgCamera);
            }
            else if (currentAspect < targetAspect - 0.005f) // Taller screen than 9:16 (pillarbox top/bottom or fit width)
            {
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                targetCamera.orthographicSize = baseOrthographicSize * (targetAspect / currentAspect);
                DestroyBackgroundCamera();
            }
            else if (currentAspect > targetAspect + 0.005f) // Wider screen than 9:16 (PC, Web browser window)
            {
                if (pillarboxWideScreens)
                {
                    targetCamera.orthographicSize = baseOrthographicSize;
                    float viewWidth = targetAspect / currentAspect;
                    float xOffset = (1f - viewWidth) * 0.5f;
                    targetCamera.rect = new Rect(xOffset, 0f, viewWidth, 1f);
                    if (createBgCamera)
                    {
                        EnsureBackgroundCamera();
                    }
                }
                else
                {
                    targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    targetCamera.orthographicSize = baseOrthographicSize;
                    DestroyBackgroundCamera();
                }
            }
            else
            {
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                targetCamera.orthographicSize = baseOrthographicSize;
                DestroyBackgroundCamera();
            }
        }

        private void ApplyStrictLetterbox(float currentAspect, float targetAspect, bool createBgCamera)
        {
            if (currentAspect > targetAspect)
            {
                float viewWidth = targetAspect / currentAspect;
                float xOffset = (1f - viewWidth) * 0.5f;
                targetCamera.rect = new Rect(xOffset, 0f, viewWidth, 1f);
                if (createBgCamera) EnsureBackgroundCamera();
            }
            else if (currentAspect < targetAspect)
            {
                float viewHeight = currentAspect / targetAspect;
                float yOffset = (1f - viewHeight) * 0.5f;
                targetCamera.rect = new Rect(0f, yOffset, 1f, viewHeight);
                if (createBgCamera) EnsureBackgroundCamera();
            }
            else
            {
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                DestroyBackgroundCamera();
            }
        }

        private void EnsureBackgroundCamera()
        {
            if (Application.isPlaying)
            {
                if (letterboxBackgroundCamera == null)
                {
                    GameObject bgObj = GameObject.Find("Letterbox_Black_Background_Camera");
                    if (bgObj != null)
                    {
                        letterboxBackgroundCamera = bgObj.GetComponent<Camera>();
                    }
                    else
                    {
                        bgObj = new GameObject("Letterbox_Black_Background_Camera");
                        letterboxBackgroundCamera = bgObj.AddComponent<Camera>();
                    }

                    letterboxBackgroundCamera.clearFlags = CameraClearFlags.SolidColor;
                    letterboxBackgroundCamera.backgroundColor = Color.black;
                    letterboxBackgroundCamera.cullingMask = 0;
                    letterboxBackgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    DontDestroyOnLoad(bgObj);
                }

                if (letterboxBackgroundCamera != null && targetCamera != null)
                {
                    letterboxBackgroundCamera.depth = targetCamera.depth - 10f;
                }
            }
        }

        private void DestroyBackgroundCamera()
        {
            if (letterboxBackgroundCamera != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(letterboxBackgroundCamera.gameObject);
                }
                else
                {
                    DestroyImmediate(letterboxBackgroundCamera.gameObject);
                }
                letterboxBackgroundCamera = null;
            }
        }

        private void OnDestroy()
        {
            DestroyBackgroundCamera();
        }

        public void AdjustCanvasScalers()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (CanvasScaler scaler in scalers)
            {
                if (scaler != null)
                {
                    var canvas = scaler.GetComponent<Canvas>();
                    if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = targetCamera;
                        canvas.planeDistance = 5f;
                    }

                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(360f, 640f);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0f;
                }
            }
        }
    }
}

