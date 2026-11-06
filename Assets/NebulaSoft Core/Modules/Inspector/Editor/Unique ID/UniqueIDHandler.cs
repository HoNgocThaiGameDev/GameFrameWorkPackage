using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

#if UNITY_6000_6_OR_NEWER
using OwnerObjectId = UnityEngine.EntityId;
#else
using OwnerObjectId = System.Int32;
#endif

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class UniqueIDHandler
    {
        private static Dictionary<string, IDCase> registeredIDS = new Dictionary<string, IDCase>();

        static UniqueIDHandler()
        {
            ClearRegisteredIDs();

            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange change)
        {
            ClearRegisteredIDs();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ClearRegisteredIDs();
        }

        public static bool HasID(string id)
        {
            return registeredIDS.ContainsKey(id);
        }

        public static IDCase GetCase(string id)
        { 
            if(registeredIDS.ContainsKey(id))
            {
                return registeredIDS[id];
            }

            return null;
        }

        public static void RegisterID(IDCase idCase)
        {
            registeredIDS.Add(idCase.ID, idCase);
        }

        public static void ClearRegisteredIDs()
        {
            registeredIDS.Clear();
        }

        public class IDCase
        {
            public string PropertyPath;
            public OwnerObjectId InstanceID;
            public string ID;

            public IDCase(string propertyPath, OwnerObjectId instanceID, string id)
            {
                PropertyPath = propertyPath;
                InstanceID = instanceID;
                ID = id;
            }

            public bool RequireReset(string propertyPath, OwnerObjectId instanceID)
            {
                if(propertyPath.Contains(".Array."))
                {
                    return false;
                }

                return (propertyPath != PropertyPath || instanceID != InstanceID);
            }
        }
    }
}
