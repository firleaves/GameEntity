using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public static class UISystemExtensions
    {
        public static UniTask<TUI> OpenAsync<TUI>(
            this IUISystem uiSystem,
            string viewLocation,
            string group = null,
            int depth = 0,
            UIReusePolicy reusePolicy = UIReusePolicy.Single,
            object userData = null,
            Transform parentOverride = null,
            string packageName = null,
            CancellationToken ct = default)
            where TUI : UIEntity, new()
        {
            if (uiSystem == null)
            {
                throw new FrameworkException("UISystem 不能为空。");
            }

            return uiSystem.OpenAsync<TUI>(
                new UIOpenParams
                {
                    ViewKey = AssetKey.Main<GameObject>(viewLocation, packageName),
                    Group = group,
                    Depth = depth,
                    ReusePolicy = reusePolicy,
                    UserData = userData,
                    ParentOverride = parentOverride
                },
                ct);
        }
    }
}
