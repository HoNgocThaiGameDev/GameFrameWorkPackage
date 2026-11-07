using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

#if UNITY_IOS && !UNITY_EDITOR_WIN
using UnityEditor.iOS.Xcode;
#endif

namespace NebulaSoft
{
    public class AdsPostBuild
    {
        [PostProcessBuild(0)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToXcode)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                AddPListValues(pathToXcode);
            }
        }

        static void AddPListValues(string pathToXcode)
        {
#if UNITY_IOS && !UNITY_EDITOR_WIN
            // Get Plist from Xcode project 
            string plistPath = pathToXcode + "/Info.plist";

            // Read in Plist 
            PlistDocument plistObj = new PlistDocument();
            plistObj.ReadFromString(File.ReadAllText(plistPath));

            // set values from the root obj
            PlistElementDict plistRoot = plistObj.root;

            // Set value in plist
            plistRoot.SetString("NSUserTrackingUsageDescription", "Your data will be used to deliver personalized ads to you.");

            // save
            File.WriteAllText(plistPath, plistObj.WriteToString());
#endif
        }

    }
}