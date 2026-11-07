using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NebulaSoft
{
    public static class VerboseLogging
    {
        private const string DEFINE_NAME = "DEBUG_LOGS";
        private const string MENU_NAME = "Actions/Verbose Logging";

        static VerboseLogging()
        {
            EditorApplication.delayCall += () => {
                UnityEditor.Menu.SetChecked(MENU_NAME, IsActive);
            };
        }

        [MenuItem(MENU_NAME)]
        private static void ToggleVerboseLogging()
        {
            if(IsActive)
            {
                DefineManager.DisableDefine(DEFINE_NAME);
            }
            else
            {
                DefineManager.EnableDefine(DEFINE_NAME);
            }
        }

        [MenuItem(MENU_NAME, validate = true)]
        private static bool ToggleVerboseLoggingStatus()
        {
            UnityEditor.Menu.SetChecked(MENU_NAME, IsActive);

            return true;
        }

        public static bool IsActive
        {
            get
            {
#if DEBUG_LOGS
                return true;
#else
                return false;
#endif
            }
        }
    }
}
