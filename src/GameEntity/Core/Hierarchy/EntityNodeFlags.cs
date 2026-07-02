using System;

namespace GameEntity
{
    [Flags]
    internal enum EntityNodeFlags
    {
        None = 0,
        Alive = 1,
        Destroying = 1 << 1,
    }
}
