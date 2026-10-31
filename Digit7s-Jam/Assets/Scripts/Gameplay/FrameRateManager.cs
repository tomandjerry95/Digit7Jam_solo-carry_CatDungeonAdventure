using UnityEngine;

namespace Digit7s
{
    /// <summary>
    /// Locks the target frame rate to 60 FPS and disables VSync so the lock takes effect across all platforms.
    /// Runs automatically before any scene loads.
    /// </summary>
    public static class FrameRateManager
    {
        public const int TargetFPS = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFPS;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[FrameRateManager] Target frame rate locked to {TargetFPS} FPS (vSyncCount = 0).");
#endif
        }
    }
}

