using System;
using System.Linq;

namespace GE
{
    /// <summary>
    /// 依赖组件基类，提供IDependentComponent接口的默认实现
    /// </summary>
    public abstract class DependentComponentBase : Entity, IDependentComponent
    {
        /// <summary>
        /// 是否所有依赖都满足
        /// </summary>
        public bool AreAllDependenciesMet { get; private set; }
        
        /// <summary>
        /// 获取依赖类型数组，默认从DependsOn特性获取
        /// </summary>
        public virtual Type[] GetDependencyTypes()
        {
            return GetType().GetCustomAttributes(typeof(DependsOnAttribute), true)
                .Cast<DependsOnAttribute>()
                .SelectMany(attr => attr.DependencyTypes)
                .Distinct()
                .ToArray();
        }
        
        /// <summary>
        /// 依赖状态变更通知
        /// </summary>
        public void OnDependencyStatusChanged(bool areAllDependenciesMet)
        {
            if (AreAllDependenciesMet == areAllDependenciesMet)
                return;
                
            AreAllDependenciesMet = areAllDependenciesMet;
            OnActivationChanged(areAllDependenciesMet);
        }
        
        /// <summary>
        /// 激活状态变更回调，子类可覆盖
        /// </summary>
        /// <param name="isActive">是否激活</param>
        protected virtual void OnActivationChanged(bool isActive) { }
    }
} 