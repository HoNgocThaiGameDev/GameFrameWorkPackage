using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NebulaSoft
{
    public static class SmartSelector
    {
        [MenuItem("GameObject/Selectability/Select First Child", false, 0)]
        public static void SelectFirstChild(MenuCommand menuCommand)
        {
            if (CallOnlyOnce(menuCommand))
                return;

            Object[] selectedGameObjects = Selection.objects;
            List<Object> tempObjects = new List<Object>();
            for (int i = 0; i < selectedGameObjects.Length; i++)
            {
                if (selectedGameObjects[i] is GameObject)
                {
                    GameObject tempGameObject = selectedGameObjects[i] as GameObject;
                    if (tempGameObject.transform.childCount > 0)
                    {
                        Transform child = tempGameObject.transform.GetChild(0);
                        if (child != null)
                        {
                            tempObjects.Add(child.gameObject);
                        }
                    }
                }
            }

            if (tempObjects.Count > 0)
                Selection.objects = tempObjects.ToArray();
        }

        [MenuItem("GameObject/Selectability/Select First Child", true, 0)]
        public static bool SelectFirstChildValidation()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/Selectability/Select Last Child", false, 0)]
        public static void SelectLastChild(MenuCommand menuCommand)
        {
            if (CallOnlyOnce(menuCommand))
                return;

            Object[] selectedGameObjects = Selection.objects;
            List<Object> tempObjects = new List<Object>();
            for (int i = 0; i < selectedGameObjects.Length; i++)
            {
                if (selectedGameObjects[i] is GameObject)
                {
                    GameObject tempGameObject = selectedGameObjects[i] as GameObject;
                    if (tempGameObject.transform.childCount > 0)
                    {
                        Transform child = tempGameObject.transform.GetChild(tempGameObject.transform.childCount - 1);
                        if (child != null)
                        {
                            tempObjects.Add(child.gameObject);
                        }
                    }
                }
            }

            if (tempObjects.Count > 0)
                Selection.objects = tempObjects.ToArray();
        }

        [MenuItem("GameObject/Selectability/Select Last Child", true, 0)]
        public static bool SelectLastChildValidation()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/Selectability/Select Parent", false, 0)]
        public static void SelectParent(MenuCommand menuCommand)
        {
            if (CallOnlyOnce(menuCommand))
                return;

            Object[] selectedGameObjects = Selection.objects;
            List<Object> tempObjects = new List<Object>();
            for (int i = 0; i < selectedGameObjects.Length; i++)
            {
                if (selectedGameObjects[i] is GameObject)
                {
                    GameObject tempGameObject = selectedGameObjects[i] as GameObject;
                    if (tempGameObject.transform.parent != null)
                    {
                        tempObjects.Add(tempGameObject.transform.parent.gameObject);
                    }
                }
            }

            if (tempObjects.Count > 0)
                Selection.objects = tempObjects.ToArray();
        }

        [MenuItem("GameObject/Selectability/Select Parent", true, 0)]
        public static bool SelectParentValidation()
        {
            return Selection.activeGameObject != null;
        }

        private static bool CallOnlyOnce(MenuCommand menuCommand)
        {
            if (Selection.gameObjects.Length > 1)
            {
                if (menuCommand.context != Selection.objects[0])
                {
                    return true;
                }
            }

            return false;
        }
    }
}