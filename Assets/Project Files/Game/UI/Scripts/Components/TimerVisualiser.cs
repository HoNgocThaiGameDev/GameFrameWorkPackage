using System;
using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    public class TimerVisualiser : MonoBehaviour
    {
        private const string EXTRA_TIME_TEXT = "+{0} SEC";

        [SerializeField] TMP_Text timerText;
        [SerializeField] SlicedFilledImage fillImage;

        [Header("Extra Time")]
        [SerializeField] TMP_Text extraTimeText;
        [SerializeField] SimpleBounce extraTimeBounce;

        [Space]
        [SerializeField] int defaultYPos = 30;
        [SerializeField] int finalYPos = 60;
        [SerializeField] float duration = 0.5f;
        [SerializeField] float delay = 0.2f;
        [SerializeField] Ease.Type moveEase = Ease.Type.CubicInOut;

        private GameplayTimer timer;
        private TweenCaseCollection tweenCases;

        public void Show(GameplayTimer timer)
        {
            this.timer = timer;

            gameObject.SetActive(true);

            extraTimeBounce.Init(extraTimeText.rectTransform);

            timer.OnTimeSpanChanged += OnTimeChanged;

            OnTimeChanged(timer.CurrentTimeSpan);
        }

        private void OnDestroy()
        {
            if (timer != null)
                timer.OnTimeSpanChanged -= OnTimeChanged;

            tweenCases.KillActive();
        }

        public void Hide()
        {
            gameObject.SetActive(false);

            if (timer != null)
                timer.OnTimeSpanChanged -= OnTimeChanged;
        }

        public void SetFreezeFillAmount(float t)
        {
            fillImage.fillAmount = t;
        }

        public void OnTimeChanged(TimeSpan timeSpan)
        {
            timerText.text = string.Format("{0:mm\\:ss}", timeSpan);
        }

        public void ShowExtraTimeText(int seconds)
        {
            tweenCases.KillActive();

            tweenCases = Tween.BeginTweenCaseCollection();

            extraTimeBounce.Bounce();

            extraTimeText.gameObject.SetActive(true);
            extraTimeText.text = string.Format(EXTRA_TIME_TEXT, seconds);

            extraTimeText.color = extraTimeText.color.SetAlpha(1.0f);
            extraTimeText.DOFade(0.0f, duration, delay);

            RectTransform rectTransform = extraTimeText.rectTransform;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, defaultYPos);
            rectTransform.DOAnchoredPosition(new Vector2(rectTransform.anchoredPosition.x, finalYPos), duration, delay).SetEasing(moveEase).OnComplete(() =>
            {
                extraTimeText.gameObject.SetActive(false);
            });

            Tween.EndTweenCaseCollection();
        }
    }
}
