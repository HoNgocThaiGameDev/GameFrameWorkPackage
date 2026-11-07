using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class SceneSavingHandler
    {
        private static IEnumerable<Type> registeredTypes;

        static SceneSavingHandler()
        {
            EditorSceneManager.sceneSaving += SceneSaving;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;

            Type monobehaviourType = typeof(MonoBehaviour);
            Type type = typeof(ISceneSavingCallback);

            registeredTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => !p.IsAbstract && type.IsAssignableFrom(p) && p.IsSubclassOf(monobehaviourType));
        }

        private static void PlayModeStateChanged(PlayModeStateChange change)
        {
            if(change == PlayModeStateChange.ExitingEditMode)
            {
                RelinkElements();
            }
        }

        private static void SceneSaving(Scene scene, string path)
        {
            RelinkElements();
        }

        private static void RelinkElements()
        {
            foreach (var type in registeredTypes)
            {
#if UNITY_6000_0_OR_NEWER
                UnityEngine.Object[] sceneObjects = GameObject.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                UnityEngine.Object[] sceneObjects = GameObject.FindObjectsOfType(type, true);
#endif

                foreach (UnityEngine.Object sceneObject in sceneObjects)
                {
                    ISceneSavingCallback sceneSavingCallback = (ISceneSavingCallback)sceneObject;
                    if (sceneSavingCallback != null)
                    {
                        sceneSavingCallback.OnSceneSaving();
                    }
                }
            }
        }
    }
}