using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [DisallowMultipleComponent]
    public sealed class QuestTabTransitionAnimation : MonoBehaviour
    {
        [Header("Selected tab")]
        [SerializeField, Range(1f, 1.2f)] float tabZoomMultiplier = 1.025f;
        [SerializeField, Min(0.01f)] float tabZoomDuration = 0.24f;

        [Header("Quest list")]
        [SerializeField, Range(0.75f, 1f)] float itemStartScale = 0.96f;
        [SerializeField, Min(0.01f)] float itemZoomDuration = 0.26f;
        [SerializeField, Min(0f)] float itemStaggerDelay = 0.04f;

        private readonly List<TweenCase> activeTweens = new List<TweenCase>();
        private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

        public void Play(Button selectedTab, IList<QuestElementView> questItems)
        {
            Stop();
            PlayTabZoom(selectedTab);
            PlayQuestListZoom(questItems);
        }

        public void Stop()
        {
            for (int i = 0; i < activeTweens.Count; i++)
                activeTweens[i].KillActive();

            activeTweens.Clear();

            foreach (KeyValuePair<Transform, Vector3> scaleData in originalScales)
            {
                if (scaleData.Key != null)
                    scaleData.Key.localScale = scaleData.Value;
            }

            originalScales.Clear();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void PlayTabZoom(Button selectedTab)
        {
            if (selectedTab == null)
                return;

            Transform target = selectedTab.transform;
            Vector3 targetScale = GetOriginalScale(target);
            target.localScale = targetScale;

            activeTweens.Add(target.DOPushScale(
                targetScale * tabZoomMultiplier,
                targetScale,
                tabZoomDuration * 0.45f,
                tabZoomDuration * 0.55f,
                Ease.Type.SineOut,
                Ease.Type.SineIn,
                unscaledTime: true));
        }

        private void PlayQuestListZoom(IList<QuestElementView> questItems)
        {
            if (questItems == null)
                return;

            for (int i = 0; i < questItems.Count; i++)
            {
                QuestElementView questItem = questItems[i];
                if (questItem == null)
                    continue;

                Transform target = questItem.transform;
                Vector3 targetScale = GetOriginalScale(target);
                target.localScale = targetScale * itemStartScale;

                activeTweens.Add(target.DOScale(
                    targetScale,
                    itemZoomDuration,
                    i * itemStaggerDelay,
                    unscaledTime: true).SetEasing(Ease.Type.SineOut));
            }
        }

        private Vector3 GetOriginalScale(Transform target)
        {
            if (!originalScales.TryGetValue(target, out Vector3 originalScale))
            {
                originalScale = target.localScale;
                originalScales.Add(target, originalScale);
            }

            return originalScale;
        }
    }
}
