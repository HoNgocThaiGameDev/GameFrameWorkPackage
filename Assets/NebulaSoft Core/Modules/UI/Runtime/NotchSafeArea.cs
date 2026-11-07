using UnityEngine;
using System.Collections.Generic;

namespace NebulaSoft
{
    [StaticUnload]
    [System.Serializable]
    public class NotchSaveArea
    {
        private static NotchSaveArea notchSaveArea;

        [SerializeField] RectTransform[] safePanels;

        [Space]
        [SerializeField] bool conformX = true;
        [SerializeField] bool conformY = true;

        private static List<RectTransform> registeredTransforms = new List<RectTransform>();

        private static Rect lastSafeArea = new Rect(0, 0, 0, 0);
        private static Vector2Int lastScreenSize = new Vector2Int(0, 0);

        private static Vector2 virtualResolution;

        private static ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        public static float TopOffset { get; private set; } = 0;

        public void Init(Vector2 virtualResolution)
        {
            notchSaveArea = this;

            NotchSaveArea.virtualResolution = virtualResolution;

            if (!safePanels.IsNullOrEmpty())
            {
                registeredTransforms.AddRange(safePanels);
            }

            Refresh();
        }

        public static void RegisterRectTransform(RectTransform rectTransform)
        {
            registeredTransforms.Add(rectTransform);

            if(notchSaveArea != null)
                Refresh(true);
        }

        public static void Refresh(bool forceRefresh = false)
        {
            if (notchSaveArea == null) return;

            Rect safeArea = Screen.safeArea;

            if (safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y || Screen.orientation != lastOrientation || forceRefresh)
            {
                lastScreenSize.x = Screen.width;
                lastScreenSize.y = Screen.height;
                lastOrientation = Screen.orientation;

                ApplySafeArea(safeArea);
            }
        }

        private static void ApplySafeArea(Rect rect)
        {
            if (notchSaveArea == null)
                return;

            lastSafeArea = rect;

            // Ignore x-axis?
            if (!notchSaveArea.conformX)
            {
                rect.x = 0;
                rect.width = Screen.width;
            }

            // Ignore y-axis?
            if (!notchSaveArea.conformY)
            {
                rect.y = 0;
                rect.height = Screen.height;
            }

            // Check for invalid screen startup state on some Samsung devices (see below)
            if (Screen.width > 0 && Screen.height > 0)
            {
                // Convert safe area rectangle from absolute pixels to normalised anchor coordinates
                Vector2 anchorMin = rect.position;
                Vector2 anchorMax = rect.position + rect.size;

                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;

                TopOffset = virtualResolution.y * (1.0f - anchorMax.y);

                // Fix for some Samsung devices (e.g. Note 10+, A71, S20) where Refresh gets called twice and the first time returns NaN anchor coordinates
                if (anchorMin.x >= 0 && anchorMin.y >= 0 && anchorMax.x >= 0 && anchorMax.y >= 0)
                {
                    for (int i = 0; i < registeredTransforms.Count; i++)
                    {
                        if(registeredTransforms[i] != null)
                        {
                            registeredTransforms[i].anchorMin = anchorMin;
                            registeredTransforms[i].anchorMax = anchorMax;
                        }
                        else
                        {
                            registeredTransforms.RemoveAt(i);

                            i--;
                        }
                    }
                }
            }
        }

        private static void UnloadStatic()
        {
            TopOffset = 0;

            lastSafeArea = new Rect(0, 0, 0, 0);
            lastScreenSize = new Vector2Int(0, 0);

            virtualResolution = Vector2.zero;
            lastOrientation = ScreenOrientation.AutoRotation;

            registeredTransforms?.Clear();
        }
    }
}
