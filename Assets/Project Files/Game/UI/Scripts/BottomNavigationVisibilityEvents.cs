using System;
using UnityEngine;

namespace NebulaSoft
{
    public static class BottomNavigationVisibilityEvents
    {
        public static event Action ShowRequested;
        public static event Action HideRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ShowRequested = null;
            HideRequested = null;
        }

        public static void RequestShow()
        {
            ShowRequested?.Invoke();
        }

        public static void RequestHide()
        {
            HideRequested?.Invoke();
        }
    }
}
