using System;

namespace GameEntity
{
    [Flags]
    internal enum NodeFlags
    {
        None = 0,
        Alive = 1,
        Disposing = 1 << 1,
    }
}
