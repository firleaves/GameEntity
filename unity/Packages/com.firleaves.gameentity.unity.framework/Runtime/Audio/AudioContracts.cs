using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public enum AudioChannel
    {
        Bgm,
        Sfx,
        Voice,
        Ambience
    }

    public sealed class AudioPlayOptions
    {
        public AudioChannel Channel = AudioChannel.Sfx;
        public float Volume = 1f;
        public bool Loop;
        public bool IgnoreMute;
        public Transform Parent;
        public Vector3? Position;
    }

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

    public readonly struct AudioPlayHandle : IEquatable<AudioPlayHandle>
    {
        internal readonly int Id;

        internal AudioPlayHandle(int id)
        {
            Id = id;
        }

        public bool IsValid => Id > 0;

        public bool Equals(AudioPlayHandle other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioPlayHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
