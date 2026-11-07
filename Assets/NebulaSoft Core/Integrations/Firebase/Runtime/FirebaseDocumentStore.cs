#if FIREBASE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using NebulaSoft.Cloud;

namespace NebulaSoft.Integrations.Firebase
{
    internal sealed class FirebaseDocumentStore : ICloudDocumentStore
    {
        private readonly FirebaseFirestore firestore;
        internal FirebaseDocumentStore(FirebaseFirestore firestore)
        {
            this.firestore = firestore ?? throw new ArgumentNullException(nameof(firestore));
        }
        public async Task<CloudDocumentSnapshot> GetSnapshotAsync(string path, CloudReadSource source)
        {
            try
            {
                var snapshot = await firestore.Document(path).GetSnapshotAsync(source == CloudReadSource.Server ? Source.Server : Source.Default);
                return Snapshot(snapshot);
            }
            catch (Exception exception) { throw ConvertError(exception); }
        }
        public async Task SetAsync(string path, IDictionary<string, object> data, CloudWriteMode mode)
        {
            try { await firestore.Document(path).SetAsync(WriteMap(data), mode == CloudWriteMode.Merge ? SetOptions.MergeAll : SetOptions.Overwrite); }
            catch (Exception exception) { throw ConvertError(exception); }
        }
        public async Task RunTransactionAsync(Func<ICloudTransaction, Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            try { await firestore.RunTransactionAsync(transaction => action(new TransactionAdapter(this, transaction))); }
            catch (Exception exception) { throw ConvertError(exception); }
        }
        private sealed class TransactionAdapter : ICloudTransaction
        {
            private readonly FirebaseDocumentStore owner;
            private readonly Transaction transaction;
            internal TransactionAdapter(FirebaseDocumentStore owner, Transaction transaction) { this.owner = owner; this.transaction = transaction; }
            private DocumentReference Resolve(CloudDocumentReference document)
            {
                if (document == null || !ReferenceEquals(document.Store, owner)) throw new ArgumentException("Document must belong to this transaction's store.", nameof(document));
                return owner.firestore.Document(document.Path);
            }
            public async Task<CloudDocumentSnapshot> GetSnapshotAsync(CloudDocumentReference document)
                => Snapshot(await transaction.GetSnapshotAsync(Resolve(document)));
            public void Set(CloudDocumentReference document, IDictionary<string, object> data, CloudWriteMode mode = CloudWriteMode.Replace)
                => transaction.Set(Resolve(document), WriteMap(data), mode == CloudWriteMode.Merge ? SetOptions.MergeAll : SetOptions.Overwrite);
            public void Delete(CloudDocumentReference document) => transaction.Delete(Resolve(document));
        }
        private static CloudDocumentSnapshot Snapshot(DocumentSnapshot snapshot)
            => new CloudDocumentSnapshot(snapshot.Exists, snapshot.Exists ? ReadMap(snapshot.ToDictionary()) : null);
        private static Dictionary<string, object> ReadMap(IDictionary<string, object> data)
        {
            var result = new Dictionary<string, object>();
            foreach (var pair in data) result.Add(pair.Key, ReadValue(pair.Value));
            return result;
        }
        private static object ReadValue(object value)
        {
            if (value is Timestamp timestamp) return timestamp.ToDateTime().ToUniversalTime();
            if (value is IDictionary<string, object> map) return ReadMap(map);
            if (value is IList list && !(value is byte[]))
            {
                var result = new List<object>(list.Count);
                foreach (var item in list) result.Add(ReadValue(item));
                return result;
            }
            return value; // CloudDocumentSnapshot rejects unsupported native values, never silently drops them.
        }
        private static Dictionary<string, object> WriteMap(IDictionary<string, object> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var result = new Dictionary<string, object>();
            foreach (var pair in data) result.Add(pair.Key, WriteValue(pair.Value));
            return result;
        }
        private static object WriteValue(object value)
        {
            if (value is CloudFieldValue marker && marker == CloudFieldValue.ServerTimestamp) return FieldValue.ServerTimestamp;
            if (value is IDictionary<string, object> map) return WriteMap(map);
            if (value is IList list && !(value is byte[]))
            {
                var result = new List<object>(list.Count);
                foreach (var item in list) result.Add(WriteValue(item));
                return result;
            }
            return value;
        }
        private static Exception ConvertError(Exception exception)
        {
            if (exception is CloudException || exception is OperationCanceledException) return exception;
            if (exception is FirestoreException firestore)
            {
                CloudError error;
                switch (firestore.ErrorCode)
                {
                    case FirestoreError.DeadlineExceeded: error = CloudError.Timeout; break;
                    case FirestoreError.PermissionDenied: error = CloudError.PermissionDenied; break;
                    case FirestoreError.Unavailable: error = CloudError.Offline; break;
                    case FirestoreError.Unauthenticated: error = CloudError.Authentication; break;
                    default: error = CloudError.Unknown; break;
                }
                return new CloudException(error, "Firestore operation failed: " + firestore.ErrorCode, exception);
            }
            // Preserve validation/session errors thrown by the application transaction callback.
            return exception;
        }
    }
}
#endif
