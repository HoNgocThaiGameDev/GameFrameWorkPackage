using UnityEngine;

namespace VoxelBusters.CoreLibrary.Editor
{

    public partial class AssemblyDefinitionProxy
    {
        private struct AssemblyDefinitionData
        {
            #region Fields

            public string name;

            public string[] references;

            public string[] optionalUnityReferences;

            public string[] includePlatforms;

            public string[] excludePlatforms;

            public bool allowUnsafeCode;

            public bool overrideReferences;

            public string[] precompiledReferences;

            public bool autoReferenced;

            public string[] defineConstraints;

            #endregion

            #region Public methods

            public static AssemblyDefinitionData Load(string dataString)
            {
                return JsonUtility.FromJson<AssemblyDefinitionData>(dataString);
            }

            public string ToJson()
            {
                return JsonUtility.ToJson(this);
            }

            #endregion
        }

    }
}