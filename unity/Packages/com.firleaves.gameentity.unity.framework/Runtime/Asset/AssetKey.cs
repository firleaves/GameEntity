using System;
using UnityEngine;

namespace GameEntity.Unity.Framework
{
    public readonly struct AssetKey : IEquatable<AssetKey>
    {
        public readonly string Location;
        public readonly string PackageName;
        public readonly AssetKind Kind;
        public readonly Type AssetType;
        public readonly string SubAssetName;

        public AssetKey(string location, string packageName, AssetKind kind, Type assetType, string subAssetName)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new FrameworkException("AssetKey 的 Location 不能为空。");
            }

            Location = location;
            PackageName = string.IsNullOrWhiteSpace(packageName) ? null : packageName;
            Kind = kind;
            AssetType = assetType;
            SubAssetName = string.IsNullOrWhiteSpace(subAssetName) ? null : subAssetName;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Location);

        public static AssetKey Main<T>(string location, string packageName = null) where T : UnityEngine.Object
        {
            return new AssetKey(location, packageName, AssetKind.MainAsset, typeof(T), null);
        }

        public static AssetKey Main(string location, Type assetType, string packageName = null)
        {
            if (assetType == null || !typeof(UnityEngine.Object).IsAssignableFrom(assetType))
            {
                throw new FrameworkException("主资源 AssetKey 必须指定 UnityEngine.Object 类型。");
            }

            return new AssetKey(location, packageName, AssetKind.MainAsset, assetType, null);
        }

        public static AssetKey SubAssets<T>(string location, string packageName = null) where T : UnityEngine.Object
        {
            return new AssetKey(location, packageName, AssetKind.SubAssets, typeof(T), null);
        }

        public static AssetKey SubAsset<T>(string location, string subAssetName, string packageName = null) where T : UnityEngine.Object
        {
            return new AssetKey(location, packageName, AssetKind.SubAssets, typeof(T), subAssetName);
        }

        public static AssetKey RawFile(string location, string packageName = null)
        {
            return new AssetKey(location, packageName, AssetKind.RawFile, null, null);
        }

        public static AssetKey Scene(string location, string packageName = null)
        {
            return new AssetKey(location, packageName, AssetKind.Scene, typeof(SceneAssetMarker), null);
        }

        public bool Equals(AssetKey other)
        {
            return string.Equals(Location, other.Location, StringComparison.Ordinal)
                && string.Equals(PackageName, other.PackageName, StringComparison.Ordinal)
                && Kind == other.Kind
                && AssetType == other.AssetType
                && string.Equals(SubAssetName, other.SubAssetName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Location ?? string.Empty);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(PackageName ?? string.Empty);
                hash = hash * 31 + (int)Kind;
                hash = hash * 31 + (AssetType != null ? AssetType.GetHashCode() : 0);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(SubAssetName ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            var package = string.IsNullOrWhiteSpace(PackageName) ? "Default" : PackageName;
            var type = AssetType == null ? "raw" : AssetType.Name;
            return $"{package}:{Kind}:{type}:{Location}:{SubAssetName}";
        }

        private sealed class SceneAssetMarker : ScriptableObject
        {
        }
    }

}
