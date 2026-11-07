using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomEditor(typeof(RemoteConfigHandler), true)]
    public class RemoteConfigHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawCopyConfigButton();
        }

        internal static void DrawCopyConfigButton()
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Copy Example JSON"))
                EditorGUIUtility.systemCopyBuffer = GetAllConfigsAsJson();
        }

        public static string GetAllConfigsAsJson()
        {
            var configObjects = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Type type in TypeCache.GetTypesDerivedFrom<RemoteConfigData>())
            {
                if (type.IsAbstract || type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                var instance = (RemoteConfigData)Activator.CreateInstance(type);
                var settings = new JsonSerializerSettings
                {
                    Formatting = instance.PrettyPrint ? Formatting.Indented : Formatting.None,
                    NullValueHandling = NullValueHandling.Ignore
                };
                configObjects[instance.Key] = JsonConvert.SerializeObject(instance, settings);
            }

            return "{\n" + ToPseudoJsonBody(configObjects) + "\n}";
        }

        public static string ToPseudoJsonBody(Dictionary<string, string> dict)
        {
            var entries = new List<string>();
            foreach (var item in dict)
                entries.Add($"{JsonConvert.ToString(item.Key)}: {item.Value}");

            return string.Join(",\n", entries);
        }
    }

    [CustomEditor(typeof(RemoteConfigInitModule), true)]
    public class RemoteConfigInitModuleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RemoteConfigHandlerEditor.DrawCopyConfigButton();
        }
    }
}
