using System.Collections;
using UnityEngine;

namespace NebulaSoft
{
    public class SparksUIAnimation : MonoBehaviour, IUIPageElement
    {
        [SerializeField] SparkData[] sparkData;

        [Space]
        [SerializeField] string poolName = "Sparks UI";

        [Space]
        [MinMaxSlider(0.1f, 2.0f)]
        [SerializeField] Vector2 delay = new Vector2(0.4f, 1.0f);
        [MinMaxSlider(0.1f, 2.0f)]
        [SerializeField] Vector2 scale = new Vector2(0.4f, 1.2f);

        [Space]
        [SerializeField] float scaleInDuration = 0.5f;
        [SerializeField] Ease.Type scaleInEasing = Ease.Type.CubicOut;
        [SerializeField] float scaleOutDuration = 0.4f;
        [SerializeField] Ease.Type scaleOutEasing = Ease.Type.CubicIn;

        private Coroutine sparksCoroutine;

        public void Init(UIPage page)
        {

        }

        private void OnDestroy()
        {
            for (int i = 0; i < sparkData.Length; i++)
            {
                TweenCase tweenCase = sparkData[i].TweenCase;
                if (tweenCase != null)
                {
                    tweenCase.Kill();

                    sparkData[i].TweenCase = null;
                }

                if(sparkData[i].IsBusy)
                {
                    GameObject sparkObject = sparkData[i].SparkObject;
                    if (sparkObject != null)
                    {
                        sparkObject.gameObject.SetActive(false);
                        sparkObject.transform.SetParent(PoolManager.DefaultContainer);

                        sparkData[i].SparkObject = null;
                    }
                }
            }
        }

        public void OnPageStateChanged(bool state)
        {
            if (state)
            {
                if (gameObject.activeInHierarchy && sparkData.Length > 0)
                    sparksCoroutine = StartCoroutine(SparkAnimation());
            }
            else
            {
                if (sparksCoroutine != null)
                    StopCoroutine(sparksCoroutine);
            }
        }

        private IEnumerator SparkAnimation()
        {
            IPool sparkPool = PoolManager.GetPoolByName(poolName);
            WaitForSecondsRealtime waitForSeconds;

            while (true)
            {
                waitForSeconds = new WaitForSecondsRealtime(UnityEngine.Random.Range(delay.x, delay.y));

                int elementIndex = UnityEngine.Random.Range(0, sparkData.Length);
                for(int i = 0; i < sparkData.Length; i++)
                {
                    if (!sparkData[elementIndex].IsBusy)
                        break;

                    elementIndex = (elementIndex + 1) % sparkData.Length;
                }

                SparkData data = sparkData[elementIndex];
                if(!data.IsBusy)
                {
                    data.IsBusy = true;

                    GameObject sparkObject = sparkPool.GetPooledObject();
                    sparkObject.gameObject.SetActive(true);
                    sparkObject.transform.SetParent(transform);
                    sparkObject.transform.localPosition = data.LocalPosition;
                    sparkObject.transform.localScale = Vector3.zero;
                    sparkObject.transform.localRotation = Quaternion.identity;

                    data.SparkObject = sparkObject;
                    data.TweenCase = sparkObject.transform.DOScale(UnityEngine.Random.Range(scale.x, scale.y), scaleInDuration, unscaledTime: true).SetEasing(scaleInEasing).OnComplete(delegate
                    {
                        data.TweenCase = sparkObject.transform.DOScale(0, scaleOutDuration, unscaledTime: true).SetEasing(scaleOutEasing).OnComplete(delegate
                        {
                            sparkObject.SetActive(false);
                            sparkObject.transform.SetParent(PoolManager.DefaultContainer);

                            data.IsBusy = false;
                        });
                    });
                }

                yield return waitForSeconds;
            }
        }

        [System.Serializable]
        public class SparkData
        {
            public Vector3 LocalPosition;

            [System.NonSerialized]
            public GameObject SparkObject;

            [System.NonSerialized]
            public bool IsBusy;

            [System.NonSerialized]
            public TweenCase TweenCase;
        }
    }
}