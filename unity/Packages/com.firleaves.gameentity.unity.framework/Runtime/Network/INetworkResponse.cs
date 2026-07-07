using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public interface INetworkResponse : INetworkPacket
    {
        int RpcId { get; set; }
        int ErrorCode { get; set; }
        string ErrorMessage { get; set; }
    }

}
