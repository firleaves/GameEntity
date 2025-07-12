using System;

namespace GE
{
    /// <summary>
    /// 依赖组件接口，实现此接口的组件将参与依赖系统
    /// </summary>
    public interface IDependentComponent
    {
        /// <summary>
        /// 获取依赖的组件类型
        /// </summary>
        Type[] GetDependencyTypes();
        
        /// <summary>
        /// 依赖状态变更通知
        /// </summary>
        /// <param name="areAllDependenciesMet">是否所有依赖都满足</param>
        void OnDependencyStatusChanged(bool areAllDependenciesMet);
        
        /// <summary>
        /// 当前组件的依赖是否都满足
        /// </summary>
        bool AreAllDependenciesMet { get; }
    }
} 