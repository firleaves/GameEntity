using System;
using System.Linq;

namespace GameEntity
{
    /// <summary>
    /// Entity 依赖系统扩展方法。
    /// </summary>
    public static class EntityDependencyExtensions
    {
        /// <summary>
        /// 检查组件是否满足依赖。
        /// </summary>
        public static bool AreDependenciesMet<T>(this Entity entity) where T : Entity
        {
            var component = entity.GetComponent<T>();
            if (component == null)
            {
                return false;
            }

            if (component is IEntityLifecycleGate gate && !gate.IsReady)
            {
                return false;
            }

            if (component is IDependentComponent dependentComponent)
            {
                return component.GetWorld().Dependencies.RefreshDependencyStatus(component) &&
                       dependentComponent.AreAllDependenciesMet;
            }

            return true;
        }

        internal static void ProcessComponentDependencies(this Entity component, World world)
        {
            if (component == null)
            {
                return;
            }

            var registry = world.Dependencies;

            if (component is IDependentComponent dependentComponent)
            {
                RegisterDependencies(component, dependentComponent.GetDependencyTypes(), registry);
                return;
            }

            var attributes = component.GetType().GetCustomAttributes(typeof(DependsOnAttribute), true);
            if (attributes.Length == 0)
            {
                return;
            }

            var dependencyTypes = attributes
                .Cast<DependsOnAttribute>()
                .SelectMany(attr => attr.DependencyTypes)
                .Distinct()
                .ToArray();

            RegisterDependencies(component, dependencyTypes, registry);
        }

        private static void RegisterDependencies(Entity component, Type[] dependencyTypes, DependencyRegistry registry)
        {
            if (dependencyTypes == null || dependencyTypes.Length == 0)
            {
                if (component is IDependentComponent dependentComponent)
                {
                    dependentComponent.OnDependencyStatusChanged(true);
                }

                return;
            }

            registry.RegisterDependentComponent(component, dependencyTypes);
        }
    }
}
