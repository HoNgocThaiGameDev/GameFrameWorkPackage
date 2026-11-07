using UnityEngine;

namespace NebulaSoft
{
    public interface ISkinData
    {
        string ID { get; }
        int Hash { get; }
        bool IsUnlocked { get; }

        AbstractSkinDatabase SkinsProvider { get; }
        Sprite PreviewSprite { get; }

        void Init(AbstractSkinDatabase provider);
        void Unlock();
    }
}