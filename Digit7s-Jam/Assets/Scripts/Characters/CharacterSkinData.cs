using UnityEngine;
using Spine.Unity;

namespace Digit7s.Characters
{
    [System.Serializable]
    public class CharacterSkinData
    {
        [Tooltip("Display name of the skin (e.g. Classic, Golden, Cyber)")]
        public string skinName = "Default Skin";

        [Tooltip("The playable character prefab instantiated in the game scene")]
        public GameObject characterPrefab;

        [Tooltip("Spine SkeletonDataAsset for live animation and preview")]
        public SkeletonDataAsset skeletonDataAsset;

        [Tooltip("UI Icon / Portrait for this skin")]
        public Sprite skinIcon;

        [Tooltip("Theme color associated with this skin for UI highlights")]
        public Color themeColor = Color.white;
    }
}
