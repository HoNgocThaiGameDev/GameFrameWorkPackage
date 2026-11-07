using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [Serializable]
    [RegisterReward(typeof(SkinRewardView))]
    public sealed class SkinReward : Reward
    {
        private const int PREVIEW_SORTING_ORDER = 0;

        [SkinPicker]
        [SerializeField] string skinID;
        public string SkinID => skinID;

        [SerializeField] bool autoSelect = true;

        public SkinReward() { }
        public SkinReward(string skinID, bool autoSelect)
        {
            this.skinID = skinID;
            this.autoSelect = autoSelect;
        }

        public override void ApplyReward()
        {
            SkinController.Instance?.UnlockSkin(skinID, autoSelect);
        }

        public override bool CheckDisableState()
        {
            SkinController skinController = SkinController.Instance;
            if(skinController != null)
            {
                return skinController.IsSkinUnlocked(skinID);
            }

            return false;
        }

        public override List<IRewardPreview> GetRewardPreviews()
        {
            Sprite skinPreviewSprite = null;

            SkinController skinController = SkinController.Instance;
            if (skinController != null)
            {
                ISkinData skinData = skinController.GetSkinData(skinID);
                if (skinData != null)
                {
                    skinPreviewSprite = skinData.PreviewSprite;
                }
            }

            return new List<IRewardPreview>()
            {
                new RewardPreview(skinPreviewSprite, "NEW SKIN!", PREVIEW_SORTING_ORDER)
            };
        }
    }
}
