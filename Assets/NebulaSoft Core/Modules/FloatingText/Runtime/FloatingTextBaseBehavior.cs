using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    public abstract class FloatingTextBaseBehavior : MonoBehaviour
    {
        [SerializeField] protected TMP_Text textRef;

        public SimpleCallback OnAnimationCompleted;

        public virtual void Activate(string text, float scaleMultiplier, Color color)
        {
            textRef.text = text;
            textRef.color = color;

            InvokeCompleteEvent();
        }

        protected void InvokeCompleteEvent()
        {
            OnAnimationCompleted?.Invoke();
        }
    }
}