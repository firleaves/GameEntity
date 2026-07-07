using System;

namespace GameEntity.Unity.Framework
{
    [Serializable]
    public sealed class SaveSystemConfig
    {
        public int MaxSlots = 3;
        public int DefaultSlot;
        public float AutoSaveInterval = 1f;
        public bool EnableChecksum;
        public bool PrettyPrint = true;
        public bool SaveOnDestroy = true;
        public string SaveFolderName = "saves";

        public SaveSystemConfig Clone()
        {
            return new SaveSystemConfig
            {
                MaxSlots = MaxSlots,
                DefaultSlot = DefaultSlot,
                AutoSaveInterval = AutoSaveInterval,
                EnableChecksum = EnableChecksum,
                PrettyPrint = PrettyPrint,
                SaveOnDestroy = SaveOnDestroy,
                SaveFolderName = SaveFolderName
            };
        }

        public static SaveSystemConfig CreateDefault()
        {
            return new SaveSystemConfig();
        }
    }

}
