using System.Threading.Tasks;

namespace NebulaSoft.Cloud
{
    public interface ICloudAuthProvider
    {
        CloudUser CurrentUser { get; }
        Task<CloudUser> SignInAnonymouslyAsync();
        Task<CloudUser> LinkFacebookAsync(string accessToken);
        Task<CloudUser> SignInFacebookAsync(string accessToken);
        void SignOut();
    }
}
