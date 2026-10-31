using System.Collections.Generic;
using UnityEngine;

namespace Digit7s.Characters
{
    public enum CatClassType
    {
        Blaster,
        Brawler,
        Guardian
    }

    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "Digit7s/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Character Information")]
        public string characterId = "character_01";
        public string characterName = "Blaster Cat";
        public string characterClass = "Ranged / Shooter";
        [TextArea(3, 10)]
        public string description = "A quick-reflex cat specialized in ranged firepower and rapid attacks.";
        public Sprite characterIcon;

        [Header("Combat Base Stats")]
        public CatClassType catClassType = CatClassType.Blaster;
        public int baseHealth = 35;
        public int baseDamage = 2;

        [Header("Attack Counter Power-Up Settings")]
        [Tooltip("Number of regular attacks before the special effect triggers on the next attack (Blaster: 4 -> 5th is x2 DMG; Guardian: 6 -> 7th is Block)")]
        public int attacksRequiredForSpecial = 4;

        [Header("Special Effect Multipliers (0-20 -> 1hp, 21-40 -> 2hp, 41-60 -> 3hp...)")]
        [Tooltip("HP gained on special proc per 20 Damage bracket (Blaster: Player Dealt DMG; Guardian: Enemy DMG)")]
        public int procHpPerDamageStep = 1;

        [Header("Stun Settings (Legacy/Unused)")]
        public int stunBlockClicksRequired = 0;

        [Header("On Kill Bonuses (Brawler)")]
        [Tooltip("HP healed on kill per 20 Enemy Total HP bracket (0-20 -> 1hp, 21-40 -> 2hp, 41-60 -> 3hp...)")]
        public int onKillHpPerEnemyHpStep = 1;

        [Header("Room Level Transition Bonus")]
        [Tooltip("Permanent Damage added per room level advance")]
        public int roomBonusDamage = 0;

        [Tooltip("HP bonus per 10 Current HP bracket on room advance (Ceil(HP/10) * multiplier)")]
        public int roomBonusHpPerHpStep = 0;

        [Header("Skins")]
        public List<CharacterSkinData> skins = new List<CharacterSkinData>();
        public int defaultSkinIndex = 0;

        public int SkinCount => skins != null ? skins.Count : 0;

        public CharacterSkinData GetSkin(int index)
        {
            if (skins == null || skins.Count == 0) return null;
            int clamped = Mathf.Clamp(index, 0, skins.Count - 1);
            return skins[clamped];
        }
    }
}
