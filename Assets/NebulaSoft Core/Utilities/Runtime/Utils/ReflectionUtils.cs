using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NebulaSoft
{
    public static class ReflectionUtils
    {
        public static readonly BindingFlags FLAGS_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        public static readonly BindingFlags FLAGS_STATIC = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        public static readonly BindingFlags FLAGS_INSTANCE_PRIVATE = BindingFlags.NonPublic | BindingFlags.Instance;
        public static readonly BindingFlags FLAGS_INSTANCE_PUBLIC = BindingFlags.Public | BindingFlags.Instance;

        public static readonly BindingFlags FLAGS_STATIC_PRIVATE = BindingFlags.NonPublic | BindingFlags.Static;
        public static readonly BindingFlags FLAGS_STATIC_PUBLIC = BindingFlags.Public | BindingFlags.Static;

        public static void InjectInstanceComponent<T>(this T instanceObject, string variableName, object value, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            if (instanceObject != null)
            {
                instanceObject.GetType().GetField(variableName, bindingFlags).SetValue(instanceObject, value);
            }
        }

        public static bool FieldExists<T>(T instanceObject, string variableName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            if (instanceObject != null)
            {
                return instanceObject.GetType().GetField(variableName, bindingFlags) != null;
            }

            return false;
        }

        public static void InjectInstanceComponent<T>(string variableName, object value, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance) where T : Object
        {
#if UNITY_6000_0_OR_NEWER
            T component = UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
            T component = UnityEngine.Object.FindObjectOfType<T>(true);
#endif

            if (component != null)
            {
                component.GetType().GetField(variableName, bindingFlags).SetValue(component, value);
            }
        }

        public static void InjectStaticComponent<T>(string variableName, object value, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static) where T : Object
        {
            typeof(T).GetField(variableName, bindingFlags).SetValue(null, value);
        }

        public static object GetInstanceComponent<T>(T instanceObject, string variableName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            if (instanceObject != null)
            {
                return instanceObject.GetType().GetField(variableName, bindingFlags).GetValue(instanceObject);
            }

            return null;
        }

        public static object GetStaticComponent<T>(string variableName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static) where T : Object
        {
            return typeof(T).GetField(variableName, bindingFlags).GetValue(null);
        }

        public static IEnumerable<Type> GetParentTypes(Type type)
        {
            yield return type;

            Type baseType = type.BaseType;
            while (baseType != null)
            {
                yield return baseType;

                baseType = baseType.BaseType;
            }
        }
    }
}