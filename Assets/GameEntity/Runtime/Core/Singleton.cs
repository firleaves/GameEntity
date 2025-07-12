using System;

namespace GE
{
    internal interface ISingletonReverseDispose
    {
        
    }
    
    public abstract class ASingleton: IDisposable
    {
        public abstract void Dispose();

        internal abstract void Register();
    }
    
    public abstract class Singleton<T>: ASingleton where T: Singleton<T>
    {
        protected bool _isDisposed;
        
        protected static T _instance;
        
        public static T Instance
        {
            get
            {
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        internal override void Register()
        {
            Instance = (T)this;
        }

        public bool IsDisposed()
        {
            return this._isDisposed;
        }

        protected virtual void OnDestroy()
        {
            
        }

        public override void Dispose()
        {
            if (this._isDisposed)
            {
                return;
            }
            
            this._isDisposed = true;

            this.OnDestroy();
            
            Instance = null;
        }
    }
}