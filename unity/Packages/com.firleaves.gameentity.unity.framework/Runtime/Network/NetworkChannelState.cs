using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public enum NetworkChannelState
    {
        Closed,
        Connecting,
        Connected,
        Closing
    }

}
