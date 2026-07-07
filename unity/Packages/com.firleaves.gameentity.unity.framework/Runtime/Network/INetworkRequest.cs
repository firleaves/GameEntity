using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public interface INetworkRequest : INetworkPacket
    {
        int RpcId { get; set; }
    }

}
