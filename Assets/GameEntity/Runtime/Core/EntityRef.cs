using System;

namespace GE
{
    public struct EntityRef<T> where T : Entity
    {
        private readonly long _instanceId;
        private T _entity;

        private EntityRef(T t)
        {
            if (t == null)
            {
                this._instanceId = 0;
                this._entity = null;
                return;
            }
            this._instanceId = t.InstanceId;
            this._entity = t;
        }

        private T UnWrap
        {
            get
            {
                if (this._entity == null)
                {
                    return null;
                }
                if (this._entity.InstanceId != this._instanceId)
                {
                    // 这里instanceId变化了，设置为null，解除引用，好让runtime去gc
                    this._entity = null;
                }
                return this._entity;
            }
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