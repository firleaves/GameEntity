using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    /// <summary>
    /// 依赖注册表，管理组件间的依赖关系
    /// </summary>
    internal sealed class DependencyRegistry : IDependencyRegistry
    {

        private Dictionary<Type, HashSet<Entity>> _dependencyDict = new Dictionary<Type, HashSet<Entity>>();


        private Dictionary<Entity, Type[]> _componentDependencies = new Dictionary<Entity, Type[]>();


        public delegate void ComponentChangeHandler(Entity entity, Type componentType);


        public event ComponentChangeHandler OnComponentAdded;
        public event ComponentChangeHandler OnComponentRemoved;

        public void Clear()
        {
            _dependencyDict.Clear();
            _componentDependencies.Clear();
            OnComponentAdded = null;
            OnComponentRemoved = null;
        }

        /// <summary>
        /// 注册依赖组件及其依赖类型
        /// </summary>
        public void RegisterDependentComponent(Entity component, Type[] dependencies)
        {
            if (component == null || dependencies == null || dependencies.Length == 0)
                return;

            _componentDependencies[component] = dependencies;

            foreach (var dependencyType in dependencies)
            {
                if (!_dependencyDict.TryGetValue(dependencyType, out var components))
                {
                    components = new HashSet<Entity>();
                    _dependencyDict[dependencyType] = components;
                }

                components.Add(component);
            }

            // 初始检查依赖状态
            RefreshDependencyStatus(component);
        }

        /// <summary>
        /// 取消注册依赖组件
        /// </summary>
        public void UnregisterDependentComponent(Entity component)
        {
            if (component == null || !_componentDependencies.TryGetValue(component, out var dependencies))
                return;

            foreach (var dependencyType in dependencies)
            {
                if (_dependencyDict.TryGetValue(dependencyType, out var components))
                {
                    components.Remove(component);

                    if (components.Count == 0)
                    {
                        _dependencyDict.Remove(dependencyType);
                    }
                }
            }

            _componentDependencies.Remove(component);
        }

        /// <summary>
        /// 通知组件变更
        /// </summary>
        public void NotifyComponentChanged(Entity entity, Type componentType, bool isAdded)
        {
            if (entity == null || componentType == null)
                return;

            if (_dependencyDict.TryGetValue(componentType, out var dependentComponents))
            {
                // 创建一个副本，防止在遍历过程中集合被修改
                var components = dependentComponents.ToArray();
                foreach (var component in components)
                {
                    if (component.Parent == entity) // 确保组件属于同一个实体
                    {
                        RefreshDependencyStatus(component);
                    }
                }
            }
        }

        /// <summary>
        /// 检查组件的依赖状态
        /// </summary>
        public bool RefreshDependencyStatus(Entity component)
        {
            if (component == null)
                return false;

            if (!_componentDependencies.TryGetValue(component, out var dependencies))
            {
                if (component is IDependentComponent dependentComponentWithoutRegistry)
                {
                    dependencies = dependentComponentWithoutRegistry.GetDependencyTypes();
                    if (dependencies == null || dependencies.Length == 0)
                    {
                        dependentComponentWithoutRegistry.OnDependencyStatusChanged(true);
                        return true;
                    }
                }

                return true;
            }

            bool allMet = true;
            var parent = component.Parent;

            if (parent == null)
            {
                allMet = false;
            }

            if (parent != null)
            {
                foreach (var dependencyType in dependencies)
                {
                    Entity dependency = parent.GetComponent(dependencyType);
                    if (!IsComponentReady(dependency))
                    {
                        allMet = false;
                        break;
                    }
                }
            }

            if (component is IDependentComponent dependentComponent)
            {
                dependentComponent.OnDependencyStatusChanged(allMet);
            }

            return allMet;
        }

        private static bool IsComponentReady(Entity component)
        {
            if (component == null)
            {
                return false;
            }

            return component is not IEntityLifecycleGate gate || gate.IsReady;
        }


        public void NotifyAddComponent(Entity entity, Type componentType)
        {
            NotifyComponentChanged(entity, componentType, true);
            OnComponentAdded?.Invoke(entity, componentType);
        }

        public void NotifyRemoveComponent(Entity entity, Type componentType)
        {
            NotifyComponentChanged(entity, componentType, false);
            OnComponentRemoved?.Invoke(entity, componentType);
        }
    }
}
