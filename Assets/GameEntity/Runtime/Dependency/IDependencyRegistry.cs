using System;

namespace GE
{
    /// <summary>
    /// 依赖注册表接口，负责管理组件的依赖关系
    /// </summary>
    public interface IDependencyRegistry
    {
        /// <summary>
        /// 注册依赖组件
        /// </summary>
        /// <param name="component">依赖组件</param>
        /// <param name="dependencies">依赖类型数组</param>
        void RegisterDependentComponent(Entity component, Type[] dependencies);
        
        /// <summary>
        /// 取消注册依赖组件
        /// </summary>
        /// <param name="component">依赖组件</param>
        void UnregisterDependentComponent(Entity component);
        
        /// <summary>
        /// 通知组件变更
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="componentType">组件类型</param>
        /// <param name="isAdded">是添加还是移除</param>
        void NotifyComponentChanged(Entity entity, Type componentType, bool isAdded);
    }
} 