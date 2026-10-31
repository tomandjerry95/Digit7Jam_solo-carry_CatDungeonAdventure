using UnityEngine;

namespace Digit7s.UI
{
    /// <summary>
    /// Manages sound effects for all UI buttons and interactive elements across the game.
    /// Auto-initializes if not present in the scene and persists across scene loads.
    /// </summary>
    public class UISoundManager : MonoBehaviour
    {
        private static UISoundManager instance;

        public static UISoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<UISoundManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("[UISoundManager]");
                        instance = go.AddComponent<UISoundManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip startClip;
        [SerializeField] private AudioClip heroSwitchClip;
        [SerializeField] private AudioClip skinSwitchClip;
        [SerializeField] private AudioClip backClip;
        [SerializeField] private AudioClip quitClip;
        [SerializeField] private AudioClip spinBonusClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip playerAttackClip;
        [SerializeField] private AudioClip enemyHitClip;
        [SerializeField] private AudioClip criticalHitClip;
        [SerializeField] private AudioClip enemyAttackClip;
        [SerializeField] private AudioClip healthPickupClip;
        [SerializeField] private AudioClip swordPickupClip;
        [SerializeField] private AudioClip keyUnlockClip;
        [SerializeField] private AudioClip blockRevealEmptyClip;
        [SerializeField] private AudioClip blockRevealEnemyClip;
        [SerializeField] private AudioClip blockRevealHeartClip;
        [SerializeField] private AudioClip blockRevealDamageClip;
        [SerializeField] private AudioClip blockRevealShieldClip;
        [SerializeField] private AudioClip blockRevealKeyClip;
        [SerializeField] private AudioClip ladderLockedClip;
        [SerializeField] private AudioClip[] blockBreakClips;
        [SerializeField] private AudioClip catBlasterEffectClip;
        [SerializeField] private AudioClip catBrawlerEffectClip;
        [SerializeField] private AudioClip catGuardianEffectClip;
        [SerializeField] private AudioClip slotReelTickClip;
        [SerializeField] private AudioClip slotItemRevealClip;
        [SerializeField] private AudioClip roomAllEnemiesClearedClip;

        [Header("Background Music")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip dungeonNormalMusic;
        [SerializeField] private AudioClip dungeonDangerMusic;
        [SerializeField] private AudioClip gameOverMusicClip;

        private AudioSource audioSource;
        private AudioSource musicSourceMainMenu;
        private AudioSource musicSourceNormal;
        private AudioSource musicSourceDanger;
        private AudioSource musicSourceGameOver;
        private bool isDangerMusicActive = false;
        private Coroutine crossfadeRoutine = null;
        private Coroutine mainMenuFadeRoutine = null;
        private const float BGM_MAINMENU_MAX_VOL = 0.55f;
        private const float BGM_NORMAL_MAX_VOL = 0.55f;
        private const float BGM_DANGER_MAX_VOL = 0.65f;

        private const string PREF_BGM_VOLUME = "BGM_VOLUME";
        private const string PREF_BGM_MUTED = "BGM_MUTED";
        private const string PREF_SFX_VOLUME = "SFX_VOLUME";
        private const string PREF_SFX_MUTED = "SFX_MUTED";

        [Header("Audio Settings")]
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;
        [SerializeField] private bool isBgmMuted = false;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.9f;
        [SerializeField] private bool isSfxMuted = false;

        public static float BgmVolume
        {
            get => Instance != null ? Instance.bgmVolume : PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
            set => SetBgmVolume(value);
        }

        public static bool IsBgmMuted
        {
            get => Instance != null ? Instance.isBgmMuted : (PlayerPrefs.GetInt(PREF_BGM_MUTED, 0) == 1);
            set => SetBgmMute(value);
        }

        public static float SfxVolume
        {
            get => Instance != null ? Instance.sfxVolume : PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.9f);
            set => SetSfxVolume(value);
        }

