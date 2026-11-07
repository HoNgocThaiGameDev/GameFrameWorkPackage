using UnityEngine;

namespace NebulaSoft
{
    public interface IRewardPreview
    {
        public Sprite Icon { get; }
        public string Text { get; }

        public int SortingOrder { get; }

        GameObject GetCustomUIPrefab();
    }
}
