using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class StaticUnloader
    {
        private const string METHOD_NAME = "UnloadStatic";

        private static IEnumerable<Type> registeredElements;

        static StaticUnloader()
        {
            registeredElements = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => t.GetCustomAttributes(typeof(StaticUnloadAttribute), true).Length > 0);

            EditorApplication.playModeStateChanged += ModeChanged;
        }

        private static void ModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += UnloadStaticElements;
            }
        }

        private static void UnloadStaticElements()
        {
            foreach (Type element in registeredElements)
            {
                MethodInfo methodInfo = element.GetMethod(METHOD_NAME, ReflectionUtils.FLAGS_STATIC);
                if(methodInfo != null)
                {
                    methodInfo.Invoke(null, null);
                }
                else
                {
                    Debug.LogWarning($"The StaticUnloadAttribute is attached to the {element.FullName} class, but {METHOD_NAME} method is not declared!");
                }
            }
        }
    }
}

