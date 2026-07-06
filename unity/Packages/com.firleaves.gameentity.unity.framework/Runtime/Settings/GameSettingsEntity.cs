using System;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class GameSettingsEntity : Entity, IAwake<IAudioSystem>, IDestroy, IGameSettings
    {
        private const string PlayerPrefsKey = "GameEntity.Unity.Framework.Settings";
        private IAudioSystem _audioSystem;

        public GameSettings Data { get; private set; } = new GameSettings();
        public event Action<GameSettings> Changed;

        public void Awake(IAudioSystem audioSystem)
        {
            _audioSystem = audioSystem;
            Load();
            ApplyAudioSettings();
        }

        public void OnDestroy()
        {
            Save();
            _audioSystem = null;
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                Data = new GameSettings();
                return;
            }

            var json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            Data = string.IsNullOrWhiteSpace(json) ? new GameSettings() : JsonUtility.FromJson<GameSettings>(json);
            if (Data == null)
            {
                Data = new GameSettings();
            }
        }

        public void Save()
        {
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public void ResetToDefault()
        {
            Data = new GameSettings();
            SaveAndNotify();
        }

        public void SetLanguage(string language)
        {
            Data.Language = string.IsNullOrWhiteSpace(language) ? "zh-CN" : language;
            SaveAndNotify();
        }

        public void SetMasterVolume(float volume)
        {
            Data.MasterVolume = Mathf.Clamp01(volume);
            SaveAndNotify();
        }

        public void SetBgmVolume(float volume)
        {
            Data.BgmVolume = Mathf.Clamp01(volume);
            SaveAndNotify();
        }

        public void SetSfxVolume(float volume)
        {
            Data.SfxVolume = Mathf.Clamp01(volume);
            SaveAndNotify();
        }

        public void SetMuted(bool muted)
        {
            Data.Muted = muted;
            SaveAndNotify();
        }

        private void SaveAndNotify()
        {
            ApplyAudioSettings();
            Save();
            Changed?.Invoke(Data.Clone());
        }

        private void ApplyAudioSettings()
        {
            if (_audioSystem == null || Data == null)
            {
                return;
            }

            _audioSystem.SetMuted(Data.Muted);
            _audioSystem.SetMasterVolume(Data.MasterVolume);
            _audioSystem.SetBgmVolume(Data.BgmVolume);
            _audioSystem.SetSfxVolume(Data.SfxVolume);
        }
    }
}
