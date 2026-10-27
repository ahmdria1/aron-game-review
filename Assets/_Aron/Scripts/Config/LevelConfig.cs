// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Config
{
    [CreateAssetMenu(menuName = "Aron/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public int levelIndex = 1;
        public string levelNameAr = "الرياض";
        public string musicTrack = "music_saudi_najd";
        public string bossMusicTrack = "music_stage_boss";
        public int startLives = 3;
        public int datesPerExtraLife = 100;
        [Tooltip("Remote JSON with shop sign texts/images. Empty = local fallback only.")]
        public string remoteConfigUrl = "";
    }
}
