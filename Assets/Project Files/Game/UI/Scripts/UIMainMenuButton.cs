#pragma warning disable 0618

using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [System.Serializable]
    public class UIMainMenuButton
    {
        [SerializeField] RectTransform rect;
        [SerializeField] Button button;
        public Button Button => button;
        public bool IsConfigured => rect != null && button != null;

        [Space]
        [SerializeField] AnimationCurve showStoreAdButtonsCurve;
        [SerializeField] AnimationCurve hideStoreAdButtonsCurve;
        [SerializeField] float showHideDuration;

        private float savedRectPosX;
        private float rectXPosBehindOfTheScreen;

        private TweenCase showHideCase;

        public void Init(float rectXPosBehindOfTheScreen)
        {
            this.rectXPosBehindOfTheScreen = rectXPosBehindOfTheScreen;
            savedRectPosX = rect.anchoredPosition.x;
        }

        public void SetActive(bool state)
        {
            if (rect == null)
                return;

            showHideCase.KillActive();
            rect.gameObject.SetActive(state);
        }

        public void Show(bool immediately = false)
        {
            if (showHideCase != null && showHideCase.IsActive) return;

            if (immediately)
            {
                rect.anchoredPosition = rect.anchoredPosition.SetX(savedRectPosX);
                return;
            }

            //RESET
            rect.anchoredPosition = rect.anchoredPosition.SetX(rectXPosBehindOfTheScreen);

            showHideCase = rect.DOAnchoredPosition(rect.anchoredPosition.SetX(savedRectPosX), showHideDuration).SetCurveEasing(showStoreAdButtonsCurve);
        }

        public void Hide(bool immediately = false)
        {
            if (showHideCase != null && showHideCase.IsActive) return;

            if (immediately)
            {
                rect.anchoredPosition = rect.anchoredPosition.SetX(rectXPosBehindOfTheScreen);
                return;
            }

            //RESET
            rect.anchoredPosition = rect.anchoredPosition.SetX(savedRectPosX);

            showHideCase = rect.DOAnchoredPosition(rect.anchoredPosition.SetX(rectXPosBehindOfTheScreen), showHideDuration).SetCurveEasing(hideStoreAdButtonsCurve);
        }
    }
}
