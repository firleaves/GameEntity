using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class AudioSystemEntity : Entity, IAwake<IAssetPool, Transform>, IUpdate, IDestroy, IAudioSystem
    {
        private readonly Dictionary<int, AudioEntry> _playing = new Dictionary<int, AudioEntry>();
        private readonly Stack<AudioSource> _idleSources = new Stack<AudioSource>();
        private IAssetPool _assetPool;
        private Transform _root;
        private AudioEntry _bgm;
        private int _nextId;

        public bool Muted { get; private set; }
        public float MasterVolume { get; private set; } = 1f;
        public float BgmVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        public void Awake(IAssetPool assetPool, Transform root)
        {
            _assetPool = assetPool ?? throw new FrameworkException("AudioSystem 初始化失败：AssetPool 不能为空。");
            _root = root != null ? root : CreateAudioRoot();
        }

        public void Update(float deltaTime)
        {
            if (_playing.Count == 0)
            {
                return;
            }

            var stopped = new List<int>();
            foreach (var pair in _playing)
            {
                var entry = pair.Value;
                if (entry.Source == null || (!entry.Source.loop && !entry.Source.isPlaying))
                {
                    stopped.Add(pair.Key);
                }
            }

            for (var i = 0; i < stopped.Count; i++)
            {
                Stop(new AudioPlayHandle(stopped[i]));
            }
        }

        public void OnDestroy()
        {
            StopAll();
            while (_idleSources.Count > 0)
            {
                var source = _idleSources.Pop();
                if (source != null)
                {
                    Object.Destroy(source.gameObject);
                }
            }

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            _assetPool = null;
            _root = null;
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            ApplyVolumes();
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetBgmVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public async UniTask<AudioPlayHandle> PlayBgmAsync(
            string location,
            string packageName = null,
            float volume = 1f,
            CancellationToken ct = default)
        {
            StopBgm();
            var handle = await PlayInternalAsync(location, packageName, new AudioPlayOptions
            {
                Channel = AudioChannel.Bgm,
                Volume = volume,
                Loop = true
            }, ct);
            _bgm = _playing.TryGetValue(handle.Id, out var entry) ? entry : null;
            return handle;
        }

        public UniTask<AudioPlayHandle> PlaySfxAsync(
            string location,
            string packageName = null,
            AudioPlayOptions options = null,
            CancellationToken ct = default)
        {
            options = options ?? new AudioPlayOptions();
            options.Channel = options.Channel == AudioChannel.Bgm ? AudioChannel.Sfx : options.Channel;
            return PlayInternalAsync(location, packageName, options, ct);
        }

        public void Stop(AudioPlayHandle handle)
        {
            if (!handle.IsValid || !_playing.TryGetValue(handle.Id, out var entry))
            {
                return;
            }

            _playing.Remove(handle.Id);
            if (ReferenceEquals(_bgm, entry))
            {
                _bgm = null;
            }

            Recycle(entry);
        }

        public void StopBgm()
        {
            if (_bgm != null)
            {
                Stop(new AudioPlayHandle(_bgm.Id));
            }
        }

        public void StopAll()
        {
            var handles = new List<int>(_playing.Keys);
            for (var i = 0; i < handles.Count; i++)
            {
                Stop(new AudioPlayHandle(handles[i]));
            }
        }

        private async UniTask<AudioPlayHandle> PlayInternalAsync(
            string location,
            string packageName,
            AudioPlayOptions options,
            CancellationToken ct)
        {
            var clipRef = await _assetPool.LoadAsync<AudioClip>(location, packageName, ct: ct);
            var source = RentSource(options);
            source.clip = clipRef.Asset;
            source.loop = options.Loop;
            source.spatialBlend = options.Position.HasValue ? 1f : 0f;
            source.volume = ComputeVolume(options);
            source.mute = Muted && !options.IgnoreMute;
            source.Play();

            var id = ++_nextId;
            var entry = new AudioEntry(id, source, clipRef, options);
            _playing.Add(id, entry);
            return new AudioPlayHandle(id);
        }

        private AudioSource RentSource(AudioPlayOptions options)
        {
            var source = _idleSources.Count > 0 ? _idleSources.Pop() : CreateSource();
            var transform = source.transform;
            transform.SetParent(options.Parent != null ? options.Parent : _root, false);
            if (options.Position.HasValue)
            {
                transform.position = options.Position.Value;
            }

            source.gameObject.SetActive(true);
            return source;
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("AudioSource");
            go.transform.SetParent(_root, false);
            return go.AddComponent<AudioSource>();
        }

        private void Recycle(AudioEntry entry)
        {
            if (entry.Source != null)
            {
                entry.Source.Stop();
                entry.Source.clip = null;
                entry.Source.loop = false;
                entry.Source.transform.SetParent(_root, false);
                entry.Source.gameObject.SetActive(false);
                _idleSources.Push(entry.Source);
            }

            entry.ClipRef?.Release();
        }

        private float ComputeVolume(AudioPlayOptions options)
        {
            var channelVolume = options.Channel == AudioChannel.Bgm ? BgmVolume : SfxVolume;
            return Mathf.Clamp01(options.Volume) * MasterVolume * channelVolume;
        }

        private void ApplyVolumes()
        {
            foreach (var pair in _playing)
            {
                var entry = pair.Value;
                if (entry.Source == null)
                {
                    continue;
                }

                entry.Source.volume = ComputeVolume(entry.Options);
                entry.Source.mute = Muted && !entry.Options.IgnoreMute;
            }
        }

        private static Transform CreateAudioRoot()
        {
            var go = new GameObject("[GameEntity.Unity.Framework.Audio]");
            Object.DontDestroyOnLoad(go);
            return go.transform;
        }

        private sealed class AudioEntry
        {
            public AudioEntry(int id, AudioSource source, AssetRef<AudioClip> clipRef, AudioPlayOptions options)
            {
                Id = id;
                Source = source;
                ClipRef = clipRef;
                Options = options;
            }

            public int Id { get; }
            public AudioSource Source { get; }
            public AssetRef<AudioClip> ClipRef { get; }
            public AudioPlayOptions Options { get; }
        }
    }
}
