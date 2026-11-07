using System;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class CachedPages
    {
        [SerializeField] PageData[] pages;
        public PageData[] Pages => pages;

        private UIController controller;

        public void Init(UIController controller)
        {
            this.controller = controller;
        }

        public bool HasPage(Type pageType)
        {
            foreach(PageData page in pages)
            {
                UIPage pagePrefab = page.PagePrefab;
                if (pagePrefab == null) continue;
                if (pagePrefab.GetType() == pageType)
                    return true;
            }

            return false;
        }

        public bool HasPage<T>(T page)
        {
            return HasPage(typeof(T));
        }

        public PageData GetPageData(Type pageType)
        {
            foreach (PageData page in pages)
            {
                UIPage pagePrefab = page.PagePrefab;
                if (pagePrefab == null) continue;
                if (pagePrefab.GetType() == pageType)
                    return page;
            }

            return null;
        }

        public UIPage InstantiateCachedPage(Type pageType)
        {
            PageData pageData = GetPageData(pageType);
            if (pageData == null)
            {
                Debug.LogWarning($"[UIController]: Cached page {pageType} was not found. Make sure the UIController is initialized and the page prefab is properly linked!");

                return null;
            }

            if(pageData.PagePrefab == null)
            {
                Debug.LogWarning($"[UIController]: Cached page {pageType} has been registered, but the linked prefab reference is missing!");

                return null;
            }

            UIPage page = GameObject.Instantiate(pageData.PagePrefab, controller.transform);
            page.CacheComponents();

            Canvas prefabCanvas = pageData.PagePrefab.GetComponent<Canvas>();
            if(prefabCanvas != null)
            {
                int sortingOrder = prefabCanvas.sortingOrder;
                if (sortingOrder != 0)
                {
                    Canvas canvas = page.Canvas;
                    Tween.NextFrame(() =>
                    {
                        canvas.overrideSorting = true;
                        canvas.sortingOrder = sortingOrder;
                    });
                }
            }

            page.PreparePage();
            page.Init();
            page.MarkAsCached();

            controller.PagesLink.Add(pageType, page);
            controller.Pages.Add(page);

            return page;
        }

        [System.Serializable]
        public class PageData
        {
            [SerializeField] UIPage pagePrefab;
            public UIPage PagePrefab => pagePrefab;

            [SerializeField] bool autoDestroy = true;
            public bool AutoDestroy => autoDestroy;
        }
    }
}