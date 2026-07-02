using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    /// <summary>
    /// EntityHierarchy 结构校验结果。
    /// </summary>
    public sealed class EntityValidationResult
    {
        public EntityValidationResult(IReadOnlyList<EntityValidationIssue> issues)
        {
            Issues = issues ?? new List<EntityValidationIssue>();
        }

        public IReadOnlyList<EntityValidationIssue> Issues { get; }

        public bool IsValid => Issues.All(issue => issue.Severity != EntityValidationSeverity.Error);
    }
}
