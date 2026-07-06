using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameEntity;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public sealed class LocalizationSystemEntity : Entity, IAwake<IAssetPool>, IDestroy, ILocalizationSystem
    {
        private readonly Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.Ordinal);
        private IAssetPool _assetPool;

        public string CurrentLanguage { get; private set; }
        public event Action<string> LanguageChanged;

        public void Awake(IAssetPool assetPool)
        {
            _assetPool = assetPool ?? throw new FrameworkException("LocalizationSystem 初始化失败：AssetPool 不能为空。");
        }

        public void OnDestroy()
        {
            _texts.Clear();
            CurrentLanguage = null;
            _assetPool = null;
        }

        public async UniTask LoadLanguageAsync(string language, string location, string packageName = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new FrameworkException("加载语言失败：language 不能为空。");
            }

            using (var textAsset = await _assetPool.LoadAsync<TextAsset>(location, packageName, ct: ct))
            {
                var table = JsonUtility.FromJson<LocalizationTable>(textAsset.Asset.text);
                _texts.Clear();
                if (table?.Entries != null)
                {
                    for (var i = 0; i < table.Entries.Length; i++)
                    {
                        var entry = table.Entries[i];
                        if (entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                        {
                            _texts[entry.Key] = entry.Value ?? string.Empty;
                        }
                    }
                }
            }

            CurrentLanguage = language;
            LanguageChanged?.Invoke(language);
        }

        public string GetText(string key, string fallback = null)
        {
            return TryGetText(key, out var value) ? value : fallback ?? key;
        }

        public string Format(string key, params object[] args)
        {
            var text = GetText(key);
            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }

        public bool TryGetText(string key, out string value)
        {
            value = null;
            return !string.IsNullOrWhiteSpace(key) && _texts.TryGetValue(key, out value);
        }
    }
}
