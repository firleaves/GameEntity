using System;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public SaveMeta Meta = new SaveMeta();
        public string PayloadJson = string.Empty;
    }

}
