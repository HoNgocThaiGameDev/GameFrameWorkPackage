namespace NebulaSoft.Analytics
{
    public interface IAnalyticsDiagnostics
    {
        AnalyticsDiagnostics Diagnostics { get; }
    }

    /// <summary>Local delivery counters. Sent means accepted by the provider call, not acknowledged by its backend.</summary>
    public readonly struct AnalyticsDiagnostics
    {
        public int PendingCount { get; }
        public long AcceptedCount { get; }
        public long SentCount { get; }
        public long QueueOverflowCount { get; }
        public long IdentityDiscardCount { get; }
        public long CollectionDiscardCount { get; }
        public long ShutdownDiscardCount { get; }
        public long RejectedCount { get; }
        public long SendFailureCount { get; }
        public long InitializationFailureCount { get; }

        internal AnalyticsDiagnostics(int pendingCount, long accepted, long sent, long overflow,
            long identity, long collection, long shutdown, long rejected, long sendFailures, long initializationFailures)
        {
            PendingCount = pendingCount;
            AcceptedCount = accepted;
            SentCount = sent;
            QueueOverflowCount = overflow;
            IdentityDiscardCount = identity;
            CollectionDiscardCount = collection;
            ShutdownDiscardCount = shutdown;
            RejectedCount = rejected;
            SendFailureCount = sendFailures;
            InitializationFailureCount = initializationFailures;
        }
    }
}
