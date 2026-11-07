using UnityEngine;

namespace NebulaSoft.IAPStore
{
    public class GroupStoreElement : MonoBehaviour, IStoreElement
    {
        public bool IsActive => gameObject.activeSelf;
        public float Height => rectTransform.sizeDelta.y;

        private RectTransform rectTransform;

        private Transform[] groupElements;
        private TweenCase[] tweenCases;

        private SimpleCallback completeCallback;

        public void Init()
        {
            rectTransform = (RectTransform)transform;

            groupElements = new Transform[transform.childCount];
            tweenCases = new TweenCase[groupElements.Length];
            for (int i = 0; i < groupElements.Length; i++)
            {
                groupElements[i] = transform.GetChild(i);
            }
        }

        public void KillTweenCases()
        {
            tweenCases.KillActive();
        }

        public void OnComplete(SimpleCallback completeCallback)
        {
            this.completeCallback = completeCallback;
        }

        public void PlayAnimation(int elementIndex)
        {
            for(int i = 0; i < groupElements.Length; i++)
            {
                groupElements[i].localScale = Vector3.zero;

                tweenCases[i] = groupElements[i].DOScale(1.0f, 0.3f, elementIndex * 0.05f, unscaledTime: true).SetEasing(Ease.Type.CircOut);
            }

            tweenCases[^1].OnComplete(completeCallback);
        }
    }
}