using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class SkinRewardView : RewardView
    {
        [SerializeField] Image previewImage;

        public SkinRewardView() { }
        public SkinRewardView(Image previewImage)
        {
            this.previewImage = previewImage;
        }

        protected override void OnInitialized()
        {
            SkinReward skinReward = (SkinReward)reward;
            if (skinReward != null)
            {
                SkinController skinController = SkinController.Instance;
                if (skinController != null)
                {
                    ISkinData skinData = skinController.GetSkinData(skinReward.SkinID);
                    if (skinData != null)
                    {
                        previewImage.sprite = skinData.PreviewSprite;
                    }
                }
            }
        }

        public override void OnPurchased()
        {

        }
    }
}
