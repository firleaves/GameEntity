using System;

namespace GameEntity
{
    /// <summary>
    /// 声明当前 Component 进入 Start、FixedUpdate 和 Update 前必须存在且就绪的同 owner Component。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class RequireForUpdateAttribute : Attribute
    {
        public RequireForUpdateAttribute(params Type[] componentTypes)
        {
            RequiredComponentTypes = componentTypes ?? Array.Empty<Type>();
        }

        public Type[] RequiredComponentTypes { get; }
    }
}
