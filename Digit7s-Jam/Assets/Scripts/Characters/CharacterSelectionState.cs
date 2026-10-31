using UnityEngine;

namespace Digit7s.Characters
{
    public static class CharacterSelectionState
    {
        private const string PrefKey_SelectedCharacter = "Digit7s_SelectedCharacterIndex";
        private const string PrefKey_SelectedSkinPrefix = "Digit7s_SelectedSkin_Char_";

        public static int SelectedCharacterIndex
        {
            get => PlayerPrefs.GetInt(PrefKey_SelectedCharacter, 0);
            set
            {
                PlayerPrefs.SetInt(PrefKey_SelectedCharacter, value);
                PlayerPrefs.Save();
            }
        }

        public static int GetSelectedSkinForCharacter(int characterIndex)
        {
            return PlayerPrefs.GetInt(PrefKey_SelectedSkinPrefix + characterIndex, 0);
        }

        public static void SetSelectedSkinForCharacter(int characterIndex, int skinIndex)
        {
            PlayerPrefs.SetInt(PrefKey_SelectedSkinPrefix + characterIndex, skinIndex);
            PlayerPrefs.Save();
        }

        public static void SaveSelection(int characterIndex, int skinIndex)
        {
            SelectedCharacterIndex = characterIndex;
            SetSelectedSkinForCharacter(characterIndex, skinIndex);
        }
    }
}
