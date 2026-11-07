namespace NebulaSoft
{
    [StaticUnload]
    public static class ConsentData
    {
        public static bool IsConsentGiven { get; private set; } = false;
        public static NebulaSoft.AuthorizationTrackingStatus ATTStatus { get; private set; } = NebulaSoft.AuthorizationTrackingStatus.NOT_DETERMINED;

        public static void SetATTStatus(NebulaSoft.AuthorizationTrackingStatus status)
        {
            ATTStatus = status;
        }

        public static void SetConsentGiven(bool consentGiven)
        {
            IsConsentGiven = consentGiven;
        }

        private static void UnloadStatic()
        {
            IsConsentGiven = false;
            ATTStatus = NebulaSoft.AuthorizationTrackingStatus.NOT_DETERMINED;
        }
    }
}
