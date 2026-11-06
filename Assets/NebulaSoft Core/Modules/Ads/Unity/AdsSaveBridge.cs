using System;
using NebulaSoft.Ads;

namespace NebulaSoft
{
    /// <summary>Preserves the existing framework save key. The ads runtime never reads SaveController.</summary>
    public sealed class AdsSaveBridge
    {
        private const double Permanent = -1;
        private readonly IAdsService service;
        private readonly AdSave save;
        private readonly Func<double> unixTime;
        public bool HasNoAds => save.ForcedAdDisabledUntil == Permanent;
        public bool ForcedAdsEnabled => !HasNoAds && unixTime() >= save.ForcedAdDisabledUntil;
        public event Action<bool> NoAdsChanged;
        public event Action ForcedAdsDisabled;
        public event Action<bool> ForcedAdsAvailabilityChanged;

        public AdsSaveBridge(IAdsService service, Func<double> unixTime)
        {
            this.service = service;
            this.unixTime = unixTime;
            save = SaveController.GetSaveObject<AdSave>("advertisement_forced_ad");
            Tick();
        }

        public void SetNoAds(bool noAds)
        {
            if (noAds) SetUntil(Permanent);
            else if (HasNoAds) SetUntil(0);
        }

        public void DisableFor(int seconds)
        {
            if (seconds < 0 || HasNoAds) return;
            var until = unixTime() + seconds;
            if (until > save.ForcedAdDisabledUntil) SetUntil(until);
        }

        private void SetUntil(double until)
        {
            if (save.ForcedAdDisabledUntil == until) return;
            bool previous = HasNoAds;
            save.ForcedAdDisabledUntil = until;
            SaveController.MarkAsSaveIsRequired();
            Tick();
            if (!ForcedAdsEnabled) ForcedAdsDisabled?.Invoke();
            if (previous != HasNoAds) NoAdsChanged?.Invoke(HasNoAds);
        }

        public void Tick()
        {
            bool disabled = !ForcedAdsEnabled;
            if (disabled == service.ForcedAdsDisabled) return;
            service.SetForcedAdsDisabled(disabled);
            ForcedAdsAvailabilityChanged?.Invoke(!disabled);
        }
    }
}
