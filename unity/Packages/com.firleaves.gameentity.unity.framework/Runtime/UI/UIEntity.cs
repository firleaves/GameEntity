using Cysharp.Threading.Tasks;
using GameEntity;

namespace GameEntity.Unity.Framework
{
    public abstract class UIEntity : Entity, IAwake, IDestroy
    {
        private IUIView _view;
        private UIOpenParams _openParams;

        internal UISystemEntity UISystem { get; private set; }

        public string UIName => GetType().Name;
        public string Group { get; private set; }
        public int Depth { get; private set; }
        public IUIView View => _view;
        public AssetKey ViewKey { get; private set; }

        public virtual void Awake()
        {
        }

        public virtual void OnDestroy()
        {
            if (_view != null && UISystem != null)
            {
                UISystem.ReleaseView(this, _view);
            }

            _view = null;
            _openParams = null;
            UISystem = null;
        }

        protected virtual AssetKey GetDefaultViewKey()
        {
            return default;
        }

        protected virtual void OnBindView(IUIView view)
        {
        }

        protected virtual UniTask OnOpenAsync(UIOpenContext context)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnCloseAsync(UICloseContext context)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnCover()
        {
        }

        protected virtual void OnReveal()
        {
        }

        protected virtual void OnRefocus()
        {
        }

        protected virtual void OnDepthChanged(int depth)
        {
        }

        internal AssetKey ResolveViewKey(UIOpenParams openParams)
        {
            if (openParams != null && openParams.ViewKey.IsValid)
            {
                return openParams.ViewKey;
            }

            var key = GetDefaultViewKey();
            if (!key.IsValid)
            {
                throw new FrameworkException($"{UIName} 没有提供 ViewKey。");
            }

            return key;
        }

        internal void BindView(IUIView view, AssetKey viewKey, UIOpenParams openParams, UISystemEntity owner)
        {
            _view = view ?? throw new FrameworkException($"{UIName} 绑定的 IUIView 不能为空。");
            ViewKey = viewKey;
            _openParams = openParams;
            UISystem = owner;
            OnBindView(view);
        }

        internal UniTask InvokeOpenAsync(UIOpenParams openParams)
        {
            return OnOpenAsync(new UIOpenContext(openParams));
        }

        internal UniTask InvokeCloseAsync(UICloseReason reason)
        {
            return OnCloseAsync(new UICloseContext(reason));
        }

        internal void InvokeCover()
        {
            OnCover();
        }

        internal void InvokeReveal()
        {
            OnReveal();
        }

        internal void InvokeRefocus()
        {
            OnRefocus();
        }

        internal void SetGroupAndDepth(string group, int depth)
        {
            Group = group;
            Depth = depth;
            _view?.SetDepth(depth);
            OnDepthChanged(depth);
        }

        internal void ClearView()
        {
            _view = null;
            _openParams = null;
            UISystem = null;
        }
    }
}
