using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(SceneObject))]
    public class SceneObjectPropertyDrawer : UnityEditor.PropertyDrawer
    {
        private const int ERROR_NULL = 0;
        private const int ERROR_UNACTIVE = 1;
        private const int ERROR_BUILD_MISSING = 2;

        private readonly string[] ERROR_MESSAGES = new string[]
        {
            "Scene file should be linked!",
            "Scene isn't added to the Build!",
            "Scene isn't added to the Build!"
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sceneObjectProperty = property.FindPropertyRelative("scene");

            Rect propertyRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            int errorID = ValidateScene(sceneObjectProperty.objectReferenceValue);
            if (errorID != -1)
            {
                Rect helpBoxRect = new Rect(position.x + 14, position.y + 2, position.width - 14, 24);

                EditorGUI.HelpBox(helpBoxRect, ERROR_MESSAGES[errorID], MessageType.Warning);

                if(errorID ==  ERROR_UNACTIVE)
                {
                    DrawHelpButton(helpBoxRect, "Fix", () =>
                    {
                        ActivateScene(sceneObjectProperty.objectReferenceValue);
                    });
                }
                else if (errorID == ERROR_BUILD_MISSING)
                {
                    DrawHelpButton(helpBoxRect, "Fix", () =>
                    {
                        AddNewScene(sceneObjectProperty.objectReferenceValue);
                    });
                }

                propertyRect.y += helpBoxRect.height + 4;
            }

            sceneObjectProperty.objectReferenceValue = EditorGUI.ObjectField(propertyRect, "Scene", sceneObjectProperty.objectReferenceValue, typeof(SceneAsset), false) as SceneAsset;

            EditorGUI.EndProperty();
        }

        private void DrawHelpButton(Rect rect, string name, SimpleCallback clickCallback)
        {
            GUIStyle buttonStyle = new GUIStyle(EditorCustomStyles.button);
            buttonStyle.fontSize = 10;
            buttonStyle.padding = new RectOffset(4, 4, 3, 3);

            GUIContent label = new GUIContent(name);

            Vector2 buttonSize = buttonStyle.CalcSize(label);

            float yOffset = (rect.height - buttonSize.y) / 2;

            Rect buttonRect = new Rect(rect.x + rect.width - buttonSize.x - 8, rect.y + yOffset, buttonSize.x, buttonSize.y);

            if(GUI.Button(buttonRect, label, buttonStyle))
            {
                clickCallback?.Invoke();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + 2;

            SerializedProperty sceneObjectProperty = property.FindPropertyRelative("scene");

            if (ValidateScene(sceneObjectProperty.objectReferenceValue) != -1)
            {
                height += 24 + 4;
            }

            return height;
        }

        private void ActivateScene(Object sceneObject)
        {
            if (sceneObject == null) return;

            string scenePath = AssetDatabase.GetAssetPath(sceneObject);

            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var scene in editorBuildSettingsScenes)
            {
                // Check if scene is already added to the list
                if (scene.path == scenePath)
                {
                    scene.enabled = true;

                    // Set the Build Settings window Scene list
                    EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();

                    break;
                }
            }
        }

        private void AddNewScene(Object sceneObject)
        {
            if (sceneObject == null) return;

            string scenePath = AssetDatabase.GetAssetPath(sceneObject);

            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach(var scene in editorBuildSettingsScenes)
            {
                // Check if scene is already added to the list
                if (scene.path == scenePath) return;
            }

            editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));

            // Set the Build Settings window Scene list
            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        }

        private int ValidateScene(Object sceneObject)
        {
            if (sceneObject == null)
            {
                return ERROR_NULL;
            }

            bool isSceneAddedToBuildList = false;

            string scenePath = AssetDatabase.GetAssetPath(sceneObject);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath)
                {
                    isSceneAddedToBuildList = true;

                    if (!scene.enabled)
                    {
                        return ERROR_UNACTIVE;
                    }
                }
            }

            if (!isSceneAddedToBuildList)
            {
                return ERROR_BUILD_MISSING;
            }

            return -1;
        }
    }
}
