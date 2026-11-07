namespace NebulaSoft
{
    public enum FakeStoreMode
    {
        /// <summary>
        /// FakeStore by default displays no dialogs.
        /// </summary>
        Default,

        /// <summary>
        /// Simple dialog is shown when Purchasing.
        /// </summary>
        StandardUser,

        /// <summary>
        /// Dialogs with failure reason code selection when
        /// Initializing/Retrieving Products and when Purchasing.
        /// </summary>
        DeveloperUser
    }
}