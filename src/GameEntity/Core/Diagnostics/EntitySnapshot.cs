using System.Collections.Generic;

namespace GameEntity
{
    /// <summary>
    /// EntityHierarchy 当前结构快照。快照是只读视图，不参与运行时结构修改。
    /// </summary>
    public sealed class EntitySnapshot
    {
        public EntitySnapshot(IReadOnlyList<EntityNodeInfo> nodes)
        {
            Nodes = nodes ?? new List<EntityNodeInfo>();
        }

        public IReadOnlyList<EntityNodeInfo> Nodes { get; }
    }
}
