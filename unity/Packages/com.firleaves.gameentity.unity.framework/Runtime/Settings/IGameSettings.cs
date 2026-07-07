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

}
