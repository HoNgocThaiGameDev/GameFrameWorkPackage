using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaSoft.Cloud
{
    public sealed class CloudCollectionReference
    {
        private readonly ICloudDocumentStore store;
        private readonly string path;
        public CloudCollectionReference(ICloudDocumentStore store, string path)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
        }
        public CloudDocumentReference Document(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Contains("/")) throw new ArgumentException("A document ID must be one non-empty path segment.", nameof(id));
            return new CloudDocumentReference(store, path + "/" + id);
        }
    }

    public sealed class CloudDocumentReference
    {
        public ICloudDocumentStore Store { get; }
        public string Path { get; }
        public CloudDocumentReference(ICloudDocumentStore store, string path)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }
        public Task<CloudDocumentSnapshot> GetSnapshotAsync(CloudReadSource source = CloudReadSource.Default)
            => Store.GetSnapshotAsync(Path, source);
        public Task SetAsync(IDictionary<string, object> data, CloudWriteMode mode = CloudWriteMode.Replace)
            => Store.SetAsync(Path, data, mode);
        public CloudCollectionReference Collection(string name) => new CloudCollectionReference(Store, Path + "/" + name);
    }
}
