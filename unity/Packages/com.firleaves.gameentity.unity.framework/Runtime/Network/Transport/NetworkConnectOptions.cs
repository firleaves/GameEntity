using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public sealed class NetworkConnectOptions
    {
        public string Address;
        public string Host;
        public int Port;
        public int ReceiveBufferSize;
        public int SendBufferSize;
    }

}
