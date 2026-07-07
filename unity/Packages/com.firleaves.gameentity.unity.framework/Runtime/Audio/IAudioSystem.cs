using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public interface IAudioSystem
    {
        bool Muted { get; }
        float MasterVolume { get; }
        float BgmVolume { get; }
        float SfxVolume { get; }

        void SetMuted(bool muted);
        void SetMasterVolume(float volume);
        void SetBgmVolume(float volume);
        void SetSfxVolume(float volume);

        UniTask<AudioPlayHandle> PlayBgmAsync(
            string location,
            string packageName = null,
            float volume = 1f,
            CancellationToken ct = default);

        UniTask<AudioPlayHandle> PlaySfxAsync(
            string location,
            string packageName = null,
            AudioPlayOptions options = null,
            CancellationToken ct = default);

        void Stop(AudioPlayHandle handle);
        void StopBgm();
        void StopAll();
    }

}
