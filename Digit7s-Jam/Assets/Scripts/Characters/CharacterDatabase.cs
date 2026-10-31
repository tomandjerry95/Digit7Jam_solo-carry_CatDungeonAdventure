using System.Collections.Generic;
using UnityEngine;

namespace Digit7s.Characters
{
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Digit7s/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [Header("Playable Characters")]
        public List<CharacterData> characters = new List<CharacterData>();

        public int CharacterCount => characters != null ? characters.Count : 0;

        public CharacterData GetCharacter(int index)
        {
            if (characters == null || characters.Count == 0) return null;
            int clamped = Mathf.Clamp(index, 0, characters.Count - 1);
            return characters[clamped];
        }

        public CharacterData GetCharacterById(string id)
        {
            if (characters == null) return null;
            return characters.Find(c => c != null && c.characterId == id);
        }
    }
}
