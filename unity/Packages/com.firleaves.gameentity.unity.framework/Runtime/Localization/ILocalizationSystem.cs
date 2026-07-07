using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameEntity.Unity.Framework
{
    public interface ILocalizationSystem
    {
        string CurrentLanguage { get; }
        event Action<string> LanguageChanged;

        UniTask LoadLanguageAsync(string language, string location, string packageName = null, CancellationToken ct = default);
        string GetText(string key, string fallback = null);
        string Format(string key, params object[] args);
        bool TryGetText(string key, out string value);
    }

}
