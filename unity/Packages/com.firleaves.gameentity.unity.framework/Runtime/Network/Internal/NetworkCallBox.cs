using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    internal sealed class NetworkCallBox
    {
        private readonly Dictionary<int, PendingCall> _calls = new Dictionary<int, PendingCall>();
        private readonly List<int> _removeBuffer = new List<int>();
        private int _nextRpcId;

        public int Count => _calls.Count;

        public UniTask<TResponse> Add<TResponse>(
            object request,
            INetworkProtocol protocol,
            float timeoutSeconds,
            CancellationToken ct)
        {
            var rpcId = NextRpcId();
            if (!protocol.TrySetRequestId(request, rpcId))
            {
                throw new FrameworkException($"网络请求失败：{request.GetType().Name} 未实现 INetworkRequest。");
            }

            var source = new UniTaskCompletionSource<TResponse>();
            var call = new PendingCall(
                rpcId,
                typeof(TResponse),
                new PendingCallSource<TResponse>(source),
                Math.Max(0f, timeoutSeconds));
            _calls.Add(rpcId, call);

            if (ct.CanBeCanceled)
            {
                call.Cancellation = ct.Register(() => Cancel(rpcId, ct));
            }

            return source.Task;
        }

        public bool TrySetResponse(object response, int rpcId)
        {
            if (!_calls.Remove(rpcId, out var call))
            {
                return false;
            }

            call.Dispose();
            if (!call.ResponseType.IsInstanceOfType(response))
            {
                call.Source.SetException(new FrameworkException(
                    $"网络响应类型不匹配：期望 {call.ResponseType.Name}，实际 {response.GetType().Name}。"));
                return true;
            }

            call.Source.SetResult(response);
            return true;
        }

        public void Fail(int rpcId, Exception exception)
        {
            if (_calls.Remove(rpcId, out var call))
            {
                call.Dispose();
                call.Source.SetException(exception);
            }
        }

        public void Update(float deltaTime)
        {
            if (_calls.Count == 0)
            {
                return;
            }

            _removeBuffer.Clear();
            foreach (var pair in _calls)
            {
                var call = pair.Value;
                if (call.TimeoutSeconds <= 0f)
                {
                    continue;
                }

                call.ElapsedSeconds += Math.Max(0f, deltaTime);
                if (call.ElapsedSeconds >= call.TimeoutSeconds)
                {
                    _removeBuffer.Add(pair.Key);
                }
            }

            for (var i = 0; i < _removeBuffer.Count; i++)
            {
                var rpcId = _removeBuffer[i];
                if (_calls.Remove(rpcId, out var call))
                {
                    call.Dispose();
                    call.Source.SetException(new TimeoutException($"网络请求超时：RpcId={rpcId}"));
                }
            }

            _removeBuffer.Clear();
        }

        public void CancelAll(NetworkCloseReason reason)
        {
            if (_calls.Count == 0)
            {
                return;
            }

            foreach (var call in _calls.Values)
            {
                call.Dispose();
                call.Source.SetException(new FrameworkException($"网络请求被取消：连接已关闭，原因={reason}。"));
            }

            _calls.Clear();
        }

        private void Cancel(int rpcId, CancellationToken ct)
        {
            if (_calls.Remove(rpcId, out var call))
            {
                call.Dispose();
                call.Source.SetCanceled(ct);
            }
        }

        private int NextRpcId()
        {
            if (_nextRpcId == int.MaxValue)
            {
                _nextRpcId = 0;
            }

            return ++_nextRpcId;
        }

        private interface IPendingCallSource
        {
            void SetResult(object response);
            void SetException(Exception exception);
            void SetCanceled(CancellationToken ct);
        }

        private sealed class PendingCallSource<TResponse> : IPendingCallSource
        {
            private readonly UniTaskCompletionSource<TResponse> _source;

            public PendingCallSource(UniTaskCompletionSource<TResponse> source)
            {
                _source = source;
            }

            public void SetResult(object response)
            {
                _source.TrySetResult((TResponse)response);
            }

            public void SetException(Exception exception)
            {
                _source.TrySetException(exception);
            }

            public void SetCanceled(CancellationToken ct)
            {
                _source.TrySetCanceled(ct);
            }
        }

        private sealed class PendingCall : IDisposable
        {
            public PendingCall(int rpcId, Type responseType, IPendingCallSource source, float timeoutSeconds)
            {
                RpcId = rpcId;
                ResponseType = responseType;
                Source = source;
                TimeoutSeconds = timeoutSeconds;
            }

            public int RpcId { get; }
            public Type ResponseType { get; }
            public IPendingCallSource Source { get; }
            public float TimeoutSeconds { get; }
            public float ElapsedSeconds;
            public CancellationTokenRegistration Cancellation;

            public void Dispose()
            {
                Cancellation.Dispose();
            }
        }
    }
}
