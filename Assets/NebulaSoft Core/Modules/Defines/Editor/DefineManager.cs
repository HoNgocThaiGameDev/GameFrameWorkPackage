using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build;
#endif

namespace NebulaSoft
{
    public static class DefineManager
    {
        public static bool HasDefine(string define)
        {
#if UNITY_6000_0_OR_NEWER
            string definesLine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            string definesLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif

            return Array.FindIndex(definesLine.Split(';'), x => x == define) != -1;
        }

        public static void EnableDefine(string define)
        {
#if UNITY_6000_0_OR_NEWER
            string defineLine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            string defineLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif

            if (Array.FindIndex(defineLine.Split(';'), x => x == define) != -1)
            {
                return;
            }

            defineLine = defineLine.Insert(0, define + ";");

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)), defineLine);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), defineLine);
#endif
        }

        public static void DisableDefine(string define)
        {
#if UNITY_6000_0_OR_NEWER
            string defineLine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            string defineLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif

            string[] splitedDefines = defineLine.Split(';');

            int tempDefineIndex = Array.FindIndex(splitedDefines, x => x == define);
            string tempDefineLine = "";
            if (tempDefineIndex != -1)
            {
                for (int i = 0; i < splitedDefines.Length; i++)
                {
                    if (i != tempDefineIndex)
                    {
                        defineLine = defineLine.Insert(0, splitedDefines[i]);
                    }
                }
            }

            if (defineLine != tempDefineLine)
            {
#if UNITY_6000_0_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)), tempDefineLine);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), tempDefineLine);
#endif
            }
        }

        public static void CheckAutoDefines(string[] deletedAssets = null)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(CoreEditor.FOLDER_CORE))
            {
                EditorApplication.delayCall += () => { CheckAutoDefines(deletedAssets); };

                return;
            }

            bool CheckDeletedAssets(string filePath)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    if (!deletedAssets.IsNullOrEmpty())
                    {
                        foreach (string deletedAsset in deletedAssets)
                        {
                            if (deletedAsset.EndsWith(filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<DefineState> markedDefines = new List<DefineState>();
            List<RegisteredDefine> registeredDefines = GetDynamicDefines();
            foreach (RegisteredDefine registeredDefine in registeredDefines)
            {
                if(CheckDeletedAssets(registeredDefine.FilePath))
                {
                    markedDefines.Add(new DefineState(registeredDefine.Define, false));

                    continue;
                }

                bool defineFound = false;

                foreach(Assembly assembly in assemblies)
                {
                    Type targetType = assembly.GetType(registeredDefine.AssemblyType, false);
                    if (targetType != null)
                    {
                        defineFound = true;

                        markedDefines.Add(new DefineState(registeredDefine.Define, true));

                        break;
                    }
                }

                if(!defineFound)
                    markedDefines.Add(new DefineState(registeredDefine.Define, false));
            }

            ChangeAutoDefinesState(markedDefines);
        }

        public static void ChangeAutoDefinesState(List<DefineState> defineStates)
        {
            if (EditorApplication.isCompiling)
                return;

            if (defineStates.IsNullOrEmpty())
                return;

            bool definesUpdated = false;

            StringBuilder sb = new StringBuilder();
            sb.Append("[Define Manager]: Dependencies change is detected. Updating Scripting Define Symbols..");
            sb.AppendLine();

            DefineString definesString = new DefineString();
            foreach (DefineState defineState in defineStates)
            {
                if (defineState.State)
                {
                    if (!definesString.HasDefine(defineState.Define))
                    {
                        definesUpdated = true;

                        definesString.AddDefine(defineState.Define);

                        sb.AppendLine();
                        sb.Append(defineState.Define);
                        sb.Append(" - added");
                    }
                }
                else
                {
                    if (definesString.HasDefine(defineState.Define))
                    {
                        definesUpdated = true;

                        definesString.RemoveDefine(defineState.Define);

                        sb.AppendLine();
                        sb.Append(defineState.Define);
                        sb.Append(" - removed");
                    }
                }
            }
            sb.AppendLine();

            if (definesUpdated)
                Debug.Log(sb.ToString());

            definesString.ApplyDefines();
        }

        public static List<RegisteredDefine> GetDynamicDefines()
        {
            //Get assembly
            List<Type> gameTypes = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly != null)
                {
                    try
                    {
                        Type[] tempTypes = assembly.GetTypes();

                        tempTypes = tempTypes.Where(m => m.IsDefined(typeof(DefineAttribute), true)).ToArray();

                        if (!tempTypes.IsNullOrEmpty())
                            gameTypes.AddRange(tempTypes);
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            List<RegisteredDefine> registeredDefines = new List<RegisteredDefine>();
            registeredDefines.AddRange(DefineSettings.STATIC_REGISTERED_DEFINES);

            foreach (Type type in gameTypes)
            {
                //Get attribute
                DefineAttribute[] defineAttributes = (DefineAttribute[])Attribute.GetCustomAttributes(type, typeof(DefineAttribute));

                for (int i = 0; i < defineAttributes.Length; i++)
                {
                    if (!string.IsNullOrEmpty(defineAttributes[i].AssemblyType))
                    {
                        int methodId = registeredDefines.FindIndex(x => x.Define == defineAttributes[i].Define);
                        if (methodId == -1)
                        {
                            registeredDefines.Add(new RegisteredDefine(defineAttributes[i]));
                        }
                    }
                }
            }

            return registeredDefines;
        }
    }
}