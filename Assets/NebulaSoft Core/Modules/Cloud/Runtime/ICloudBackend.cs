using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Cloud
{
    /// <summary>Application-owned adapter. No native SDK types cross this boundary.</summary>
    public interface ICloudBackend : IDisposable
    {
        ICloudAuthProvider Auth { get; }
        ICloudDocumentStore Documents { get; }
        Task InitializeAsync(CancellationToken cancellation);
    }
}
