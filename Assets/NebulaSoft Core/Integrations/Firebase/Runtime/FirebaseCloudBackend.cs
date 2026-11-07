#if FIREBASE
using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using NebulaSoft.Cloud;

namespace NebulaSoft.Integrations.Firebase
{
    internal sealed class FirebaseCloudBackend : ICloudBackend
    {
        private bool disposed;
        public ICloudAuthProvider Auth { get; private set; }
        public ICloudDocumentStore Documents { get; private set; }

        public async Task InitializeAsync(CancellationToken cancellation)
        {
            await FirebaseDependencies.EnsureAsync(cancellation);
            if (disposed) throw new ObjectDisposedException(nameof(FirebaseCloudBackend));
            Auth = new FirebaseAuthAdapter(FirebaseAuth.DefaultInstance);
            Documents = new FirebaseDocumentStore(FirebaseFirestore.DefaultInstance);
        }
        public void Dispose()
        {
            disposed = true;
            Auth = null;
            Documents = null;
            // Firebase singletons can be shared by Analytics and other SDK features.
        }
    }

    internal sealed class FirebaseAuthAdapter : ICloudAuthProvider
    {
        private readonly FirebaseAuth auth;
        internal FirebaseAuthAdapter(FirebaseAuth auth) { this.auth = auth; }
        public CloudUser CurrentUser => Snapshot(auth.CurrentUser);
        public async Task<CloudUser> SignInAnonymouslyAsync()
        {
            try
            {
                var result = await auth.SignInAnonymouslyAsync();
                var user = Snapshot(auth.CurrentUser);
                GC.KeepAlive(result);
                return user;
            }
            catch (Exception exception) { throw ConvertError(exception); }
        }
        public async Task<CloudUser> LinkFacebookAsync(string accessToken)
        {
            try
            {
                var user = auth.CurrentUser;
                if (user == null) throw new CloudException(CloudError.Authentication, "No current user to link.");
                using (var credential = FacebookAuthProvider.GetCredential(accessToken))
                {
                    // Do not expose AuthResult/FirebaseUser lifetimes or their version-specific return types.
                    var result = await user.LinkWithCredentialAsync(credential);
                    var snapshot = Snapshot(auth.CurrentUser);
                    GC.KeepAlive(result);
                    return snapshot;
                }
            }
            catch (Exception exception) { throw ConvertError(exception); }
        }
        public async Task<CloudUser> SignInFacebookAsync(string accessToken)
        {
            try
            {
                using (var credential = FacebookAuthProvider.GetCredential(accessToken))
                {
                    var result = await auth.SignInWithCredentialAsync(credential);
                    var snapshot = Snapshot(auth.CurrentUser);
                    GC.KeepAlive(result);
                    return snapshot;
                }
            }
            catch (Exception exception) { throw ConvertError(exception); }
        }
        public void SignOut() => auth.SignOut();
        private static CloudUser Snapshot(FirebaseUser user) => user == null ? null : new CloudUser(user.UserId, user.IsAnonymous);
        private static Exception ConvertError(Exception exception)
        {
            if (exception is CloudException) return exception;
            if (exception is FirebaseException firebase && firebase.ErrorCode == (int)AuthError.CredentialAlreadyInUse)
                return new CloudException(CloudError.CredentialAlreadyInUse, "Credential belongs to another account.", exception);
            return new CloudException(CloudError.Authentication, "Firebase authentication failed.", exception);
        }
    }
}
#endif
