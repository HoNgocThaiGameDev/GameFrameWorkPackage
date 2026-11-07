using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NebulaSoft;

public class UIOfferIndicator : MonoBehaviour
{
    [SerializeField] Image fillImage;

    public bool IsAnimationActive => animationTween != null && animationTween.IsActive;
    public event SimpleCallback AnimationCompleted;

    private TweenCase animationTween;
    private float animationDuration;

    public void Init(float animationDuration)
    {
        this.animationDuration = animationDuration;

        StopAnimation(false);
    }

    public void PlayAnimation()
    {
        StopAnimation(false);

        fillImage.fillAmount = 0;
        fillImage.transform.localScale = Vector3.one;

        animationTween = fillImage.DOFillAmount(1f, animationDuration).OnComplete(() =>
        {
            AnimationCompleted?.Invoke();
        });
    }

    public void StopAnimation(bool doBounce = true)
    {
        animationTween.KillActive();

        if (doBounce)
        {
            animationTween = fillImage.DOPushScale(1.1f, 0.5f, 0.3f, 0.2f, Ease.Type.SineIn, Ease.Type.SineOut).OnComplete(() =>
            {
                fillImage.fillAmount = 0;
            });
        }
        else
        {
            fillImage.fillAmount = 0;
        }
    }
}
