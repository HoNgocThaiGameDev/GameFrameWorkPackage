using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class SceneObject : ISerializationCallbackReceiver
    {
        [SerializeField] Object scene;

        [SerializeField] string path;
        public string Path => path;

        [SerializeField] string name;
        public string Name => name;

        public void OnAfterDeserialize()
        {

        }

        public void OnBeforeSerialize()
        {
            if(scene != null)
            {
                name = scene.name;

#if UNITY_EDITOR
                path = UnityEditor.AssetDatabase.GetAssetPath(scene);
#endif

                return;
            }

            name = "";
            path = "";
        }
    }
}
