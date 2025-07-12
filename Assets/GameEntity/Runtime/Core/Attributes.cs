using System;

namespace GE
{
    /// <summary>
    /// 子实体的父级实体类型约束
    /// 父级实体类型唯一的 标记指定父级实体类型[ChildOf(typeof(parentType)]
    /// 不唯一则标记[ChildOf]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ChildOfAttribute : Attribute
    {
        public Type Type { get; private set; }

        public ChildOfAttribute(Type type = null)
        {
            Type = type;
        }
    }

    // 标记一个实体可以作为另一个实体的组件
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentOfAttribute : Attribute
    {
        public Type ComponentType { get; private set;}

        public ComponentOfAttribute(Type componentType)
        {
            ComponentType = componentType;
        }
    }
}