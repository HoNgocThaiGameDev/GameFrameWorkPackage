using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class TweenExtensions
    {
        public static bool ExistsAndActive(this TweenCase tweenCase)
        {
            return tweenCase != null && tweenCase.IsActive;
        }

        public static bool KillActive(this TweenCase tweenCase)
        {
            if (tweenCase != null && tweenCase.IsActive)
            {
                tweenCase.Kill();

                return true;
            }

            return false;
        }

        public static void KillActive(this TweenCase[] tweenCases)
        {
            if(tweenCases != null)
            {
                foreach (TweenCase tweenCase in tweenCases)
                {
                    if (tweenCase != null && tweenCase.IsActive)
                    {
                        tweenCase.Kill();
                    }
                }
            }
        }

        public static bool KillActive(this TweenCaseCollection tweenCase)
        {
            if (tweenCase != null && !tweenCase.IsComplete())
            {
                tweenCase.Kill();

                return true;
            }

            return false;
        }
        
        public static bool CompleteActive(this TweenCase tweenCase)
        {
            if (tweenCase != null && tweenCase.IsActive && !tweenCase.IsCompleted)
            {
                tweenCase.Complete();

                return true;
            }

            return false;
        }

        public static void CompleteActive(this TweenCase[] tweenCases)
        {
            if (tweenCases != null)
            {
                foreach (TweenCase tweenCase in tweenCases)
                {
                    if (tweenCase != null && tweenCase.IsActive)
                    {
                        tweenCase.Complete();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenCase DoSnapTargetBottom(this ScrollRect scrollRect, RectTransform target, float duration, float offsetX = 0, float offsetY = 0)
        {
            var targetPosition = target.position + Vector3.up * (scrollRect.viewport.rect.height / 2 + target.sizeDelta.y);
            return scrollRect.SnapToTarget(targetPosition, duration, offsetX, offsetY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenCase DoSnapTargetTop(this ScrollRect scrollRect, RectTransform target, float duration, float offsetX = 0, float offsetY = 0)
        {
            return scrollRect.SnapToTarget(target.position, duration, offsetX, offsetY);
        }

        public static TweenCase SnapToTarget(this ScrollRect scrollRect, Vector3 target, float duration, float offsetX = 0, float offsetY = 0)
        {
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            var content = scrollRect.content;
            Vector3 targetInViewport = viewport.InverseTransformPoint(target);
            Vector3 viewportDelta = new Vector3(-targetInViewport.x - offsetX, -targetInViewport.y - offsetY, 0);
            Vector3 worldDelta = viewport.TransformVector(viewportDelta);
            Vector3 parentDelta = content.parent != null ? content.parent.InverseTransformVector(worldDelta) : worldDelta;
            Vector2 destination = content.anchoredPosition;
            if (scrollRect.horizontal) destination.x += parentDelta.x;
            if (scrollRect.vertical) destination.y += parentDelta.y;
            return content.DOAnchoredPosition(destination, duration);
        }
    }
}
