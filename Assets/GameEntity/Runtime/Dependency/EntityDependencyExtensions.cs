using System;
using System.Linq;

namespace GE
{
    /// <summary>
    /// Entity依赖系统扩展方法
    /// </summary>
    public static class EntityDependencyExtensions
    {
        /// <summary>
        /// 初始化依赖系统
        /// </summary>
        internal static void InitializeDependencySystem(this World world)
        {
            var registry = world.AddSingleton<DependencyRegistry>();

            // 注册组件变更事件处理器
            registry.OnComponentAdded += OnComponentAdded;
            registry.OnComponentRemoved += OnComponentRemoved;

        }

        // 组件添加事件处理
        private static void OnComponentAdded(Entity entity, Type componentType)
        {
            DependencyRegistry.Instance?.NotifyComponentChanged(entity, componentType, true);
        }

        // 组件移除事件处理
        private static void OnComponentRemoved(Entity entity, Type componentType)
        {
            DependencyRegistry.Instance?.NotifyComponentChanged(entity, componentType, false);
        }

        /// <summary>
        /// 检查组件是否满足依赖
        /// </summary>
        public static bool AreDependenciesMet<T>(this Entity entity) where T : Entity
        {
            var component = entity.GetComponent<T>();
            if (component == null) return false;

            return component is IDependentComponent dependentComponent
                ? dependentComponent.AreAllDependenciesMet
                : true; // 非依赖组件默认满足
        }



        internal static void ProcessComponentDependencies(this Entity component)
        {
            if (component == null)
                return;

            var registry = DependencyRegistry.Instance;
            if (registry == null)
                return;

            // 处理接口依赖
            if (component is IDependentComponent dependentComponent)
            {
                var dependencyTypes = dependentComponent.GetDependencyTypes();
                if (dependencyTypes != null && dependencyTypes.Length > 0)
                {
                    registry.RegisterDependentComponent(component, dependencyTypes);
                }
                return;
            }

            // 处理特性依赖
            var attributes = component.GetType().GetCustomAttributes(typeof(DependsOnAttribute), true);
            if (attributes.Length > 0)
            {
                var dependencyTypes = attributes
                    .Cast<DependsOnAttribute>()
                    .SelectMany(attr => attr.DependencyTypes)
                    .Distinct()
                    .ToArray();

                if (dependencyTypes.Length > 0)
                {
                    registry.RegisterDependentComponent(component, dependencyTypes);
                }
            }

        }
    }
}