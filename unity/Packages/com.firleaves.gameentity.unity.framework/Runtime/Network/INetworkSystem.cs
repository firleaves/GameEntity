using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public interface INetworkSystem
    {
        void SetDefaultProtocol(INetworkProtocol protocol);
        INetworkChannel CreateTcpChannel(string name);
        INetworkChannel CreateTcpChannel(string name, NetworkChannelConfig config);
        INetworkChannel CreateMockChannel(string name);
        INetworkChannel CreateMockChannel(string name, NetworkChannelConfig config);
        INetworkChannel CreateChannel(string name, NetworkTransportKind transport, NetworkChannelConfig config);
        bool TryGetChannel(string name, out INetworkChannel channel);
        bool DestroyChannel(string name);
        void CloseAll();
        NetworkSystemSnapshot GetSnapshot();
    }

}
