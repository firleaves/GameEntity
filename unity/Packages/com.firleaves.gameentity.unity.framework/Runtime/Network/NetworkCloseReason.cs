using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public enum NetworkCloseReason
    {
        Local,
        Remote,
        Error,
        HeartbeatTimeout,
        Shutdown
    }

}
