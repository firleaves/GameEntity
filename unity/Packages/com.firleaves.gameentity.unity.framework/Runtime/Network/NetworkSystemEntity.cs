using System;
using System.Collections.Generic;
using GameEntity;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkSystemEntity : Entity, IAwake<NetworkOptions>, IUpdate, IDestroy, INetworkSystem
    {
        private readonly Dictionary<string, NetworkChannel> _channels = new Dictionary<string, NetworkChannel>(StringComparer.Ordinal);
        private NetworkOptions _options;

        public void Awake(NetworkOptions options)
        {
            _options = options != null ? options.Clone() : NetworkOptions.CreateDefault();
        }

        public void Update(float deltaTime)
        {
            if (_channels.Count == 0)
            {
                return;
            }

            foreach (var channel in _channels.Values)
            {
                channel.Tick(deltaTime);
            }
        }

        public void OnDestroy()
        {
            CloseAll();
        }

        public INetworkChannel CreateChannel(string name, NetworkChannelOptions options)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new FrameworkException("创建网络频道失败：name 不能为空。");
            }

            if (_channels.ContainsKey(name))
            {
                throw new FrameworkException($"创建网络频道失败：频道已存在：{name}");
            }

            if (_options.MaxChannelCount > 0 && _channels.Count >= _options.MaxChannelCount)
            {
                throw new FrameworkException($"创建网络频道失败：频道数量已达到上限：{_options.MaxChannelCount}");
            }

            var runtimeOptions = (options ?? new NetworkChannelOptions()).ToRuntimeOptions(_options);
            if (runtimeOptions.Protocol == null)
            {
                throw new FrameworkException("创建网络频道失败：Protocol 不能为空。");
            }

            var channel = new NetworkChannel(name, runtimeOptions);
            _channels.Add(name, channel);
            return channel;
        }

        public bool TryGetChannel(string name, out INetworkChannel channel)
        {
            if (!string.IsNullOrWhiteSpace(name) && _channels.TryGetValue(name, out var concrete))
            {
                channel = concrete;
                return true;
            }

            channel = null;
            return false;
        }

        public bool DestroyChannel(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !_channels.Remove(name, out var channel))
            {
                return false;
            }

            channel.Shutdown();
            return true;
        }

        public void CloseAll()
        {
            foreach (var channel in _channels.Values)
            {
                channel.Shutdown();
            }

            _channels.Clear();
        }

        public NetworkSystemSnapshot GetSnapshot()
        {
            var channels = new List<NetworkChannelSnapshot>(_channels.Count);
            foreach (var channel in _channels.Values)
            {
                channels.Add(channel.GetSnapshot());
            }

            return new NetworkSystemSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow,
                ChannelCount = channels.Count,
                Channels = channels
            };
        }
    }
}
