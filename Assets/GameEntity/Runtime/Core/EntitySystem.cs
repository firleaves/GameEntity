using System;
using System.Collections.Generic;

namespace GE
{

    /// <summary>
    /// 实体队列索引，用于标识不同类型的实体队列
    /// </summary>
    internal static class InstanceQueueIndex
    {
        public const int None = 0;
        public const int Update = 1;
        public const int LateUpdate = 2;

        // 可以根据需要添加更多队列类型

        public const int Max = 3;
    }

    internal class EntitySystem : Singleton<EntitySystem>, ISingletonAwake
    {

        private readonly Queue<EntityRef<Entity>>[] _queues = new Queue<EntityRef<Entity>>[InstanceQueueIndex.Max];
        private readonly Dictionary<int, IUpdateStrategy> _updateStrategies = new Dictionary<int, IUpdateStrategy>();
        public void Awake()
        {
            for (int i = 0; i < _queues.Length; i++)
            {
                _queues[i] = new Queue<EntityRef<Entity>>();
            }
        }


        public void SetUpdateStrategy(int queueIndex, IUpdateStrategy strategy)
        {
            _updateStrategies[queueIndex] = strategy;
        }



        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var queue in _queues)
            {
                int count = queue.Count;
                while (count-- > 0)
                {
                    Entity entity = queue.Dequeue();

                    if (entity == null) continue;
                    if (entity.IsDisposed) continue;

                    Destroy(entity);
                }
                queue.Clear();
            }
        }

        
        public void TryRegisterUpdate(Entity entity)
        {
            RegisterUpdate(entity);
            RegisterLateUpdate(entity);
        }

        /// <summary>
        /// 注册实体到更新列表
        /// </summary>
        private void RegisterUpdate(Entity entity)
        {
            if (entity is IUpdate)
            {
                _queues[InstanceQueueIndex.Update].Enqueue(entity);
            }
        }

        private void RegisterLateUpdate(Entity entity)
        {
            if (entity is ILateUpdate)
            {
                _queues[InstanceQueueIndex.LateUpdate].Enqueue(entity);
            }
        }


        private void UnregisterUpdate(Entity entity)
        {
            _queues[InstanceQueueIndex.Update].Enqueue(entity);
        }

        private void UnregisterLateUpdate(Entity entity)
        {
            _queues[InstanceQueueIndex.LateUpdate].Enqueue(entity);
        }


        public void Update(float deltaTime, float unscaledDeltaTime)
        {

            var queue = _queues[InstanceQueueIndex.Update];
            int count = queue.Count;
            while (count-- > 0)
            {
                Entity entity = queue.Dequeue();

                if (entity == null) continue;
                if (entity.IsDisposed) continue;

                if (entity is IDependentComponent dependentComponent && !dependentComponent.AreAllDependenciesMet) continue;

                if (entity is not IUpdate updateableEntity) continue;

                queue.Enqueue(entity);


                IUpdateStrategy strategy = null;
                if (entity is IHasUpdateStrategy hasStrategy)
                {
                    strategy = hasStrategy.GetUpdateStrategy();
                }
                if (strategy == null && _updateStrategies.TryGetValue(InstanceQueueIndex.Update, out var globalStrategy))
                {
                    strategy = globalStrategy;
                }

                if (strategy == null)
                {
                    try
                    {
                        updateableEntity.Update(unscaledDeltaTime);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Update error: {e}");
                    }
                }
                else
                {
                    int updateCount = strategy.GetUpdateCount(entity, deltaTime, unscaledDeltaTime, out float singleDeltaTime);
                    for (int i = 0; i < updateCount; i++)
                    {
                        try
                        {
                            updateableEntity.Update(singleDeltaTime);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Update error: {e}");
                        }
                    }
                }
            }
        }


        public void LateUpdate()
        {

            var queue = _queues[InstanceQueueIndex.LateUpdate];
            int count = queue.Count;
            while (count-- > 0)
            {
                Entity entity = queue.Dequeue();

                if (entity == null) continue;
                if (entity.IsDisposed) continue;
                if (entity is not ILateUpdate lateUpdateableEntity) continue;
                queue.Enqueue(entity);
                try
                {
                    lateUpdateableEntity.LateUpdate();
                }
                catch (Exception e)
                {
                    Log.Error($"LateUpdate error: {e}");
                }
            }
        }




        /// <summary>
        /// 初始化实体
        /// </summary>
        public void Awake(Entity entity)
        {
            try
            {
                if (entity is IAwake awakable)
                {
                    awakable.Awake();
                }

               
            }
            catch (Exception e)
            {
                Log.Error($"Awake error: {e}");
            }
        }

        /// <summary>
        /// 初始化实体（带参数）
        /// </summary>
        public void Awake<P1>(Entity entity, P1 p1)
        {
            try
            {
                if (entity is IAwake<P1> awakable)
                {
                    awakable.Awake(p1);
                }

                
            }
            catch (Exception e)
            {
                Log.Error($"Awake error: {e}");
            }
        }

        /// <summary>
        /// 初始化实体（带两个参数）
        /// </summary>
        public void Awake<P1, P2>(Entity entity, P1 p1, P2 p2)
        {
            try
            {
                if (entity is IAwake<P1, P2> awakable)
                {
                    awakable.Awake(p1, p2);
                }

              
            }
            catch (Exception e)
            {
                Log.Error($"Awake error: {e}");
            }
        }

        /// <summary>   
        /// 初始化实体（带三个参数）
        /// </summary>
        public void Awake<P1, P2, P3>(Entity entity, P1 p1, P2 p2, P3 p3)
        {
            try
            {
                if (entity is IAwake<P1, P2, P3> awakable)
                {
                    awakable.Awake(p1, p2, p3);
                }

                
            }
            catch (Exception e)
            {
                Log.Error($"Awake error: {e}");
            }
        }

        /// <summary>
        /// 初始化实体（带四个参数）
        /// </summary>
        public void Awake<P1, P2, P3, P4>(Entity entity, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            try
            {
                if (entity is IAwake<P1, P2, P3, P4> awakable)
                {
                    awakable.Awake(p1, p2, p3, p4);
                }

              
            }
            catch (Exception e)
            {
                Log.Error($"Awake error: {e}");
            }
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public void Destroy(Entity entity)
        {
            try
            {
                UnregisterUpdate(entity);
                UnregisterLateUpdate(entity);
                if (entity is IDestroy destroyable)
                {
                    destroyable.OnDestroy();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Destroy error: {e}");
            }
        }


    }
}