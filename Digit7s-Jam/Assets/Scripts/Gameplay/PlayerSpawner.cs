using UnityEngine;
using Spine.Unity;
using Digit7s.Characters;

namespace Digit7s.Gameplay
{
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Database Reference")]
        [SerializeField] private CharacterDatabase characterDatabase;

        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Vector3 bottomSpawnPosition = new Vector3(-3.1f, -10.1f, 0f);
        [SerializeField] private float playerScale = 1.5f;

        private GameObject spawnedPlayer;

        private void Start()
        {
            if (characterDatabase == null)
            {
                characterDatabase = Resources.Load<CharacterDatabase>("CharacterDatabase");
#if UNITY_EDITOR
                if (characterDatabase == null)
                {
                    characterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/Data/Characters/CharacterDatabase.asset");
                }
#endif
            }

            SpawnPlayer();
        }

        public GameObject SpawnPlayer()
        {
            if (characterDatabase == null || characterDatabase.CharacterCount == 0)
            {
                Debug.LogWarning("[PlayerSpawner] CharacterDatabase not assigned or empty!");
                return null;
            }

            int charIndex = CharacterSelectionState.SelectedCharacterIndex;
            CharacterData charData = characterDatabase.GetCharacter(charIndex);
            if (charData == null)
            {
                charData = characterDatabase.GetCharacter(0);
                charIndex = 0;
            }

            int skinIndex = CharacterSelectionState.GetSelectedSkinForCharacter(charIndex);
            CharacterSkinData skinData = charData.GetSkin(skinIndex);
            if (skinData == null && charData.SkinCount > 0)
            {
                skinData = charData.GetSkin(0);
            }

            if (skinData == null || skinData.characterPrefab == null)
            {
                Debug.LogError($"[PlayerSpawner] No character prefab configured for {charData.characterName} (Skin {skinIndex})!");
                return null;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : bottomSpawnPosition;
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            spawnedPlayer = Instantiate(skinData.characterPrefab, position, rotation);
            spawnedPlayer.name = $"Player_{charData.characterName}_{skinData.skinName}";
            spawnedPlayer.transform.localScale = Vector3.one * playerScale;

            // Ensure spine animation plays Idle
            var sa = spawnedPlayer.GetComponent<SkeletonAnimation>();
            if (sa != null && sa.SkeletonDataAsset != null)
            {
                var skelData = sa.SkeletonDataAsset.GetSkeletonData(false);
                if (skelData != null)
                {
                    var idleAnim = skelData.FindAnimation("Idle") ?? skelData.FindAnimation("idle") ?? skelData.Animations.Items[0];
                    if (idleAnim != null)
                    {
                        sa.AnimationState.SetAnimation(0, idleAnim.Name, true);
                    }
                }
            }

            // Determine Cat Class Type & Base Stats from CharacterData
            CatClassType catType = charData != null ? charData.catClassType : CatClassType.Blaster;
            int baseHp = (charData != null && charData.baseHealth > 0) ? charData.baseHealth : 35;
            int baseDmg = (charData != null && charData.baseDamage > 0) ? charData.baseDamage : 2;

            string id = charData != null && charData.characterId != null ? charData.characterId.ToLowerInvariant() : "";
            if (id.Contains("brawler") || charIndex == 1)
            {
                catType = CatClassType.Brawler;
                if (charData == null || charData.baseHealth <= 0) baseHp = 30;
                if (charData == null || charData.baseDamage <= 0) baseDmg = 4;
            }
            else if (id.Contains("guardian") || charIndex == 2)
            {
                catType = CatClassType.Guardian;
                if (charData == null || charData.baseHealth <= 0) baseHp = 40;
                if (charData == null || charData.baseDamage <= 0) baseDmg = 3;
            }

            // Ensure PlayerInstance is attached
            var playerInstance = spawnedPlayer.GetComponent<PlayerInstance>();
            if (playerInstance == null)
            {
                playerInstance = spawnedPlayer.AddComponent<PlayerInstance>();
            }

            // Ensure CatPowerUpController is attached
            var powerUp = spawnedPlayer.GetComponent<CatPowerUpController>();
            if (powerUp == null)
            {
                powerUp = spawnedPlayer.AddComponent<CatPowerUpController>();
            }

            if (PlayerInstance.HasSavedRunStats)
            {
                playerInstance.Initialize(PlayerInstance.SavedMaxHealth, PlayerInstance.SavedDamage, PlayerInstance.SavedCurrentHealth);
                powerUp.Initialize(charData, catType, PlayerInstance.SavedAttackCounter, PlayerInstance.SavedCritCharges);

                // If entered a new room, apply level-up room bonus
                if (PlayerInstance.LastAppliedRoomBonusLevel < EnemySpawner.CurrentRoomLevel)
                {
                    powerUp.ApplyRoomLevelBonus(EnemySpawner.CurrentRoomLevel);
                    PlayerInstance.LastAppliedRoomBonusLevel = EnemySpawner.CurrentRoomLevel;
                }

                Debug.Log($"[PlayerSpawner] Carried over player stats into Room {EnemySpawner.CurrentRoomLevel}: HP {playerInstance.CurrentHealth}/{playerInstance.MaxHealth}, DMG {playerInstance.Damage} ({catType})");
            }
            else
            {
                // New Run: Start with base stats, initialize powerup, and apply Level 1 bonus!
                playerInstance.Initialize(baseHp, baseDmg, baseHp);
                powerUp.Initialize(charData, catType);

                // Apply initial Level 1 bonus as requested
                powerUp.ApplyRoomLevelBonus(1);
                PlayerInstance.LastAppliedRoomBonusLevel = 1;

                Debug.Log($"[PlayerSpawner] Fresh Run initialized: {charData.characterName} ({catType}) with Level 1 bonus -> HP: {playerInstance.CurrentHealth}/{playerInstance.MaxHealth}, DMG: {playerInstance.Damage}");
            }

            return spawnedPlayer;
        }
    }
}
