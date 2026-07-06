using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public interface IGameLaunchTask
    {
        int Order { get; }
        UniTask LaunchAsync(FrameworkEntry framework, CancellationToken ct = default);
    }
}
