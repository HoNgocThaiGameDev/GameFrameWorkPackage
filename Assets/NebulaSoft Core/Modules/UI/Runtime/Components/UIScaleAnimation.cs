using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class UIScaleAnimation
    {
        [SerializeField] Transform transform;
        public Transform Transform => transform;

        private TweenCase scaleTweenCase;

        public UIScaleAnimation(Transform transform)
        {
            this.transform = transform;
        }

        public UIScaleAnimation(GameObject gameObject) : this(gameObject.transform) { }
        public UIScaleAnimation(Component component) : this(component.transform) { }

        public void Show(float scaleMultiplier = 1.1f, float duration = 0.5f, float delay = 0f, bool immediately = false, SimpleCallback onCompleted = null)
        {
            if(transform == null)
            {
                Debug.LogError("Transform value cannot be null. Please ensure it is assigned in the Inspector or passed through the constructor.");

                onCompleted?.Invoke();

                return;
            }

            scaleTweenCase.KillActive();

            if (immediately)
            {
                transform.localScale = Vector3.one;

                onCompleted?.Invoke();

                return;
            }

            // RESET
            transform.localScale = Vector3.zero;
            scaleTweenCase = transform.DOPushScale(Vector3.one * scaleMultiplier, Vector3.one, duration * 0.64f, duration * 0.36f, Ease.Type.CubicOut, Ease.Type.CubicIn, delay).OnComplete(() =>
            {
                onCompleted?.Invoke();
            });
        }

        public void Hide(float scaleMultiplier = 1.1f, float duration = 0.5f, float delay = 0f, bool immediately = false, SimpleCallback onCompleted = null)
        {
            if (transform == null)
            {
                Debug.LogError("Transform value cannot be null. Please ensure it is assigned in the Inspector or passed through the constructor.");

                onCompleted?.Invoke();

                return;
            }

            scaleTweenCase.KillActive();

            if (immediately)
            {
                transform.localScale = Vector3.zero;
                onCompleted?.Invoke();

                return;
            }

            scaleTweenCase = transform.DOPushScale(Vector3.one * scaleMultiplier, Vector3.zero, duration * 0.36f, duration * 0.64f, Ease.Type.CubicOut, Ease.Type.CubicIn, delay).OnComplete(() =>
            {
                onCompleted?.Invoke();
            });
        }

        public void Unload()
        {
            scaleTweenCase?.KillActive();
        }
    }
}