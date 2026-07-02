using System;

namespace GameEntity
{
    public struct EntityRef<T> where T : Entity
    {
        private readonly long _instanceId;
        private readonly EntityHandle _handle;
        private T _entity;

        private EntityRef(T t)
        {
            if (t == null)
            {
                this._instanceId = 0;
                this._handle = EntityHandle.None;
                this._entity = null;
                return;
            }
            this._instanceId = t.InstanceId;
            this._handle = t.Handle;
            this._entity = t;
        }

        private T UnWrap
        {
            get
            {
                if (TryGet(out var entity))
                {
                    return entity;
                }

                return null;
            }
        }

        public bool IsAlive => TryGet(out _);

        public EntityHandle Handle => _handle;

        public T ValueOrNull => UnWrap;

        public bool TryGet(out T entity)
        {
            if (this._entity != null &&
                this._entity.InstanceId == this._instanceId &&
                this._entity.Handle == this._handle)
            {
                entity = this._entity;
                return true;
            }

            this._entity = null;
            if (!_handle.IsValid || _instanceId == 0)
            {
                entity = null;
                return false;
            }

            if (World.Instance.TryResolve(_handle, out T resolved) && resolved.InstanceId == _instanceId)
            {
                this._entity = resolved;
                entity = resolved;
                return true;
            }

            entity = null;
            return false;
        }

        public static implicit operator EntityRef<T>(T t)
        {
            return new EntityRef<T>(t);
        }

        public static implicit operator T(EntityRef<T> v)
        {
            return v.UnWrap;
        }
    }

}
