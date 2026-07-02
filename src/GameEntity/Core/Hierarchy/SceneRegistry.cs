using System.Collections.Generic;

namespace GameEntity
{
    internal sealed class SceneRegistry
    {
        private readonly Dictionary<string, int> _sceneNameToNodeId = new Dictionary<string, int>();
        private readonly Dictionary<int, string> _sceneNameByNodeId = new Dictionary<int, string>();

        public void Register(string sceneName, int sceneNodeId)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            _sceneNameToNodeId[sceneName] = sceneNodeId;
            _sceneNameByNodeId[sceneNodeId] = sceneName;
        }

        public bool TryGetSceneNodeId(string sceneName, out int sceneNodeId)
        {
            return _sceneNameToNodeId.TryGetValue(sceneName, out sceneNodeId);
        }

        public void Unregister(int sceneNodeId)
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
