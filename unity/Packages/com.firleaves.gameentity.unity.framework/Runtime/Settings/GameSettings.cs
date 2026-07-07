using System;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class GameSettings
    {
        public string Language = "zh-CN";
        public float MasterVolume = 1f;
        public float BgmVolume = 1f;
        public float SfxVolume = 1f;
        public bool Muted;

        public GameSettings Clone()
        {
            return new GameSettings
            {
                Language = Language,
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                Muted = Muted
            };
        }
    }

}
