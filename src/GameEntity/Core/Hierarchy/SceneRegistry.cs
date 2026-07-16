using System.Collections.Generic;
using System.Linq;

namespace GameEntity
{
    internal sealed class SceneRegistry
    {
        private readonly Dictionary<string, long> _sceneNameToNodeId = new Dictionary<string, long>();
        private readonly Dictionary<long, string> _sceneNameByNodeId = new Dictionary<long, string>();

        public void Register(string sceneName, long sceneNodeId)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            _sceneNameToNodeId[sceneName] = sceneNodeId;
            _sceneNameByNodeId[sceneNodeId] = sceneName;
        }

        public bool TryGetSceneNodeId(string sceneName, out long sceneNodeId)
        {
            return _sceneNameToNodeId.TryGetValue(sceneName, out sceneNodeId);
        }

        public bool TryGetSceneName(long sceneNodeId, out string sceneName)
        {
            return _sceneNameByNodeId.TryGetValue(sceneNodeId, out sceneName);
        }

        public IReadOnlyList<KeyValuePair<string, long>> GetNameEntries()
        {
            return _sceneNameToNodeId.ToList();
        }

        public IReadOnlyList<KeyValuePair<long, string>> GetNodeEntries()
        {
            return _sceneNameByNodeId.ToList();
        }

        public void Unregister(long sceneNodeId)
        {
            if (!_sceneNameByNodeId.TryGetValue(sceneNodeId, out var sceneName))
            {
                return;
            }

            if (_sceneNameToNodeId.TryGetValue(sceneName, out var currentNodeId) && currentNodeId == sceneNodeId)
            {
                _sceneNameToNodeId.Remove(sceneName);
            }

            _sceneNameByNodeId.Remove(sceneNodeId);
        }

        public void Clear()
        {
            _sceneNameToNodeId.Clear();
            _sceneNameByNodeId.Clear();
        }
    }
}
