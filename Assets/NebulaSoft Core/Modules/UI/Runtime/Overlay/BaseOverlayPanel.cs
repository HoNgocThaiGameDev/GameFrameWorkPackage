using UnityEngine;

namespace NebulaSoft
{
    [RequireComponent(typeof(Canvas))]
    public abstract class BaseOverlayPanel : MonoBehaviour, IOverlayPanel
    {
        [SerializeField] GameObject loadingObject;

        protected Canvas canvas;

        public bool IsActive => canvas.enabled;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        public abstract void Init();
        public abstract void Clear();

        public abstract void Hide(float duration, SimpleCallback onCompleted);
        public abstract void Show(float duration, SimpleCallback onCompleted);

        public virtual void SetLoadingState(bool state)
        {
            if(loadingObject != null)
                loadingObject.SetActive(state);
        }

        public virtual void SetState(bool state)
        {
            canvas.enabled = state;
        }
    }
}
