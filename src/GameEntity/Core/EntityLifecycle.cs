using System;

namespace GameEntity
{
    internal sealed class EntityLifecycle
    {
        private readonly World _world;

        public EntityLifecycle(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public void Awake(Entity entity)
        {
            if (entity is IAwake awakable)
            {
                awakable.Awake();
            }
        }

        public void Awake<P1>(Entity entity, P1 p1)
        {
            if (entity is IAwake<P1> awakable)
            {
                awakable.Awake(p1);
            }
        }

        public void Awake<P1, P2>(Entity entity, P1 p1, P2 p2)
        {
            if (entity is IAwake<P1, P2> awakable)
            {
                awakable.Awake(p1, p2);
            }
        }

        public void Awake<P1, P2, P3>(Entity entity, P1 p1, P2 p2, P3 p3)
        {
            if (entity is IAwake<P1, P2, P3> awakable)
            {
                awakable.Awake(p1, p2, p3);
            }
        }

        public void Awake<P1, P2, P3, P4>(Entity entity, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            if (entity is IAwake<P1, P2, P3, P4> awakable)
            {
                awakable.Awake(p1, p2, p3, p4);
            }
        }

        public void Destroy(Entity entity)
        {
            try
            {
                _world.Hierarchy.Scheduler.Unregister(entity);
            }
            catch (Exception e)
            {
                Log.Error($"Destroy scheduler unregister error: {e}");
            }

            if (entity is not IDestroy destroyable)
            {
                return;
            }

            try
            {
                destroyable.OnDestroy();
            }
            catch (Exception e)
            {
                Log.Error($"Destroy callback error: {e}");
            }
        }
    }
}
