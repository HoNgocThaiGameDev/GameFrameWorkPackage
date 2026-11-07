namespace NebulaSoft
{
    public interface ISkinsProvider
    {
        ISkinData GetSkinData(string skinId);

        void UnlockSkin(ISkinData skinData, bool select = false);
        void UnlockSkin(string skinId, bool select = false);

        bool IsSkinSelected(ISkinData skinData);
        bool IsSkinSelected(string skinId);

        void SelectSkin(ISkinData data);
        void SelectSkin(string skinId);
    }
}
