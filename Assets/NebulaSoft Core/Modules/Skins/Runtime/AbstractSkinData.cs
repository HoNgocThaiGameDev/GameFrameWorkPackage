using UnityEngine;

namespace NebulaSoft
{
    public abstract class AbstractSkinData : ISkinData
    {
        [SerializeField, UniqueID] string id;
        public string ID => id;

        [SkinPreview]
        [SerializeField] 
        protected Sprite previewSprite;
        public Sprite PreviewSprite => previewSprite;

        public int Hash { get; private set; }

        public AbstractSkinDatabase SkinsProvider { get; private set; }

        public bool IsUnlocked => save.IsUnlocked;

        private SkinSave save;

        public virtual void Init(AbstractSkinDatabase provider)
        {
            save = SaveController.GetSaveObject<SkinSave>(id);
            Hash = id.GetHashCode();

            SkinsProvider = provider;
        }

        public void Unlock()
        {
            if (save.IsUnlocked) return;
            save.IsUnlocked = true;
            SaveController.MarkAsSaveIsRequired();
        }
    }
}
