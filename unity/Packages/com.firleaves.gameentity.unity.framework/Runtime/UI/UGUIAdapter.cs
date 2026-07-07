using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameEntity.Unity.Framework
{
    public sealed class UGUIAdapter : IUIAdapter
    {
        private readonly IInstancePool _instancePool;

        public UGUIAdapter(IInstancePool instancePool)
        {
            _instancePool = instancePool ?? throw new FrameworkException("UGUIAdapter 需要 InstancePool。");
        }

        public async UniTask<IUIView> CreateViewAsync(AssetKey viewKey, Transform parent, CancellationToken ct = default)
        {
            var instance = await _instancePool.RentAsync(viewKey, parent, new InstanceRentOptions
            {
                SetActive = true,
                WorldPositionStays = false
            }, ct);

            return new UGUIView(instance);
        }

        public void ReleaseView(IUIView view)
        {
            if (view is UGUIView uguiView)
            {
                uguiView.Release();
            }
            else if (view?.GameObject != null)
            {
                Object.Destroy(view.GameObject);
            }
        }
    }

}
