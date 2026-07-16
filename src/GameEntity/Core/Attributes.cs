using System;

namespace GameEntity
{
    /// <summary>
    /// 声明 Entity 只能作为 Child 挂接；指定父类型时，owner 必须是该类型或其派生类型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ChildOfAttribute : Attribute
    {
        public Type Type { get; }

        public ChildOfAttribute(Type type = null)
        {
            Type = type;
        }
    }

}
