using System;

namespace NebulaSoft.Cloud
{
    public enum CloudError
    {
        Unknown, Unavailable, Timeout, PermissionDenied, Offline, Authentication,
        CredentialAlreadyInUse, InvalidData
    }

    public sealed class CloudException : Exception
    {
        public CloudError Error { get; }
        public CloudException(CloudError error, string message, Exception inner = null) : base(message, inner)
        {
            Error = error;
        }
    }
}
