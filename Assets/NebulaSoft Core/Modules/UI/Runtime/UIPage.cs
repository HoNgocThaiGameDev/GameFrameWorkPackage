using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Canvas)), RequireComponent(typeof(GraphicRaycaster))]
    public abstract class UIPage : MonoBehaviour, ISceneSavingCallback
    {
        [Hide]
        [SerializeField] Component[] registeredElements;

        protected bool isPageDisplayed;
        public bool IsPageDisplayed { get => isPageDisplayed; set => isPageDisplayed = value; }

        protected Canvas canvas;
        public Canvas Canvas => canvas;

        protected GraphicRaycaster graphicRaycaster;
        public GraphicRaycaster GraphicRaycaster => graphicRaycaster;

        private string defaultName;

        private IUIPageElement[] pageElements;

        protected bool isCached;
        public bool IsCached => isCached;

        public void CacheComponents()
        {
            defaultName = name;

            canvas = GetComponent<Canvas>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();
        }

        public void PreparePage()
        {
            isPageDisplayed = false;
            canvas.enabled = false;

            pageElements = new IUIPageElement[registeredElements.Length];
            if(!pageElements.IsNullOrEmpty())
            {
                for (int i = 0; i < pageElements.Length; i++)
                {
                    pageElements[i] = (IUIPageElement)registeredElements[i];
                    pageElements[i].Init(this);
                }
            }
        }

        public abstract void Init();

        public void EnableCanvas()
        {
            isPageDisplayed = true;

            canvas.enabled = true;

            if(!pageElements.IsNullOrEmpty())
            {
                for (int i = 0; i < pageElements.Length; i++)
                {
                    pageElements[i]?.OnPageStateChanged(true);
                }
            }

#if UNITY_EDITOR
            name = string.Format("{0} (Active)", defaultName);
#endif
        }

        public void DisableCanvas()
        {
            isPageDisplayed = false;

            canvas.enabled = false;

            if (!pageElements.IsNullOrEmpty())
            {
                for (int i = 0; i < pageElements.Length; i++)
                {
                    pageElements[i]?.OnPageStateChanged(false);
                }
            }

#if UNITY_EDITOR
            name = defaultName;
#endif
        }

        public abstract void PlayShowAnimation();
        public abstract void PlayHideAnimation();

        public virtual void Unload()
        {
            isPageDisplayed = false;

            canvas.enabled = false;
        }

        public void MarkAsCached()
        {
            isCached = true;
        }

        public bool OnPrefabSaving()
        {
            Component[] cachedPageElements = GetComponentsInChildren(typeof(IUIPageElement));

            if(registeredElements == null || registeredElements.Length != cachedPageElements.Length)
            {
                registeredElements = cachedPageElements;

                return true;
            }

            for(int i = 0; i < registeredElements.Length; i++)
            {
                if (registeredElements[i] == null)
                {
                    registeredElements = cachedPageElements;

                    return true;
                }
            }

            for (int i = 0; i < cachedPageElements.Length; i++)
            {
                if(!ReferenceEquals(registeredElements[i], cachedPageElements[i]))
                {
                    registeredElements = cachedPageElements;

                    return true;
                }
            }

            return false;
        }

        public void OnSceneSaving()
        {
            void SaveElements(Component[] elements)
            {
                registeredElements = elements;

                RuntimeEditorUtils.SetDirty(this);
            }

            Component[] cachedPageElements = GetComponentsInChildren(typeof(IUIPageElement));

            if (registeredElements == null || registeredElements.Length != cachedPageElements.Length)
            {
                SaveElements(cachedPageElements);

                return;
            }

            for (int i = 0; i < registeredElements.Length; i++)
            {
                if (registeredElements[i] == null)
                {
                    SaveElements(cachedPageElements);

                    return;
                }
            }

            for (int i = 0; i < cachedPageElements.Length; i++)
            {
                if (!ReferenceEquals(registeredElements[i], cachedPageElements[i]))
                {
                    SaveElements(cachedPageElements);

                    return;
                }
            }
        }
    }
}
