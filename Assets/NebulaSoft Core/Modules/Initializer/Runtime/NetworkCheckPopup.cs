using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Canvas))]
    public class NetworkCheckPopup : MonoBehaviour
    {
        [SerializeField] Button retryButton;
        [SerializeField] GameObject textObject;
        [SerializeField] GameObject loadingObject;

        private Canvas canvas;

        private Coroutine checkCoroutine;
        private NetworkConnection networkConnection;

        private bool isLoading;

        private static bool forceCheck;

        public void Init()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = false;

            DontDestroyOnLoad(gameObject);

            textObject.SetActive(true);
            loadingObject.SetActive(false);

            CanvasScaler canvasScaler = GetComponent<CanvasScaler>();
            canvasScaler.MatchSize();

            retryButton.onClick.AddListener(OnRetryButtonClicked);
            UIAudioFeedback.RegisterButton(retryButton);
            UIHapticFeedback.RegisterButton(retryButton);

            networkConnection = new NetworkConnection(NetworkConnection.ServiceProbeUrl);

            checkCoroutine = StartCoroutine(CheckCoroutine());
        }

        private IEnumerator CheckCoroutine()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(5.0f);

            while (true)
            {
                yield return wait;

                if(Application.internetReachability == NetworkReachability.NotReachable || forceCheck)
                {
                    bool isConnected = false;
                    IEnumerator connectionCheck = networkConnection.CheckConnection((state) => isConnected = state);

                    yield return connectionCheck;

                    if(!isConnected)
                    {
                        float storedTimeScale = Time.timeScale;

                        Time.timeScale = 0;
                        canvas.enabled = true;
                        
                        while(canvas.enabled)
                        {
                            yield return null;
                        }

                        Time.timeScale = storedTimeScale;
                    }
                }
            }
        }

        private void OnRetryButtonClicked()
        {
            if (isLoading) return;

            StartCoroutine(RetryCoroutine());
        }

        private IEnumerator RetryCoroutine()
        {
            isLoading = true;

            textObject.SetActive(false);
            loadingObject.SetActive(true);

            bool isConnected = false;

            IEnumerator connectionCheck = networkConnection.CheckConnection((state) => isConnected = state);

            yield return connectionCheck;

            if (isConnected)
            {
                canvas.enabled = false;
                isLoading = false;
            }
            else
            {
                textObject.SetActive(true);
                loadingObject.SetActive(false);
            }

            isLoading = false;
        }
    }
}
