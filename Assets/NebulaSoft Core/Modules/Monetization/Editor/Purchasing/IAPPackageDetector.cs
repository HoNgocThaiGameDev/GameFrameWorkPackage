using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class IAPPackageDetector
    {
        static ListRequest listRequest;

        static IAPPackageDetector()
        {
            listRequest = Client.List(true);
            EditorApplication.update += CheckList;
        }

        static void CheckList()
        {
            if (!listRequest.IsCompleted)
                return;

            EditorApplication.update -= CheckList;

            UnityEditor.PackageManager.PackageInfo iap = listRequest.Result.FirstOrDefault(p => p.name == "com.unity.purchasing");
            if (iap != null)
            {
                string version = iap.version;
                if(version.StartsWith("5"))
                {
#if !UNITY_IAP_NEW
                    DefineManager.EnableDefine("UNITY_IAP_NEW");

                    Debug.Log($"Detected IAP version: {iap.version}");
#endif
                }
                else
                {
#if UNITY_IAP_NEW
                    DefineManager.DisableDefine("UNITY_IAP_NEW");
#endif
                }
            }
            else
            {
#if UNITY_IAP_NEW
                DefineManager.DisableDefine("UNITY_IAP_NEW");
#endif
            }
        }
    }
}
