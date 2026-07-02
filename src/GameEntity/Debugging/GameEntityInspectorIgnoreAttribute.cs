using System;

namespace GameEntity
{
    /// <summary>
    /// 标记字段或属性不在调试 Inspector 中显示。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class GameEntityInspectorIgnoreAttribute : Attribute
    {
        public GameEntityInspectorIgnoreAttribute()
        {
        }

        public GameEntityInspectorIgnoreAttribute(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
