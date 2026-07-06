using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public enum UIReusePolicy
    {
        Single,
        Multiple
    }

    public enum UICloseReason
    {
        User,
        System,
        GroupClosed,
        Shutdown
    }

    public sealed class UIOpenParams
    {
        public AssetKey ViewKey;
        public string Group = "Default";
        public int Depth;
        public UIReusePolicy ReusePolicy = UIReusePolicy.Single;
        public object UserData;
        public Transform ParentOverride;
    }

    public readonly struct UIOpenContext
    {
        public readonly UIOpenParams Params;

        public UIOpenContext(UIOpenParams openParams)
        {
            Params = openParams;
        }
    }

    public readonly struct UICloseContext
    {
        public readonly UICloseReason Reason;

        public UICloseContext(UICloseReason reason)
        {
            Reason = reason;
        }
    }

    public interface IUIView
    {
        GameObject GameObject { get; }
        Transform Transform { get; }
        Canvas Canvas { get; }
        void SetVisible(bool visible);
        void SetDepth(int depth);
    }

    public interface IUIAdapter
    {
        UniTask<IUIView> CreateViewAsync(AssetKey viewKey, Transform parent, CancellationToken ct = default);
        void ReleaseView(IUIView view);
    }

    public interface IUISystem
    {
        UniTask<TUI> OpenAsync<TUI>(UIOpenParams options = null, CancellationToken ct = default)
            where TUI : UIEntity, new();

        UniTask CloseAsync(UIEntity ui, UICloseReason reason = UICloseReason.User);
        UniTask CloseGroupAsync(string group, UICloseReason reason = UICloseReason.User);
        TUI Get<TUI>() where TUI : UIEntity;
        bool TryGet<TUI>(out TUI ui) where TUI : UIEntity;
        UISystemSnapshot GetSnapshot();
    }

    public sealed class UIInfo
    {
        public string UIName;
        public string Group;
        public int Depth;
        public string ViewLocation;
        public bool Visible;
    }

    public sealed class UISystemSnapshot
    {
        public DateTime CapturedAtUtc;
        public IReadOnlyList<UIInfo> OpenUIs;
    }
}
