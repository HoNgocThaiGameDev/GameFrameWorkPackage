namespace NebulaSoft
{
    public class UINoAdsOffer : UIIAPOffer, IRewardPresentationBlocker
    {
        private void OnEnable()
        {
            AdsManager.NoAdsEntitlementChanged += OnNoAdsEntitlementChanged;
        }

        private void OnDisable()
        {
            AdsManager.NoAdsEntitlementChanged -= OnNoAdsEntitlementChanged;
        }

        public override void PlayShowAnimation()
        {
            BottomNavigationVisibilityEvents.RequestHide();
            base.PlayShowAnimation();
        }

        public override void PlayHideAnimation()
        {
            base.PlayHideAnimation();
        }

        protected override void OnOfferClosed()
        {
            BottomNavigationVisibilityEvents.RequestShow();
        }

        private void OnNoAdsEntitlementChanged(bool hasNoAds)
        {
            if (hasNoAds && IsOpened)
                UIController.HidePage(this);
        }
    }
}
