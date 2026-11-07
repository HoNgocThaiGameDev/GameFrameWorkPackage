namespace NebulaSoft.Cloud
{
    /// <summary>Detached identity snapshot; never holds a native user handle.</summary>
    public sealed class CloudUser
    {
        public string UserId { get; }
        public bool IsAnonymous { get; }
        public CloudUser(string userId, bool isAnonymous)
        {
            UserId = userId;
            IsAnonymous = isAnonymous;
        }
    }
}
