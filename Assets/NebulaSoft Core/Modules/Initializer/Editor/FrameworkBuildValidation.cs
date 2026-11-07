using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NebulaSoft
{
    public sealed class FrameworkBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Validate framework configurations before starting a player build.
            foreach (string guid in AssetDatabase.FindAssets("t:ProjectInitSettings"))
            {
                var settings = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>(AssetDatabase.GUIDToAssetPath(guid));
                var errors = settings.ValidateModules();
                if (errors.Count > 0) throw new BuildFailedException(settings.name + "\n" + string.Join("\n", errors));
            }
            if (!EditorBuildSettings.scenes.Any(scene => scene.enabled))
                throw new BuildFailedException("At least one enabled startup scene is required.");
        }
    }
}
