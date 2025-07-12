using System;

namespace GE
{
    /// <summary>
    /// 声明组件依赖关系的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        /// <summary>
        /// 依赖的组件类型
        /// </summary>
        public Type[] DependencyTypes { get; }

        /// <summary>
        /// 声明组件依赖
        /// </summary>
        /// <param name="dependencyTypes">依赖的组件类型列表</param>
        public DependsOnAttribute(params Type[] dependencyTypes)
        {
            DependencyTypes = dependencyTypes ?? new Type[0];
        }
    }
} 