namespace GameEntity
{
    public enum EntityValidationSeverity
    {
        Warning = 0,
        Error = 1,
    }

    /// <summary>
    /// EntityHierarchy 结构校验问题。
    /// </summary>
    public sealed class EntityValidationIssue
    {
        private EntityValidationIssue(int nodeId, string code, string message, EntityValidationSeverity severity)
        {
            NodeId = nodeId;
            Code = code;
            Message = message;
            Severity = severity;
        }

        public int NodeId { get; }

        public string Code { get; }

        public string Message { get; }

        public EntityValidationSeverity Severity { get; }

        public static EntityValidationIssue Error(int nodeId, string code, string message)
        {
            return new EntityValidationIssue(nodeId, code, message, EntityValidationSeverity.Error);
        }

        public static EntityValidationIssue Warning(int nodeId, string code, string message)
        {
            return new EntityValidationIssue(nodeId, code, message, EntityValidationSeverity.Warning);
        }
    }
}