        public static bool IsSfxMuted
        {
            get => Instance != null ? Instance.isSfxMuted : (PlayerPrefs.GetInt(PREF_SFX_MUTED, 0) == 1);
            set => SetSfxMute(value);
        }

        public static void SetBgmVolume(float volume)
        {
            var mgr = Instance;
            if (mgr == null) return;
            mgr.bgmVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREF_BGM_VOLUME, mgr.bgmVolume);
            PlayerPrefs.Save();
            mgr.ApplyBgmState();
        }

        public static void SetBgmMute(bool muted)
        {
            var mgr = Instance;
            if (mgr == null) return;
            mgr.isBgmMuted = muted;
            PlayerPrefs.SetInt(PREF_BGM_MUTED, muted ? 1 : 0);
            PlayerPrefs.Save();
            mgr.ApplyBgmState();
        }

        public static void ToggleBgmMute()
        {
            SetBgmMute(!IsBgmMuted);
        }

        public static void SetSfxVolume(float volume)
        {
            var mgr = Instance;
            if (mgr == null) return;
            mgr.sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, mgr.sfxVolume);
            PlayerPrefs.Save();
        }

        public static void SetSfxMute(bool muted)
        {
            var mgr = Instance;
            if (mgr == null) return;
            mgr.isSfxMuted = muted;
            PlayerPrefs.SetInt(PREF_SFX_MUTED, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleSfxMute()
        {
            SetSfxMute(!IsSfxMuted);
        }

        public void ApplyBgmState()
        {
            float mult = isBgmMuted ? 0f : bgmVolume;

            if (musicSourceMainMenu != null && musicSourceMainMenu.isPlaying)
            {
                if (mainMenuFadeRoutine != null)
                {
                    StopCoroutine(mainMenuFadeRoutine);
                    mainMenuFadeRoutine = null;
                }
                musicSourceMainMenu.volume = BGM_MAINMENU_MAX_VOL * mult;
            }

            if (musicSourceNormal != null && musicSourceNormal.isPlaying)
            {
                if (!isDangerMusicActive)
                {
                    musicSourceNormal.volume = BGM_NORMAL_MAX_VOL * mult;
                }
            }

            if (musicSourceDanger != null && musicSourceDanger.isPlaying)
            {
                if (isDangerMusicActive)
                {
                    musicSourceDanger.volume = BGM_DANGER_MAX_VOL * mult;
                }
            }

            if (musicSourceGameOver != null && musicSourceGameOver.isPlaying)
            {
                musicSourceGameOver.volume = 0.65f * mult;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.runInBackground = true;
            if (transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
            isBgmMuted = PlayerPrefs.GetInt(PREF_BGM_MUTED, 0) == 1;
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.9f);
            isSfxMuted = PlayerPrefs.GetInt(PREF_SFX_MUTED, 0) == 1;

            InitAudioSource();
            LoadClipsIfNeeded();
        }

        private void InitAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // Pure 2D audio
            audioSource.bypassEffects = true;
            audioSource.bypassListenerEffects = true;

            if (musicSourceMainMenu == null)
            {
                var menuObj = transform.Find("MusicSource_MainMenu");
                if (menuObj != null)
                {
                    musicSourceMainMenu = menuObj.GetComponent<AudioSource>();
                }
                else
                {
                    var go = new GameObject("MusicSource_MainMenu");
                    go.transform.SetParent(transform, false);
                    musicSourceMainMenu = go.AddComponent<AudioSource>();
                }
                musicSourceMainMenu.playOnAwake = false;
                musicSourceMainMenu.loop = true;
                musicSourceMainMenu.spatialBlend = 0f;
                musicSourceMainMenu.volume = 0f;
            }

            if (musicSourceNormal == null)
            {
                var normObj = transform.Find("MusicSource_Normal");
                if (normObj != null)
                {
                    musicSourceNormal = normObj.GetComponent<AudioSource>();
                }
                else
                {
                    var go = new GameObject("MusicSource_Normal");
                    go.transform.SetParent(transform, false);
                    musicSourceNormal = go.AddComponent<AudioSource>();
                }
                musicSourceNormal.playOnAwake = false;
                musicSourceNormal.loop = true;
                musicSourceNormal.spatialBlend = 0f;
                musicSourceNormal.volume = 0f;
            }

            if (musicSourceDanger == null)
            {
                var dangObj = transform.Find("MusicSource_Danger");
                if (dangObj != null)
                {
                    musicSourceDanger = dangObj.GetComponent<AudioSource>();
                }
                else
                {
                    var go = new GameObject("MusicSource_Danger");
                    go.transform.SetParent(transform, false);
                    musicSourceDanger = go.AddComponent<AudioSource>();
                }
                musicSourceDanger.playOnAwake = false;
                musicSourceDanger.loop = true;
                musicSourceDanger.spatialBlend = 0f;
                musicSourceDanger.volume = 0f;
            }

            if (musicSourceGameOver == null)
            {
                var goObj = transform.Find("MusicSource_GameOver");
                if (goObj != null)
                {
                    musicSourceGameOver = goObj.GetComponent<AudioSource>();
                }
                else
                {
                    var go = new GameObject("MusicSource_GameOver");
                    go.transform.SetParent(transform, false);
                    musicSourceGameOver = go.AddComponent<AudioSource>();
                }
                musicSourceGameOver.playOnAwake = false;
                musicSourceGameOver.loop = true;
                musicSourceGameOver.spatialBlend = 0f;
                musicSourceGameOver.volume = 0f;
            }
        }

        private void LoadClipsIfNeeded()
        {
            if (startClip == null) startClip = Resources.Load<AudioClip>("Audio/UI_StartGame");
            if (heroSwitchClip == null) heroSwitchClip = Resources.Load<AudioClip>("Audio/UI_HeroSwitch");
            if (skinSwitchClip == null) skinSwitchClip = Resources.Load<AudioClip>("Audio/UI_SkinSwitch");
            if (backClip == null) backClip = Resources.Load<AudioClip>("Audio/UI_Back");
            if (quitClip == null) quitClip = Resources.Load<AudioClip>("Audio/UI_Quit");
            if (spinBonusClip == null) spinBonusClip = Resources.Load<AudioClip>("Audio/UI_SpinBonus");
            if (buttonClickClip == null) buttonClickClip = Resources.Load<AudioClip>("Audio/UI_ButtonClick");
            if (playerAttackClip == null) playerAttackClip = Resources.Load<AudioClip>("Audio/Combat_PlayerAttack");
            if (enemyHitClip == null) enemyHitClip = Resources.Load<AudioClip>("Audio/Combat_EnemyHit");
            if (criticalHitClip == null) criticalHitClip = Resources.Load<AudioClip>("Audio/Combat_CriticalHit");
            if (enemyAttackClip == null) enemyAttackClip = Resources.Load<AudioClip>("Audio/Combat_EnemyAttack");
            if (healthPickupClip == null) healthPickupClip = Resources.Load<AudioClip>("Audio/Item_HealthPickup");
            if (swordPickupClip == null) swordPickupClip = Resources.Load<AudioClip>("Audio/Item_SwordPickup");
            if (keyUnlockClip == null) keyUnlockClip = Resources.Load<AudioClip>("Audio/Item_KeyUnlock");
            if (blockRevealEmptyClip == null) blockRevealEmptyClip = Resources.Load<AudioClip>("Audio/Block_Reveal_Empty");
            if (blockRevealEnemyClip == null) blockRevealEnemyClip = Resources.Load<AudioClip>("Audio/Block_Reveal_Enemy");
            if (blockRevealHeartClip == null) blockRevealHeartClip = Resources.Load<AudioClip>("Audio/Block_Reveal_Heart");
            if (blockRevealDamageClip == null) blockRevealDamageClip = Resources.Load<AudioClip>("Audio/Block_Reveal_Damage");
            if (blockRevealShieldClip == null) blockRevealShieldClip = Resources.Load<AudioClip>("Audio/Block_Reveal_Shield");
            if (blockRevealKeyClip == null) blockRevealKeyClip = Resources.Load<AudioClip>("Audio/Block_Reveal_Key");
            if (ladderLockedClip == null) ladderLockedClip = Resources.Load<AudioClip>("Audio/Ladder_Locked");
            if (catBlasterEffectClip == null) catBlasterEffectClip = Resources.Load<AudioClip>("Audio/Skill_CatBlaster");
            if (catBrawlerEffectClip == null) catBrawlerEffectClip = Resources.Load<AudioClip>("Audio/Skill_CatBrawler");
            if (catGuardianEffectClip == null) catGuardianEffectClip = Resources.Load<AudioClip>("Audio/Skill_CatGuardian");
            if (slotReelTickClip == null) slotReelTickClip = Resources.Load<AudioClip>("Audio/Slot_ReelTick");
            if (slotItemRevealClip == null) slotItemRevealClip = Resources.Load<AudioClip>("Audio/Slot_ItemReveal");
            if (roomAllEnemiesClearedClip == null) roomAllEnemiesClearedClip = Resources.Load<AudioClip>("Audio/Room_AllEnemiesCleared");
            if (mainMenuMusic == null) mainMenuMusic = Resources.Load<AudioClip>("Audio/BGM_MainMenu");
            if (dungeonNormalMusic == null) dungeonNormalMusic = Resources.Load<AudioClip>("Audio/BGM_Dungeon_Normal");
            if (dungeonDangerMusic == null) dungeonDangerMusic = Resources.Load<AudioClip>("Audio/BGM_Dungeon_Danger");
            if (gameOverMusicClip == null) gameOverMusicClip = Resources.Load<AudioClip>("Audio/BGM_GameOver");
            if (blockBreakClips == null || blockBreakClips.Length == 0)
            {
                var clipsList = new System.Collections.Generic.List<AudioClip>();
                for (int i = 1; i <= 4; i++)
                {
                    var c = Resources.Load<AudioClip>($"Audio/Block_Break_{i}");
                    if (c != null) clipsList.Add(c);
                }
                if (clipsList.Count == 0 && blockRevealEmptyClip != null)
                {
                    clipsList.Add(blockRevealEmptyClip);
                }
                blockBreakClips = clipsList.ToArray();
            }
        }

        /// <summary>
        /// Plays an audio clip with specified volume.
        /// </summary>
        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            if (isSfxMuted) return;
            float finalVol = Mathf.Clamp01(volume * sfxVolume);
            if (finalVol <= 0.0001f) return;

            InitAudioSource();
            if (audioSource != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip, finalVol);
            }
        }

        // ==========================================
        // Static helper methods for UI buttons
        // ==========================================

        /// <summary>
        /// Uplifting rising fanfare sound for Start Game / New Game buttons.
        /// </summary>
        public static void PlayStart()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.startClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.startClip, 0.9f);
            }
        }

        /// <summary>
        /// Punchy melodic pop for switching characters / hero tabs.
        /// </summary>
        public static void PlayHeroSwitch()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.heroSwitchClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.heroSwitchClip, 0.85f);
            }
        }

        /// <summary>
        /// Crisp crystal bell blip for skin prev/next and skin dot selectors.
        /// </summary>
        public static void PlaySkinSwitch()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.skinSwitchClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.skinSwitchClip, 0.8f);
            }
        }

        /// <summary>
        /// Soft descending cancel chime for Back / Return buttons.
        /// </summary>
        public static void PlayBack()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.backClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.backClip, 0.85f);
            }
        }

        /// <summary>
        /// Low mechanical wooden latch sound for Quit / Exit buttons.
        /// </summary>
        public static void PlayQuit()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.quitClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.quitClip, 0.9f);
            }
        }

        /// <summary>
        /// Shimmering golden slot machine chime for Bonus Spin buttons.
        /// </summary>
        public static void PlaySpinBonus()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.spinBonusClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.spinBonusClip, 0.95f);
            }
        }

        /// <summary>
        /// Snappy tactile tap for general buttons, restart, character select, etc.
        /// </summary>
        public static void PlayClick()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.buttonClickClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.buttonClickClip, 0.85f);
            }
        }

        /// <summary>
        /// Energetic blaster laser sound when player attacks an enemy.
        /// </summary>
        public static void PlayPlayerAttack()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.playerAttackClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.playerAttackClip, 0.9f);
            }
        }

        /// <summary>
        /// Crunchy visceral impact sound when an enemy takes damage (or critical hit blast).
        /// </summary>
        public static void PlayEnemyHit(bool isCrit = false)
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.enemyHitClip == null) mgr.LoadClipsIfNeeded();
                if (isCrit && mgr.criticalHitClip != null)
                {
                    mgr.Play(mgr.criticalHitClip, 1.0f);
                }
                else
                {
                    mgr.Play(mgr.enemyHitClip, 0.9f);
                }
            }
        }

        /// <summary>
        /// Menacing claw slash whoosh when an enemy counter-attacks.
        /// </summary>
        public static void PlayEnemyAttack()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.enemyAttackClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.enemyAttackClip, 0.9f);
            }
        }

        /// <summary>
        /// Sweet sparkling healing chime when health/heart item is picked up.
        /// </summary>
        public static void PlayHealthPickup()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.healthPickupClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.healthPickupClip, 0.95f);
            }
        }

        /// <summary>
        /// Crisp metallic power-up sword unsheathe shing when sword/damage item is picked up.
        /// </summary>
        public static void PlaySwordPickup()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.swordPickupClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.swordPickupClip, 0.95f);
            }
        }

        /// <summary>
        /// Mechanical key turn click and open latch chime when the key unlocks the ladder.
        /// </summary>
        public static void PlayKeyUnlock()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.keyUnlockClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.keyUnlockClip, 1.0f);
            }
        }

        // ==========================================
        // Block Reveal Sound Methods
        // ==========================================

        /// <summary>
        /// Plays the single best fixed stone fracture audio clip with a subtle volume variation
        /// ("ပုံသေ အကောင်းဆုံးသင့်တဲ့ အသံတခုပဲ + volume အတိုးအလျော့လေးနည်းနည်း").
        /// </summary>
        public static void PlayBlockBreak()
        {
            var mgr = Instance;
            if (mgr == null) return;
            if (mgr.blockRevealEmptyClip == null && (mgr.blockBreakClips == null || mgr.blockBreakClips.Length == 0))
            {
                mgr.LoadClipsIfNeeded();
            }

            AudioClip fixedBestClip = mgr.blockRevealEmptyClip;
            if (fixedBestClip == null && mgr.blockBreakClips != null && mgr.blockBreakClips.Length > 0)
            {
                fixedBestClip = mgr.blockBreakClips[0];
            }

            // Slight volume variation only ("volume အတိုးအလျော့လေးနည်းနည်း"), fixed natural pitch (1.0f)
            float subtleVol = UnityEngine.Random.Range(0.86f, 1.0f);
            mgr.Play(fixedBestClip, subtleVol, 1.0f);
        }

        /// <summary>
        /// Crisp stone crack & crumble sound when breaking an empty block (fixed best clip + subtle volume variation).
        /// </summary>
        public static void PlayBlockRevealEmpty()
        {
            PlayBlockBreak();
        }

        /// <summary>
        /// Menacing alert danger stinger when an enemy is revealed under a block.
        /// </summary>
        public static void PlayBlockRevealEnemy()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.blockRevealEnemyClip == null) mgr.LoadClipsIfNeeded();
                float vol = UnityEngine.Random.Range(0.88f, 1.0f);
                mgr.Play(mgr.blockRevealEnemyClip, vol, 1.0f);
            }
        }

        /// <summary>
        /// Sweet sparkling fairy healing chime when a Heart (Hp) item is revealed from a block.
        /// </summary>
        public static void PlayBlockRevealHeart()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.blockRevealHeartClip == null) mgr.LoadClipsIfNeeded();
                float vol = UnityEngine.Random.Range(0.88f, 1.0f);
                mgr.Play(mgr.blockRevealHeartClip, vol, 1.0f);
            }
        }

        /// <summary>
        /// Sharp metallic power-up sword shing when Damage (Sword) item is revealed from a block.
        /// </summary>
        public static void PlayBlockRevealDamage()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.blockRevealDamageClip == null) mgr.LoadClipsIfNeeded();
                float vol = UnityEngine.Random.Range(0.88f, 1.0f);
                mgr.Play(mgr.blockRevealDamageClip, vol, 1.0f);
            }
        }

        /// <summary>
        /// Crystalline protective energy pulse when a Shield item is revealed from a block.
        /// </summary>
        public static void PlayBlockRevealShield()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.blockRevealShieldClip == null) mgr.LoadClipsIfNeeded();
                float vol = UnityEngine.Random.Range(0.88f, 1.0f);
                mgr.Play(mgr.blockRevealShieldClip, vol, 1.0f);
            }
        }

        /// <summary>
        /// Bright magical golden discovery fanfare when the Gold Key is revealed from a block.
        /// </summary>
        public static void PlayBlockRevealKey()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.blockRevealKeyClip == null) mgr.LoadClipsIfNeeded();
                float vol = UnityEngine.Random.Range(0.90f, 1.0f);
                mgr.Play(mgr.blockRevealKeyClip, vol, 1.0f);
            }
        }

        /// <summary>
        /// Heavy iron padlock rattle and refusal thud when player clicks on the locked ladder without having the key.
        /// </summary>
        public static void PlayLadderLocked()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.ladderLockedClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.ladderLockedClip, 1.0f);
            }
        }

        /// <summary>
        /// High-pitched sparkling chime when a Coin is picked up.
        /// </summary>
        public static void PlayCoinPickup()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.keyUnlockClip == null) mgr.LoadClipsIfNeeded();
                float pitch = UnityEngine.Random.Range(1.30f, 1.50f);
                mgr.Play(mgr.keyUnlockClip ?? mgr.buttonClickClip, 0.85f, pitch);
            }
        }

        // ==========================================
        // Character Special Effect SFX Methods
        // ==========================================

        /// <summary>
        /// High-tech twin-laser plasma overcharge & harmonic chime when Cat Blaster's special effect triggers.
        /// </summary>
        public static void PlayCatBlasterEffect()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.catBlasterEffectClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.catBlasterEffectClip ?? mgr.criticalHitClip, 0.95f, 1.0f);
            }
        }

        /// <summary>
        /// Heavy fiery uppercut impact & warm power surge when Cat Brawler's special effect triggers.
        /// </summary>
        public static void PlayCatBrawlerEffect()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.catBrawlerEffectClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.catBrawlerEffectClip ?? mgr.criticalHitClip, 0.95f, 1.0f);
            }
        }

        /// <summary>
        /// Resonant metallic aegis shield clang & holy barrier bell when Cat Guardian's special effect triggers.
        /// </summary>
        public static void PlayCatGuardianEffect()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.catGuardianEffectClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.catGuardianEffectClip ?? mgr.blockRevealShieldClip, 0.95f, 1.0f);
            }
        }

        // ==========================================
        // Bonus Slot Machine SFX Methods
        // ==========================================

        /// <summary>
        /// Crisp tactile mechanical ticker when cards scroll past in the horizontal bonus reel.
        /// </summary>
        public static void PlaySlotReelTick(float pitch = 1.0f)
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.slotReelTickClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.slotReelTickClip ?? mgr.buttonClickClip, 0.70f, pitch);
            }
        }

        /// <summary>
        /// Triumphant shimmering golden fanfare chord when the winning bonus item is landed and revealed.
        /// </summary>
        public static void PlaySlotItemReveal()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.slotItemRevealClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.slotItemRevealClip ?? mgr.spinBonusClip, 0.95f, 1.0f);
            }
        }

        // ==========================================
        // Dynamic Dungeon Background Music (BGM)
        // ==========================================

        /// <summary>
        /// Updates dynamic background music in the level room.
        /// Switches smoothly to Danger BGM when total active enemy damage > player's health,
        /// or returns to Normal BGM when safe / high health.
        /// </summary>
        public static void SetDungeonMusicDangerState(bool isDanger)
        {
            var mgr = Instance;
            if (mgr != null)
            {
                mgr.UpdateDungeonMusic(isDanger);
            }
        }

        public void UpdateDungeonMusic(bool isDanger)
        {
            InitAudioSource();
            LoadClipsIfNeeded();
            StopMainMenuMusic(false);

            if (musicSourceNormal != null && dungeonNormalMusic != null && musicSourceNormal.clip != dungeonNormalMusic)
            {
                musicSourceNormal.clip = dungeonNormalMusic;
            }
            if (musicSourceDanger != null && dungeonDangerMusic != null && musicSourceDanger.clip != dungeonDangerMusic)
            {
                musicSourceDanger.clip = dungeonDangerMusic;
            }

            if (musicSourceNormal != null && !musicSourceNormal.isPlaying && musicSourceNormal.clip != null)
            {
                musicSourceNormal.Play();
            }
            if (musicSourceDanger != null && !musicSourceDanger.isPlaying && musicSourceDanger.clip != null)
            {
                musicSourceDanger.Play();
            }

            if (crossfadeRoutine == null || isDanger != isDangerMusicActive)
            {
                isDangerMusicActive = isDanger;
                if (crossfadeRoutine != null)
                {
                    StopCoroutine(crossfadeRoutine);
                }
                crossfadeRoutine = StartCoroutine(CrossfadeMusicRoutine(isDanger));
            }
        }

        private System.Collections.IEnumerator CrossfadeMusicRoutine(bool toDanger)
        {
            if (musicSourceNormal == null || musicSourceDanger == null) yield break;

            float duration = 0.65f;
            float elapsed = 0f;

            float startNorm = musicSourceNormal.volume;
            float startDang = musicSourceDanger.volume;

            float mult = isBgmMuted ? 0f : bgmVolume;
            float targetNorm = toDanger ? 0f : (BGM_NORMAL_MAX_VOL * mult);
            float targetDang = toDanger ? (BGM_DANGER_MAX_VOL * mult) : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                musicSourceNormal.volume = Mathf.Lerp(startNorm, targetNorm, t);
                musicSourceDanger.volume = Mathf.Lerp(startDang, targetDang, t);
                yield return null;
            }

            musicSourceNormal.volume = targetNorm;
            musicSourceDanger.volume = targetDang;
            crossfadeRoutine = null;
        }

        /// <summary>
        /// Stops both dungeon music tracks (e.g. on game over or return to main menu).
        /// </summary>
        public static void StopDungeonMusic()
        {
            var mgr = Instance;
            if (mgr == null) return;
            if (mgr.crossfadeRoutine != null)
            {
                mgr.StopCoroutine(mgr.crossfadeRoutine);
                mgr.crossfadeRoutine = null;
            }
            if (mgr.musicSourceNormal != null) mgr.musicSourceNormal.Stop();
            if (mgr.musicSourceDanger != null) mgr.musicSourceDanger.Stop();
        }

        /// <summary>
        /// Triumphant, sparkling fanfare when all enemies in the room are defeated and bonus item is ready.
        /// </summary>
        public static void PlayRoomAllEnemiesCleared()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                if (mgr.roomAllEnemiesClearedClip == null) mgr.LoadClipsIfNeeded();
                mgr.Play(mgr.roomAllEnemiesClearedClip ?? mgr.spinBonusClip, 1.0f, 1.0f);
            }
        }

        /// <summary>
        /// Plays somber, atmospheric Game Over background music when the player is defeated.
        /// </summary>
        public static void PlayGameOverMusic()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                mgr.StartGameOverMusicInternal();
            }
        }

        private void StartGameOverMusicInternal()
        {
            InitAudioSource();
            LoadClipsIfNeeded();
            StopDungeonMusic();

            if (musicSourceGameOver != null && gameOverMusicClip != null)
            {
                musicSourceGameOver.clip = gameOverMusicClip;
                float mult = isBgmMuted ? 0f : bgmVolume;
                musicSourceGameOver.volume = 0.65f * mult;
                if (!musicSourceGameOver.isPlaying)
                {
                    musicSourceGameOver.Play();
                }
            }
        }

        /// <summary>
        /// Stops the Game Over background music (e.g. when restarting or going to character select).
        /// </summary>
        public static void StopGameOverMusic()
        {
            var mgr = Instance;
            if (mgr != null && mgr.musicSourceGameOver != null)
            {
                mgr.musicSourceGameOver.Stop();
            }
        }

        // ==========================================
        // Main Menu Background Music (BGM)
        // ==========================================

        /// <summary>
        /// Plays cheerful, looping background music for the Main Menu with smooth fade in.
        /// Automatically stops any dungeon or game over tracks.
        /// </summary>
        public static void PlayMainMenuMusic()
        {
            var mgr = Instance;
            if (mgr != null)
            {
                mgr.StartMainMenuMusicInternal();
            }
        }

        private void StartMainMenuMusicInternal()
        {
            InitAudioSource();
            LoadClipsIfNeeded();
            StopDungeonMusic();
            StopGameOverMusic();

            if (musicSourceMainMenu != null && mainMenuMusic != null)
            {
                if (musicSourceMainMenu.clip != mainMenuMusic)
                {
                    musicSourceMainMenu.clip = mainMenuMusic;
                }

                float mult = isBgmMuted ? 0f : bgmVolume;
                float targetVol = BGM_MAINMENU_MAX_VOL * mult;

                if (!musicSourceMainMenu.isPlaying)
                {
                    musicSourceMainMenu.volume = 0f;
                    musicSourceMainMenu.Play();
                    if (mainMenuFadeRoutine != null)
                    {
                        StopCoroutine(mainMenuFadeRoutine);
                    }
                    mainMenuFadeRoutine = StartCoroutine(FadeAudioSource(musicSourceMainMenu, targetVol, 0.5f));
                }
                else if (Mathf.Abs(musicSourceMainMenu.volume - targetVol) > 0.01f && mainMenuFadeRoutine == null)
                {
                    mainMenuFadeRoutine = StartCoroutine(FadeAudioSource(musicSourceMainMenu, targetVol, 0.5f));
                }
            }
        }

        /// <summary>
        /// Smoothly fades out and stops the Main Menu background music (e.g. when starting gameplay).
        /// </summary>
        public static void StopMainMenuMusic(bool immediate = false)
        {
            var mgr = Instance;
            if (mgr == null || mgr.musicSourceMainMenu == null) return;

            if (mgr.mainMenuFadeRoutine != null)
            {
                mgr.StopCoroutine(mgr.mainMenuFadeRoutine);
                mgr.mainMenuFadeRoutine = null;
            }

            if (immediate)
            {
                mgr.musicSourceMainMenu.Stop();
                mgr.musicSourceMainMenu.volume = 0f;
            }
            else if (mgr.musicSourceMainMenu.isPlaying)
            {
                mgr.mainMenuFadeRoutine = mgr.StartCoroutine(mgr.FadeOutAndStop(mgr.musicSourceMainMenu, 0.4f));
            }
        }

        private System.Collections.IEnumerator FadeAudioSource(AudioSource src, float targetVol, float duration)
        {
            if (src == null) yield break;
            float startVol = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                src.volume = Mathf.Lerp(startVol, targetVol, t);
                yield return null;
            }
            src.volume = targetVol;
            mainMenuFadeRoutine = null;
        }

        private System.Collections.IEnumerator FadeOutAndStop(AudioSource src, float duration)
        {
            if (src == null) yield break;
            float startVol = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                src.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }
            src.volume = 0f;
            src.Stop();
            mainMenuFadeRoutine = null;
        }
    }
}

