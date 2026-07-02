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

        public void Destroy(Entity entity)
        {
            try
            {
                _world.Hierarchy.Scheduler.Unregister(entity);
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
