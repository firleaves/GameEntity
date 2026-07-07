using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameEntity.Unity.Framework
{
    public sealed class UISystemEntity : Entity, IAwake<UISystemDependencies>, IDestroy, IUISystem
    {
        private readonly Dictionary<string, UIGroupEntity> _groups = new Dictionary<string, UIGroupEntity>(StringComparer.Ordinal);
        private readonly Dictionary<Type, UIEntity> _singleOpen = new Dictionary<Type, UIEntity>();
        private readonly List<UIEntity> _open = new List<UIEntity>();
        private readonly List<UIEntity> _closeBuffer = new List<UIEntity>();
        private UIOptions _options;
        private IUIAdapter _adapter;
        private Transform _root;

        public Transform Root => _root;

        public void Awake(UISystemDependencies dependencies)
        {
            if (dependencies == null)
            {
                throw new FrameworkException("UISystem 初始化参数不能为空。");
            }

            _options = dependencies.Options != null ? dependencies.Options.Clone() : UIOptions.CreateDefault();
            _root = EnsureUIRoot(_options, dependencies.FrameworkRoot);
            _adapter = new UGUIAdapter(dependencies.InstancePool);

            if (dependencies.AutoCreateEventSystem)
            {
                EnsureEventSystem();
            }
        }

        public void OnDestroy()
        {
            _groups.Clear();
            _singleOpen.Clear();
            _open.Clear();
            _closeBuffer.Clear();
            _adapter = null;
            _root = null;
            _options = null;
        }

        public async UniTask<TUI> OpenAsync<TUI>(UIOpenParams options = null, CancellationToken ct = default)
            where TUI : UIEntity, new()
        {
            options = NormalizeOpenParams(options);
            var type = typeof(TUI);
            if (options.ReusePolicy == UIReusePolicy.Single && _singleOpen.TryGetValue(type, out var existing) && existing != null && !existing.IsDestroyed)
            {
                GetOrCreateGroup(existing.Group).Refocus(existing);
                return (TUI)existing;
            }

            var ui = AddChild<TUI>();
            IUIView view = null;
            try
            {
                var viewKey = ui.ResolveViewKey(options);
                var group = GetOrCreateGroup(options.Group);
                var parent = options.ParentOverride != null ? options.ParentOverride : group.Root;
                view = await _adapter.CreateViewAsync(viewKey, parent, ct);
                ui.BindView(view, viewKey, options, this);
                group.Add(ui, options.Depth);
                _open.Add(ui);
                if (options.ReusePolicy == UIReusePolicy.Single)
                {
                    _singleOpen[type] = ui;
                }

                await ui.InvokeOpenAsync(options).AttachExternalCancellation(ct);
                return ui;
            }
            catch
            {
                if (view != null && ui.View == null)
                {
                    _adapter.ReleaseView(view);
                }

                if (!ui.IsDestroyed)
                {
                    ui.Destroy();
                }

                throw;
            }
        }

        public UniTask CloseAsync(UIEntity ui, UICloseReason reason = UICloseReason.User)
        {
            return CloseInternalAsync(ui, reason);
        }

        public async UniTask CloseGroupAsync(string group, UICloseReason reason = UICloseReason.User)
        {
            if (string.IsNullOrWhiteSpace(group) || !_groups.TryGetValue(group, out var uiGroup))
            {
                return;
            }

            _closeBuffer.Clear();
            for (var i = 0; i < uiGroup.Entities.Count; i++)
            {
                _closeBuffer.Add(uiGroup.Entities[i]);
            }

            for (var i = _closeBuffer.Count - 1; i >= 0; i--)
            {
                await CloseInternalAsync(_closeBuffer[i], reason == UICloseReason.User ? UICloseReason.GroupClosed : reason);
            }

            _closeBuffer.Clear();
        }

        public TUI Get<TUI>() where TUI : UIEntity
        {
            TryGet<TUI>(out var ui);
            return ui;
        }

        public bool TryGet<TUI>(out TUI ui) where TUI : UIEntity
        {
            if (_singleOpen.TryGetValue(typeof(TUI), out var existing) && existing != null && !existing.IsDestroyed)
            {
                ui = (TUI)existing;
                return true;
            }

            for (var i = 0; i < _open.Count; i++)
            {
                if (_open[i] is TUI typed && !typed.IsDestroyed)
                {
                    ui = typed;
                    return true;
                }
            }

            ui = null;
            return false;
        }

        public UISystemSnapshot GetSnapshot()
        {
            var infos = new List<UIInfo>(_open.Count);
            for (var i = 0; i < _open.Count; i++)
            {
                var ui = _open[i];
                if (ui == null || ui.IsDestroyed)
                {
                    continue;
                }

                infos.Add(new UIInfo
                {
                    UIName = ui.UIName,
                    Group = ui.Group,
                    Depth = ui.Depth,
                    ViewLocation = ui.ViewKey.Location,
                    Visible = ui.View != null && ui.View.GameObject != null && ui.View.GameObject.activeSelf
                });
            }

            return new UISystemSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow,
                OpenUIs = infos
            };
        }

        private async UniTask CloseInternalAsync(UIEntity ui, UICloseReason reason)
        {
            if (ui == null || ui.IsDestroyed)
            {
                return;
            }

            try
            {
                await ui.InvokeCloseAsync(reason);
            }
            finally
            {
                ui.Destroy();
            }
        }

        internal void ReleaseView(UIEntity ui, IUIView view)
        {
            if (view != null)
            {
                _adapter?.ReleaseView(view);
            }

            if (ui == null)
            {
                return;
            }

            _open.Remove(ui);
            var type = ui.GetType();
            if (_singleOpen.TryGetValue(type, out var existing) && ReferenceEquals(existing, ui))
            {
                _singleOpen.Remove(type);
            }

            if (!string.IsNullOrWhiteSpace(ui.Group) && _groups.TryGetValue(ui.Group, out var group))
            {
                group.Remove(ui);
            }
        }

        private UIGroupEntity GetOrCreateGroup(string group)
        {
            group = string.IsNullOrWhiteSpace(group) ? _options.DefaultGroup : group;
            if (_groups.TryGetValue(group, out var entity))
            {
                return entity;
            }

            var root = CreateGroupRoot(group, _root);
            entity = AddChild<UIGroupEntity, UIGroupOptions>(new UIGroupOptions
            {
                GroupName = group,
                Root = root
            });
            _groups.Add(group, entity);
            return entity;
        }

        private UIOpenParams NormalizeOpenParams(UIOpenParams options)
        {
            if (options == null)
            {
                options = new UIOpenParams();
            }

            if (string.IsNullOrWhiteSpace(options.Group))
            {
                options.Group = _options.DefaultGroup;
            }

            return options;
        }

        private static Transform EnsureUIRoot(UIOptions options, Transform frameworkRoot)
        {
            if (options != null && options.UIRoot != null)
            {
                EnsureCanvas(options.UIRoot.gameObject);
                return options.UIRoot;
            }

            var root = new GameObject("[GameEntity.Unity.Framework.UIRoot]", typeof(RectTransform));
            if (frameworkRoot != null)
            {
                root.transform.SetParent(frameworkRoot, false);
            }
            else
            {
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            EnsureCanvas(root);
            return root.transform;
        }

        private static Transform CreateGroupRoot(string group, Transform root)
        {
            var go = new GameObject($"UIGroup_{group}", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.transform;
        }

        private static void EnsureCanvas(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (go.GetComponent<CanvasScaler>() == null)
            {
                go.AddComponent<CanvasScaler>();
            }

            if (go.GetComponent<GraphicRaycaster>() == null)
            {
                go.AddComponent<GraphicRaycaster>();
            }
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                EnsureCompatibleInputModule(eventSystem.gameObject);
                return;
            }

            var go = new GameObject("[GameEntity.Unity.Framework.EventSystem]");
            go.AddComponent<EventSystem>();
            EnsureCompatibleInputModule(go);
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        private static void EnsureCompatibleInputModule(GameObject go)
        {
#if ENABLE_INPUT_SYSTEM
            var legacyModule = go.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                UnityEngine.Object.Destroy(legacyModule);
            }

            var inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType == null)
            {
                Debug.LogWarning("当前项目启用了 Input System，但未找到 InputSystemUIInputModule，UI 事件输入可能不可用。");
                return;
            }

            if (go.GetComponent(inputSystemModuleType) == null)
            {
                go.AddComponent(inputSystemModuleType);
            }
#else
            if (go.GetComponent<StandaloneInputModule>() == null)
            {
                go.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }

}
