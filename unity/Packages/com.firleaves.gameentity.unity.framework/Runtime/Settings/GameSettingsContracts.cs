using System;

namespace GameEntity.Unity.Framework
{
    public interface IGameSettings
    {
        GameSettings Data { get; }
        event Action<GameSettings> Changed;

        void Load();
        void Save();
        void ResetToDefault();
        void SetLanguage(string language);
        void SetMasterVolume(float volume);
        void SetBgmVolume(float volume);
        void SetSfxVolume(float volume);
        void SetMuted(bool muted);
    }

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
