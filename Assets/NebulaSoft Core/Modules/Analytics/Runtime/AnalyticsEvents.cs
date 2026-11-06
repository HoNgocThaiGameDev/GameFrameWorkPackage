using System.Collections.Generic;

namespace NebulaSoft.Analytics
{
    /// <summary>Typed game analytics events independent of game genre and provider. Level numbers start at one.</summary>
    public sealed class AnalyticsEvents
    {
        private readonly IAnalyticsService service;
        public AnalyticsEvents(IAnalyticsService service) { this.service = service; }

        public bool LevelStart(int level, int attempt = 1, string levelType = "normal") => Send("level_start", P("level", level), P("level_name", level.ToString()), P("attempt", attempt), P("level_type", levelType));
        public bool LevelEnd(int level, bool success, double durationSeconds, int attempt = 1, int moves = 0, int boosters = 0, string reason = null) =>
            Send("level_end", P("level", level), P("level_name", level.ToString()), P("success", success), P("duration", durationSeconds), P("attempt", attempt), P("moves", moves), P("boosters_used", boosters), P("reason", reason));
        public bool LevelFail(int level, string reason) => Send("level_fail", P("level", level), P("reason", reason));
        public bool LevelRetry(int level) => Send("level_retry", P("level", level));
        public bool LevelRevive(int level, string source) => Send("level_revive", P("level", level), P("source", source));
        public bool TutorialBegin(string tutorialId) => Send("tutorial_begin", P("tutorial_id", tutorialId));
        public bool TutorialStep(string tutorialId, int step) => Send("tutorial_step", P("tutorial_id", tutorialId), P("step", step));
        public bool TutorialComplete(string tutorialId) => Send("tutorial_complete", P("tutorial_id", tutorialId));
        public bool BoosterUsed(string boosterId, int level, int remaining = 0) => Send("booster_used", P("booster_id", boosterId), P("level", level), P("remaining", remaining));
        public bool BoosterPurchased(string boosterId, int amount, string source) => Send("booster_purchased", P("booster_id", boosterId), P("quantity", amount), P("source", source));
        public bool EarnCurrency(string currency, double amount, string source) => Send("earn_virtual_currency", P("virtual_currency_name", currency), P("value", amount), P("source", source));
        public bool SpendCurrency(string currency, double amount, string itemId) => Send("spend_virtual_currency", P("virtual_currency_name", currency), P("value", amount), P("item_name", itemId));
        public bool RewardClaimed(string source, string rewardId, int quantity = 1) => Send("reward_claimed", P("source", source), P("reward_id", rewardId), P("quantity", quantity));
        public bool IapAttempt(string productId) => Send("iap_attempt", P("item_id", productId));
        // Operational purchase funnel, deliberately separate from Firebase's automatically collected in_app_purchase revenue.
        public bool IapCompleted(string productId) => Send("iap_purchase", P("item_id", productId));
        public bool IapFailed(string productId, string reason) => Send("iap_failed", P("item_id", productId), P("reason", reason));
        public bool ScreenView(string screen, string screenClass = null) => Send("screen_view", P("screen_name", screen), P("screen_class", screenClass));
        public bool Login(string method) => Send("login", P("method", method));
        public bool SettingChanged(string setting, string value) => Send("setting_change", P("setting_name", setting), P("setting_value", value));
        public bool AdRequested(string placement, string format) => Send("ad_request", P("placement", placement), P("ad_format", format));
        public bool AdShown(string placement, string format, string platform) => Send("ad_show", P("placement", placement), P("ad_format", format), P("ad_platform", platform));
        // Operational callback count, separate from the SDK's ad_impression/revenue reporting.
        public bool AdImpression(string placement, string format, string platform) => Send("ad_impression_custom", P("placement", placement), P("ad_format", format), P("ad_platform", platform));
        public bool AdClicked(string placement, string format) => Send("ad_click_custom", P("placement", placement), P("ad_format", format));
        public bool AdClosed(string placement, string format) => Send("ad_close", P("placement", placement), P("ad_format", format));
        public bool AdRewarded(string placement) => Send("ad_reward", P("placement", placement));
        public bool AdFailed(string placement, string format, string stage, string reason) => Send("ad_fail", P("placement", placement), P("ad_format", format), P("stage", stage), P("reason", reason));

        private bool Send(string name, params KeyValuePair<string, object>[] parameters)
        {
            var values = new Dictionary<string, object>();
            foreach (var pair in parameters) values.Add(pair.Key, pair.Value);
            return service.Track(new AnalyticsEvent(name, values));
        }
        private static KeyValuePair<string, object> P(string name, object value) => new KeyValuePair<string, object>(name, value);
    }
}
