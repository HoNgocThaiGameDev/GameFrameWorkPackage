namespace NebulaSoft
{
    public class PurchaseFailEvent : IAnalyticsEventData
    {
        public IAPItem Item;
        public NebulaSoft.PurchaseFailureReason FailureReason;
    }
}
