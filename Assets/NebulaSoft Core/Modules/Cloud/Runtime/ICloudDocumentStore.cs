using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaSoft.Cloud
{
    public enum CloudReadSource { Default, Server }
    public enum CloudWriteMode { Replace, Merge }
    public enum CloudFieldValue { ServerTimestamp }

    public interface ICloudDocumentStore
    {
        Task<CloudDocumentSnapshot> GetSnapshotAsync(string path, CloudReadSource source);
        Task SetAsync(string path, IDictionary<string, object> data, CloudWriteMode mode);
        // The adapter may rerun the callback on contention. Read before writing; no local side effects.
        Task RunTransactionAsync(Func<ICloudTransaction, Task> action);
    }

    public interface ICloudTransaction
    {
        Task<CloudDocumentSnapshot> GetSnapshotAsync(CloudDocumentReference document);
        void Set(CloudDocumentReference document, IDictionary<string, object> data, CloudWriteMode mode = CloudWriteMode.Replace);
        void Delete(CloudDocumentReference document);
    }

    public static class CloudDocumentStoreExtensions
    {
        public static CloudCollectionReference Collection(this ICloudDocumentStore store, string path)
            => new CloudCollectionReference(store, path);
    }
}
