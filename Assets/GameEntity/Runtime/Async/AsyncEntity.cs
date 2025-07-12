using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GE
{
    public abstract class AsyncEntity : Entity
    {
        public bool IsLoaded { get; private set; }
        private CancellationTokenSource _cts;

        public async UniTask InitializeAsync(CancellationToken cancelToken = default)
        {
            if (IsLoaded) return;

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            var token = _cts.Token;
            if (token.IsCancellationRequested) return;
            try
            {
                await OnLoadAsync(token);
                IsLoaded = true;
                OnLoaded();
            }
            catch (OperationCanceledException )
            {
                Log.Info("AsyncEntity 加载被取消");
            }
            catch (Exception ex)
            {
                Log.Error($"AsyncEntity 加载异常: {ex}");
                throw ;
            }

        }

        protected abstract UniTask OnLoadAsync(CancellationToken cancelToken);
        protected virtual void OnLoaded() { }


        public override void Dispose()
        {
            base.Dispose();
            IsLoaded = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

    }
}
