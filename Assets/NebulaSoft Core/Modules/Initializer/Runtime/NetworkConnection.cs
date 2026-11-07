using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Networking;

namespace NebulaSoft
{
    public enum NetworkTransportKind
    {
        Unknown,
        WiFi,
        MobileData
    }

    public sealed class NetworkCheckResult
    {
        public bool IsReachable { get; }
        public NetworkReachability Transport { get; }
        public NetworkTransportKind TransportKind { get; }
        public long ResponseCode { get; }
        public bool TimedOut { get; }
        public string Error { get; }

        public NetworkCheckResult(
            bool isReachable,
            NetworkReachability transport,
            long responseCode,
            bool timedOut,
            string error)
        {
            IsReachable = isReachable;
            Transport = transport;
            TransportKind = GetTransportKind(transport);
            ResponseCode = responseCode;
            TimedOut = timedOut;
            Error = error;
        }

        private static NetworkTransportKind GetTransportKind(NetworkReachability transport)
        {
            switch (transport)
            {
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    return NetworkTransportKind.WiFi;

                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return NetworkTransportKind.MobileData;

                default:
                    return NetworkTransportKind.Unknown;
            }
        }
    }

    public class NetworkConnection
    {
        public const string ServiceProbeUrl = "https://example.com/";

        private readonly string serverUrl;

        public NetworkConnection(string url)
        {
            serverUrl = url;
        }

        public IEnumerator CheckConnection(Action<bool> onConnectionChecked)
        {
            bool isReachable = false;
            yield return CheckConnectionDetailed(result => isReachable = result.IsReachable);
            onConnectionChecked?.Invoke(isReachable);
        }

        public IEnumerator CheckConnectionDetailed(Action<NetworkCheckResult> onConnectionChecked)
        {
            NetworkReachability transport = Application.internetReachability;
            using (UnityWebRequest request = UnityWebRequest.Head(serverUrl))
            {
                request.timeout = 5;

                yield return request.SendWebRequest();

                string error = request.error;
                bool timedOut = IsTimeoutError(error);
                bool isReachable = IsResponseReachable(request.result, timedOut);
                onConnectionChecked?.Invoke(new NetworkCheckResult(
                    isReachable,
                    transport,
                    request.responseCode,
                    timedOut,
                    error));
            }
        }

        public static bool IsResponseReachable(UnityWebRequest.Result result)
        {
            return result != UnityWebRequest.Result.ConnectionError
                && result != UnityWebRequest.Result.DataProcessingError;
        }

        public static bool IsResponseReachable(UnityWebRequest.Result result, bool timedOut)
        {
            return !timedOut && IsResponseReachable(result);
        }

        public static bool IsTimeoutError(string error)
        {
            return !string.IsNullOrEmpty(error)
                && error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
        }

    }
}
