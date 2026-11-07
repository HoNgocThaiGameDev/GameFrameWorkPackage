using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

#if UNITY_IOS && !UNITY_EDITOR_WIN
using UnityEditor.iOS.Xcode;
#endif

namespace NebulaSoft
{
    public class HapticiOSPostProcessor
    {
#if UNITY_IOS && !UNITY_EDITOR_WIN
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                // Initialize the PBXProject
                string projectPath = PBXProject.GetPBXProjectPath(path);
                PBXProject project = new PBXProject();
                project.ReadFromFile(projectPath);

                // Get the UnityFramework target GUID
                string targetGuid = project.GetUnityFrameworkTargetGuid();

                // Add CoreHaptics.framework
                project.AddFrameworkToProject(targetGuid, "CoreHaptics.framework", false);

                // Write the modified project back to the file
                File.WriteAllText(projectPath, project.WriteToString());
            }
        }
#endif
    }
}
