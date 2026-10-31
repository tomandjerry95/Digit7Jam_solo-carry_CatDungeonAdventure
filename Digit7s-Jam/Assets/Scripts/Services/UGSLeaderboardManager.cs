using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

namespace Digit7s.Services
{
    [Serializable]
    public struct LeaderboardEntryData
    {
        public int rank;
        public string playerName;
        public int score;
        public int catClassIndex; // 0: Blaster, 1: Brawler, 2: Guardian
        public bool isCurrentPlayer;
    }

    /// <summary>
    /// Manages Unity Gaming Services (UGS) Authentication and Leaderboards.
    /// Handles unique player name generation (PlayerName + Device ID suffix),
    /// anonymous sign-in, submitting scores (All-Time & Weekly), and fetching top 25 scores.
    /// </summary>
    public class UGSLeaderboardManager : MonoBehaviour
    {
        public static UGSLeaderboardManager Instance { get; private set; }

        private const string PREF_RAW_NAME = "UGS_PlayerName_Raw";
        private const string PREF_UNIQUE_NAME = "UGS_PlayerName_Unique";
        private const string PREF_LOCAL_BEST = "UGS_LocalBestScore";
        private const string FALLBACK_DEVICE_PREF = "UGS_FallbackDeviceId";

        [Header("Leaderboard IDs (Configured in UGS Dashboard)")]
        [SerializeField] private string allTimeLeaderboardId = "AllTime";
        [SerializeField] private string weeklyLeaderboardId = "Weekly";

        public string AllTimeLeaderboardId => allTimeLeaderboardId;
        public string WeeklyLeaderboardId => weeklyLeaderboardId;

        public bool IsInitialized { get; private set; } = false;
        public bool IsSignedIn => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
        public bool IsReady => IsInitialized && IsSignedIn;

        public event Action<bool> OnInitializeCompleted;
        public event Action<string> OnPlayerNameUpdated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("UGSLeaderboardManager");
                Instance = go.AddComponent<UGSLeaderboardManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await InitializeAsync();
        }

        /// <summary>
        /// Initializes Unity Services and signs in anonymously using device identity.
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (IsReady) return true;

            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    // Use device suffix as profile to keep player anonymous session consistent
                    var options = new InitializationOptions();
                    string deviceProfile = $"dev_{GetDeviceSuffix().ToLowerInvariant()}";
                    options.SetProfile(deviceProfile);

                    await UnityServices.InitializeAsync(options);
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                IsInitialized = true;
                Debug.Log($"<color=#00E5FF>[UGSLeaderboard] Initialized & Signed In! PlayerId: {AuthenticationService.Instance.PlayerId}</color>");

                // Sync name if previously saved
                if (HasSetPlayerName())
                {
                    string uniqueName = GetUniquePlayerName();
                    try
                    {
                        await AuthenticationService.Instance.UpdatePlayerNameAsync(uniqueName);
                    }
                    catch (Exception nameEx)
                    {
                        Debug.LogWarning($"[UGSLeaderboard] Sync name notice: {nameEx.Message}");
                    }
                }

