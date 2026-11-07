using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public abstract class SettingsButtonBase : MonoBehaviour
    {
        public RectTransform RectTransform { get; protected set; }
        public Button Button { get; protected set; }

        public bool IsSelected { get; protected set; }

        private void Awake()
        {
            RectTransform = (RectTransform)transform;

            Button = GetComponent<Button>();
            Button.onClick.AddListener(OnClick);

            IsSelected = false;

            Deselect();

            Init();
        }

        public abstract void Init();
        public abstract void OnClick();

        public virtual void Select()
        {
            IsSelected = true;
        }

        public virtual void Deselect()
        {
            IsSelected = false;
        }
    }
}