using System;
using System.Collections.Generic;
using System.Linq;

namespace GE
{
    /// <summary>
    /// 依赖注册表，管理组件间的依赖关系
    /// </summary>
    public class DependencyRegistry : Singleton<DependencyRegistry>, ISingletonAwake, IDependencyRegistry
    {

        private Dictionary<Type, HashSet<Entity>> _dependencyDict = new Dictionary<Type, HashSet<Entity>>();


        private Dictionary<Entity, Type[]> _componentDependencies = new Dictionary<Entity, Type[]>();


        public delegate void ComponentChangeHandler(Entity entity, Type componentType);


        public event ComponentChangeHandler OnComponentAdded;
        public event ComponentChangeHandler OnComponentRemoved;

        public void Awake()
        {

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _dependencyDict.Clear();
            _componentDependencies.Clear();
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
            CheckDependenciesForComponent(component);
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
                        CheckDependenciesForComponent(component);
                    }
                }
            }
        }

        /// <summary>
        /// 检查组件的依赖状态
        /// </summary>
        private void CheckDependenciesForComponent(Entity component)
        {
            if (!_componentDependencies.TryGetValue(component, out var dependencies))
                return;

            var allMet = true;
            var parent = component.Parent;

            if (parent == null)
                return;

            foreach (var dependencyType in dependencies)
            {
                if (parent.GetComponent(dependencyType) == null)
                {
                    allMet = false;
                    break;
                }
            }

            if (component is IDependentComponent dependentComponent)
            {
                dependentComponent.OnDependencyStatusChanged(allMet);
            }
        }


        public void NotifyAddComponent(Entity entity, Type componentType)
        {
            OnComponentAdded?.Invoke(entity, componentType);
        }

        public void NotifyRemoveComponent(Entity entity, Type componentType)
        {
            OnComponentRemoved?.Invoke(entity, componentType);
        }
    }
}