                OnInitializeCompleted?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGSLeaderboard] Initialization failed (Check Internet or Project ID Link): {ex.Message}");
                OnInitializeCompleted?.Invoke(false);
                return false;
            }
        }

        /// <summary>
        /// Returns a 4-character uppercase unique suffix derived from the device's hardware ID.
        /// </summary>
        public static string GetDeviceSuffix()
        {
            string id = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(id) || id == SystemInfo.unsupportedIdentifier)
            {
                if (!PlayerPrefs.HasKey(FALLBACK_DEVICE_PREF))
                {
                    string randId = UnityEngine.Random.Range(0x1000, 0xFFFF).ToString("X4");
                    PlayerPrefs.SetString(FALLBACK_DEVICE_PREF, randId);
                    PlayerPrefs.Save();
                }
                return PlayerPrefs.GetString(FALLBACK_DEVICE_PREF);
            }

            int len = id.Length;
            string sub = len >= 4 ? id.Substring(len - 4) : id;
            return sub.ToUpperInvariant();
        }

        /// <summary>
        /// Sanitizes player input name (removes invalid characters, converts spaces to underscores, limits length).
        /// </summary>
        public static string SanitizeRawName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Hero";

            var sb = new StringBuilder();
            foreach (char c in raw.Trim())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (c == ' ' || c == '_' || c == '-')
                {
                    sb.Append('_');
                }
            }

            string clean = sb.ToString();
            if (clean.Length > 12) clean = clean.Substring(0, 12);
            if (string.IsNullOrEmpty(clean)) clean = "Hero";
            return clean;
        }

        public bool HasSetPlayerName()
        {
            return PlayerPrefs.HasKey(PREF_RAW_NAME) && !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_RAW_NAME));
        }

        public string GetRawPlayerName()
        {
            return PlayerPrefs.GetString(PREF_RAW_NAME, "");
        }

        public string GetUniquePlayerName()
        {
            string raw = GetRawPlayerName();
            if (string.IsNullOrEmpty(raw)) raw = "Hero";
            return $"{SanitizeRawName(raw)}_{GetDeviceSuffix()}";
        }

        /// <summary>
        /// Sets and saves the player name. Combines raw name with device unique ID.
        /// Updates UGS Authentication display name.
        /// </summary>
        public async Task<bool> SetPlayerNameAsync(string rawName)
        {
            string cleanRaw = SanitizeRawName(rawName);
            string uniqueDisplayName = $"{cleanRaw}_{GetDeviceSuffix()}";

            PlayerPrefs.SetString(PREF_RAW_NAME, cleanRaw);
            PlayerPrefs.SetString(PREF_UNIQUE_NAME, uniqueDisplayName);
            PlayerPrefs.Save();

            if (!IsReady)
            {
                await InitializeAsync();
            }

            if (IsReady)
            {
                try
                {
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(uniqueDisplayName);
                    Debug.Log($"<color=#00E5FF>[UGSLeaderboard] Updated player name in Cloud: {uniqueDisplayName}</color>");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UGSLeaderboard] Failed to update player name in cloud: {ex.Message}");
                }
            }

            OnPlayerNameUpdated?.Invoke(uniqueDisplayName);
            return true;
        }

        public int GetLocalBestScore()
        {
            return PlayerPrefs.GetInt(PREF_LOCAL_BEST, 0);
        }

        /// <summary>
        /// Fetches the current player's recorded score on Cloud for a specific leaderboard.
        /// Returns -1 if player has no entry yet or cannot connect.
        /// </summary>
        public async Task<int> GetPlayerCloudScoreAsync(string leaderboardId)
        {
            if (!IsReady || string.IsNullOrEmpty(leaderboardId)) return -1;

            try
            {
                var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
                if (entry != null)
                {
                    return (int)entry.Score;
                }
            }
            catch (Exception)
            {
                // Expected when player hasn't submitted an entry yet (404 Not Found)
            }

            return -1;
        }

        /// <summary>
        /// Submits score to a specific leaderboard ONLY IF newScore is greater than existing cloud score.
        /// Strictly ensures only the MAXIMUM (highest) score is recorded and never accumulates.
        /// </summary>
        public async Task<bool> SubmitIfHigherAsync(string leaderboardId, int newScore, int catClassIndex)
        {
            if (string.IsNullOrEmpty(leaderboardId) || newScore <= 0) return false;

            try
            {
                int currentCloudBest = await GetPlayerCloudScoreAsync(leaderboardId);

                // If cloud already has a score, only update if newScore is strictly greater (Max-only)
                if (currentCloudBest > 0 && newScore <= currentCloudBest)
                {
                    Debug.Log($"<color=#FFD700>[UGSLeaderboard] Run level {newScore} on '{leaderboardId}' is not higher than best ({currentCloudBest}). Kept maximum record!</color>");
                    return true;
                }

                // If cloud check was unavailable (-1), check against local best as fallback
                if (currentCloudBest < 0)
                {
                    int localBest = GetLocalBestScore();
                    if (localBest > 0 && newScore <= localBest)
                    {
                        Debug.Log($"<color=#FFD700>[UGSLeaderboard] Run level {newScore} on '{leaderboardId}' is not higher than local best ({localBest}). Kept maximum record!</color>");
                        return true;
                    }
                }

                var scoreOptions = new AddPlayerScoreOptions
                {
                    Metadata = new Dictionary<string, string> { { "cat", catClassIndex.ToString() } }
                };

                var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, newScore, scoreOptions);
                Debug.Log($"<color=green>[UGSLeaderboard] Updated new HIGHEST score on '{leaderboardId}': Lv. {newScore} (Previous: {currentCloudBest}, Cat: {catClassIndex})</color>");
                return entry != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGSLeaderboard] Submission notice for '{leaderboardId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Submits score to both All-Time and Weekly leaderboards.
        /// Strictly updates ONLY IF newScore is greater than the existing record for each leaderboard (Max only, never accumulates).
        /// Attaches Cat Class Index (0: Blaster, 1: Brawler, 2: Guardian) in metadata.
        /// Ties score to player's unique device UGS identity so duplicates are never created.
        /// </summary>
        public async Task<bool> SubmitScoreAsync(int score, int catClassIndex = 0)
        {
            if (score <= 0) return false;

            int localBest = GetLocalBestScore();
            if (score > localBest)
            {
                PlayerPrefs.SetInt(PREF_LOCAL_BEST, score);
                PlayerPrefs.SetInt("UGS_LocalBestCat", catClassIndex);
                PlayerPrefs.Save();
            }

            if (!IsReady)
            {
                bool initOk = await InitializeAsync();
                if (!initOk) return false;
            }

            var allTimeTask = SubmitIfHigherAsync(allTimeLeaderboardId, score, catClassIndex);
            var weeklyTask = SubmitIfHigherAsync(weeklyLeaderboardId, score, catClassIndex);

            await Task.WhenAll(allTimeTask, weeklyTask);

            return allTimeTask.Result || weeklyTask.Result;
        }

        /// <summary>
        /// Fetches top scores from the specified leaderboard up to the given limit (default 25).
        /// </summary>
        public async Task<List<LeaderboardEntryData>> GetScoresAsync(string leaderboardId, int limit = 25)
        {
            if (!IsReady)
            {
                bool initOk = await InitializeAsync();
                if (!initOk) return null;
            }

            try
            {
                var options = new GetScoresOptions
                {
                    Limit = Mathf.Clamp(limit, 1, 25),
                    IncludeMetadata = true
                };
                var page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
                var resultList = new List<LeaderboardEntryData>();
                string myPlayerId = AuthenticationService.Instance?.PlayerId;

                if (page?.Results != null)
                {
                    foreach (var entry in page.Results)
                    {
                        int catIndex = 0;
                        if (!string.IsNullOrEmpty(entry.Metadata))
                        {
                            if (entry.Metadata.Contains("\"cat\":\"1\"") || entry.Metadata.Contains("\"cat\":1"))
                                catIndex = 1;
                            else if (entry.Metadata.Contains("\"cat\":\"2\"") || entry.Metadata.Contains("\"cat\":2"))
                                catIndex = 2;
                            else
                                catIndex = 0;
                        }

                        resultList.Add(new LeaderboardEntryData
                        {
                            rank = entry.Rank + 1,
                            playerName = string.IsNullOrEmpty(entry.PlayerName) ? "Nameless Hero" : entry.PlayerName,
                            score = (int)entry.Score,
                            catClassIndex = catIndex,
                            isCurrentPlayer = !string.IsNullOrEmpty(myPlayerId) && entry.PlayerId == myPlayerId
                        });
                    }
                }

                return resultList;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGSLeaderboard] Failed to fetch scores for {leaderboardId}: {ex.Message}");
                return null;
            }
        }
    }
}

