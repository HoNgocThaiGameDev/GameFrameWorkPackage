using UnityEngine;

namespace NebulaSoft.IAPStore
{
    public class DefaultStoreElement : IStoreElement
    {
        private RectTransform rectTransform;
        private TweenCase tweenCase;

        private GameObject gameObject;

        public bool IsActive => gameObject != null && gameObject.activeSelf;
        public float Height => rectTransform.sizeDelta.y;

        private SimpleCallback completeCallback;

        public DefaultStoreElement(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;

            gameObject = rectTransform.gameObject;
        }
        
        public void Init()
        {

        }

        public void KillTweenCases()
        {
            tweenCase.KillActive();
        }

        public void PlayAnimation(int elementIndex)
        {
            rectTransform.localScale = Vector3.zero;

            tweenCase = rectTransform.DOScale(1.0f, 0.3f, elementIndex * 0.05f, unscaledTime: true).SetEasing(Ease.Type.CircOut).OnComplete(completeCallback);
        }

        public void OnComplete(SimpleCallback completeCallback)
        {
            this.completeCallback = completeCallback;
        }
    }
}