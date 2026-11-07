using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NebulaSoft
{
    [RegisterModule("Remote Config")]
    public class RemoteConfigInitModule : InitModule
    {
        [SerializeField, Tooltip("Optional direct HTTPS URL of a JSON object.")]
        private string endpoint;

        [SerializeField, Range(1, 15), Tooltip("Maximum download wait in seconds.")]
        private int timeoutSeconds = 3;

        [SerializeField, Tooltip("Optional bundled JSON used when remote and cache are unavailable.")]
        private TextAsset localConfig;

        public override string ModuleName => "Remote Config";

        public override IEnumerator Prepare()
        {
            RemoteConfigController.SetDefaults(localConfig != null ? localConfig.text : null);
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                if (Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out Uri url) &&
                    url.Scheme == Uri.UriSchemeHttps && url.UserInfo.Length == 0 && url.Fragment.Length == 0)
                {
                    string cacheKey = "NebulaSoft.RemoteConfig.v1." + Hash128.Compute(url.AbsoluteUri);
                    string payload = null;
                    yield return Fetch(url, value => payload = value);

                    if (RemoteConfigController.TryApply(payload))
                    {
                        try
                        {
                            if (PlayerPrefs.GetString(cacheKey) != payload)
                            {
                                PlayerPrefs.SetString(cacheKey, payload);
                                PlayerPrefs.Save();
                            }
                        }
                        catch (PlayerPrefsException)
                        {
                            Debug.LogWarning("[Remote Config] Could not persist the active configuration.");
                        }
                        yield break;
                    }

                    if (payload != null)
                        Debug.LogWarning("[Remote Config] Remote payload was invalid. Using fallback configuration.");
                    if (RemoteConfigController.TryApply(PlayerPrefs.GetString(cacheKey)))
                        yield break;
                }
                else
                {
                    Debug.LogWarning("[Remote Config] Endpoint must be a direct HTTPS URL without credentials or a fragment.");
                }
            }

            if (localConfig != null) RemoteConfigController.Init(localConfig.text);
        }

        public override void CreateComponent() { }
        public override void Shutdown() => RemoteConfigController.Shutdown();

        protected virtual IEnumerator Fetch(Uri url, Action<string> onSuccess)
        {
            using var buffer = new ConfigDownloadHandler();
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = buffer,
                disposeDownloadHandlerOnDispose = false,
                timeout = Mathf.Clamp(timeoutSeconds, 1, 15),
                redirectLimit = 0
            };
            request.SetRequestHeader("Accept", "application/json");
            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (InvalidOperationException)
            {
                Debug.LogWarning("[Remote Config] Could not start download. Using fallback configuration.");
                yield break;
            }
            yield return operation;

            if (request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 && request.responseCode < 300)
            {
                string payload = buffer.ReadUtf8();
                if (payload != null)
                {
                    onSuccess(payload);
                    yield break;
                }
            }

            Debug.LogWarning($"[Remote Config] Download failed ({request.result}, HTTP {request.responseCode}). Using fallback configuration.");
        }

        private sealed class ConfigDownloadHandler : DownloadHandlerScript
        {
            private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
            private readonly MemoryStream data = new MemoryStream();

            public ConfigDownloadHandler() : base(new byte[8192]) { }

            protected override bool ReceiveData(byte[] bytes, int length)
            {
                if (length == 0) return true;
                if (length > RemoteConfigController.MaxPayloadBytes - data.Length) return false;
                data.Write(bytes, 0, length);
                return true;
            }

            public string ReadUtf8()
            {
                try { return Utf8.GetString(data.GetBuffer(), 0, (int)data.Length); }
                catch (DecoderFallbackException) { return null; }
            }
        }
    }
}
