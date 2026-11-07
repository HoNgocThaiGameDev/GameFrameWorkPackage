using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace NebulaSoft
{
    public class DefineString
    {
        private readonly string BaseDefineLine;

        private List<string> definesList;

        public DefineString()
        {
#if UNITY_6000_0_OR_NEWER
            BaseDefineLine = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            BaseDefineLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif

            definesList = new List<string>(BaseDefineLine.Split(';'));
        }

        public bool HasDefine(string define)
        {
            return definesList.FindIndex(x => x == define) != -1;
        }

        public void RemoveDefine(string define)
        {
            int defineIndex = definesList.FindIndex(x => x == define);
            if (defineIndex == -1)
                return;

            definesList.RemoveAt(defineIndex);
        }

        public void AddDefine(string define)
        {
            int defineIndex = definesList.FindIndex(x => x == define);
            if (defineIndex != -1)
                return;

            definesList.Add(define);
        }

        public string GetDefineLine()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string define in definesList)
            {
                sb.Append(define);
                sb.Append(";");
            }

            return sb.ToString();
        }

        public bool HasChanges()
        {
            return BaseDefineLine != GetDefineLine();
        }

        public void ApplyDefines()
        {
            string newDefineLine = GetDefineLine();

            if (BaseDefineLine != newDefineLine)
            {
#if UNITY_6000_0_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)), newDefineLine);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), newDefineLine);
#endif
            }
        }
    }
}