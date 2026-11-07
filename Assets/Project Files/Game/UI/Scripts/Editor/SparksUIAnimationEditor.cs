using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomEditor(typeof(SparksUIAnimation))]
    public class SparksUIAnimationEditor : UnityEditor.Editor
    {
        private SparksUIAnimation sparksUI;

        private void OnEnable()
        {
            sparksUI = (SparksUIAnimation)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            if (GUILayout.Button("Pick Transforms"))
            {
                // Collect all direct child transforms
                System.Collections.Generic.List<Transform> childList = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in sparksUI.transform)
                {
                    childList.Add(child);
                }

                if(childList.Count == 0)
                {
                    Debug.LogWarning("No child transforms found to pick.");

                    return;
                }

                Undo.RecordObject(sparksUI, "Pick Transforms");

                // Prepare new SparkData array
                SparksUIAnimation.SparkData[] sparkDataArray = new SparksUIAnimation.SparkData[childList.Count];
                for (int i = 0; i < childList.Count; i++)
                {
                    SparksUIAnimation.SparkData data = new SparksUIAnimation.SparkData();
                    data.LocalPosition = childList[i].localPosition;

                    sparkDataArray[i] = data;
                }

                // Assign to serialized property
                SerializedObject so = new SerializedObject(sparksUI);
                SerializedProperty sparkDataProp = so.FindProperty("sparkData");
                sparkDataProp.arraySize = sparkDataArray.Length;
                for (int i = 0; i < sparkDataArray.Length; i++)
                {
                    SerializedProperty element = sparkDataProp.GetArrayElementAtIndex(i);

                    element.FindPropertyRelative("LocalPosition").vector3Value = sparkDataArray[i].LocalPosition;

                    // LinkedTransform is [NonSerialized], skip
                }
                so.ApplyModifiedProperties();

                // Destroy child objects
                for (int i = 0; i < childList.Count; i++)
                {
                    Undo.DestroyObjectImmediate(childList[i].gameObject);
                }
            }

            if (GUILayout.Button("Spawn Transforms"))
            {
                Undo.RecordObject(sparksUI, "Spawn Transforms");

                SerializedObject so = new SerializedObject(sparksUI);
                SerializedProperty sparkDataProp = so.FindProperty("sparkData");

                for (int i = 0; i < sparkDataProp.arraySize; i++)
                {
                    SerializedProperty element = sparkDataProp.GetArrayElementAtIndex(i);
                    Vector3 localPos = element.FindPropertyRelative("LocalPosition").vector3Value;

                    // Create new GameObject as child
                    GameObject go = new GameObject("Spark_" + i);
                    Undo.RegisterCreatedObjectUndo(go, "Create Spark Transform");
                    go.transform.SetParent(sparksUI.transform);
                    go.transform.localPosition = localPos;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    go.hideFlags = HideFlags.DontSave;
                }

                EditorUtility.SetDirty(sparksUI);
            }
        }
    }
}