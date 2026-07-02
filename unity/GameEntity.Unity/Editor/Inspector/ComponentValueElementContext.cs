using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GameEntity.Unity.Editor
{
    internal sealed class ComponentValueElementContext
    {
        private readonly HashSet<object> _activeObjects = new HashSet<object>(ReferenceObjectComparer.Instance);

        private readonly Dictionary<string, bool> _foldoutStates;

        public ComponentValueElementContext(
            string searchText,
            bool showPrivate,
            bool showNullValues,
            bool showProperties,
            bool showRawFrameworkFields,
            Dictionary<string, bool> foldoutStates)
        {
            SearchText = searchText ?? string.Empty;
            ShowPrivate = showPrivate;
            ShowNullValues = showNullValues;
            ShowProperties = showProperties;
            ShowRawFrameworkFields = showRawFrameworkFields;
            _foldoutStates = foldoutStates;
        }

        public string SearchText { get; }

        public bool ShowPrivate { get; }

        public bool ShowNullValues { get; }

        public bool ShowProperties { get; }

        public bool ShowRawFrameworkFields { get; }

        public int MaxDepth => 8;

        public int MaxCollectionItems => 64;

        public bool GetFoldoutState(string path, bool defaultValue)
        {
            if (string.IsNullOrEmpty(path) || _foldoutStates == null)
            {
                return defaultValue;
            }

            return _foldoutStates.TryGetValue(path, out bool value) ? value : defaultValue;
        }

        public void SetFoldoutState(string path, bool value)
        {
            if (string.IsNullOrEmpty(path) || _foldoutStates == null)
            {
                return;
            }

            _foldoutStates[path] = value;
        }

        public bool IsActiveObject(object value)
        {
            if (value == null || value.GetType().IsValueType)
            {
                return false;
            }

            return _activeObjects.Contains(value);
        }

        public bool TryEnterObject(object value)
        {
            if (value == null || value.GetType().IsValueType)
            {
                return true;
            }

            return _activeObjects.Add(value);
        }

        public void ExitObject(object value)
        {
            if (value == null || value.GetType().IsValueType)
            {
                return;
            }

            _activeObjects.Remove(value);
        }
    }

    internal sealed class ReferenceObjectComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceObjectComparer Instance = new ReferenceObjectComparer();

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
