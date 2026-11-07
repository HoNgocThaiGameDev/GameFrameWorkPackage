using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace NebulaSoft
{
    [CustomEditor(typeof(ProjectInitSettings))]
    public class ProjectInitSettingsEditor : Editor
    {
        private const string MODULES_PROPERTY_NAME = "modules";

        private SerializedProperty modulesProperty;

        private List<InitModuleContainer> initModulesEditors;

        private ProjectInitSettings projectInitSettings;
        private GenericMenu modulesGenericMenu;

        private static InitModulesHandler modulesHandler;

        [MenuItem("Window/NebulaSoft Core/Project Init Settings", priority = 50)]
        public static void SelectProjectInitSettings()
        {
            ProjectInitSettings selectedObject = EditorUtils.GetAsset<ProjectInitSettings>();
            if (selectedObject != null)
            {
                Selection.activeObject = selectedObject;
            }
            else
            {
                Debug.LogError("Asset with type \"ProjectInitSettings\" don`t exist.");
            }
        }

        protected void OnEnable()
        {
            projectInitSettings = (ProjectInitSettings)target;

            modulesHandler = new InitModulesHandler();

            modulesProperty = serializedObject.FindProperty(MODULES_PROPERTY_NAME);

            InitGenericMenu();
            InitCoreModules(projectInitSettings.Modules);

            LoadEditorsList();

            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private void InitCoreModules(InitModule[] coreModules)
        {
            RequiredModule[] requiredModules = GetRequiredModules(coreModules);
            if(requiredModules.Length > 0)
            {
                foreach (RequiredModule requiredModule in requiredModules)
                {
                    AddModule(requiredModule.Type);
                }

                LoadEditorsList();

                EditorUtility.SetDirty(target);

                AssetDatabase.SaveAssets();
            }
        }

        private void InitGenericMenu()
        {
            modulesGenericMenu = new GenericMenu();

            //Load all modules
            InitModule[] initModules = projectInitSettings.Modules;

            IEnumerable<Type> registeredTypes = GetRegisteredAttributes();
            foreach (Type type in registeredTypes)
            {
                RegisterModuleAttribute defineAttribute = (RegisterModuleAttribute)Attribute.GetCustomAttribute(type, typeof(RegisterModuleAttribute));
                if(defineAttribute != null)
                {
                    if (!defineAttribute.Core)
                    {
                        bool isAlreadyActive = initModules != null && initModules.Any(x => x != null && x.GetType() == type);
                        if (isAlreadyActive)
                        {
                            modulesGenericMenu.AddDisabledItem(new GUIContent("Add Module/" + defineAttribute.Path), false);
                        }
                        else
                        {
                            modulesGenericMenu.AddItem(new GUIContent("Add Module/" + defineAttribute.Path), false, delegate
                            {
                                AddModule(type);

                                InitGenericMenu();
                            });
                        }
                    }
                }
            }
        }

        private IEnumerable<Type> GetRegisteredAttributes()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<Type> types = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(InitModule)));
                foreach (Type type in types)
                {
                    yield return type;
                }
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= LogPlayModeState;
        }

        private void LogPlayModeState(PlayModeStateChange obj)
        {
            if (Selection.activeObject == target)
                Selection.activeObject = null;
        }

        private void LoadEditorsList()
        {
            ClearInitModules();
            SerializedProperty initModule;
            SerializedObject initModuleSerializedObject;

            for (int i = 0; i < modulesProperty.arraySize; i++)
            {
                initModule = modulesProperty.GetArrayElementAtIndex(i);

                if(initModule.objectReferenceValue != null)
                {
                    initModuleSerializedObject = new SerializedObject(initModule.objectReferenceValue);

                    initModulesEditors.Add(new InitModuleContainer(initModule.objectReferenceValue.GetType(), initModuleSerializedObject, Editor.CreateEditor(initModuleSerializedObject.targetObject), modulesHandler.IsCoreModule(initModule.objectReferenceValue.GetType())));
                }
            }
        }

        private InitModuleContainer GetEditor(Type type)
        {
            for(int i = 0; i < initModulesEditors.Count; i++)
            {
                if (initModulesEditors[i].Type == type)
                    return initModulesEditors[i];
            }

            return null;
        }

        private void OnDestroy()
        {
            ClearInitModules();
        }

        private void ClearInitModules()
        {
            if (initModulesEditors != null)
            {
                // Destroy old editors
                for (int i = 0; i < initModulesEditors.Count; i++)
                {
                    if (initModulesEditors[i] != null && initModulesEditors[i].Editor != null)
                    {
                        DestroyImmediate(initModulesEditors[i].Editor);
                    }
                }

                initModulesEditors.Clear();
            }
            else
            {
                initModulesEditors = new List<InitModuleContainer>();
            }
        }

        private void DrawModules(SerializedProperty arrayProperty)
        {
            var current = Event.current;

            if (arrayProperty.arraySize > 0)
            {
                for (int i = 0; i < arrayProperty.arraySize; i++)
                {
                    SerializedProperty initModuleProperty = arrayProperty.GetArrayElementAtIndex(i);

                    if (initModuleProperty.objectReferenceValue != null)
                    {
                        InitModule initModule = (InitModule)initModuleProperty.objectReferenceValue;

                        SerializedObject moduleSeializedObject = new SerializedObject(initModuleProperty.objectReferenceValue);

                        moduleSeializedObject.Update();

                        Rect blockRect = EditorGUILayout.BeginVertical(EditorCustomStyles.box, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));

                        GUI.Box(new Rect(blockRect.x - 10, blockRect.y, blockRect.width + 10, 21), GUIContent.none);

                        GUILayout.Space(14);

                        initModuleProperty.isExpanded = EditorGUI.Foldout(new Rect(blockRect.x + 8, blockRect.y, blockRect.width - 30, 21), initModuleProperty.isExpanded, initModule.ModuleName, true);

                        if (initModuleProperty.isExpanded)
                        {
                            GUILayout.Space(12);

                            InitModuleContainer moduleContainer = GetEditor(initModuleProperty.objectReferenceValue.GetType());
                            if (moduleContainer == null) continue;

                            moduleContainer.OnInspectorGUI();

                            GUILayout.Space(10);

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            moduleContainer.DrawButtons();

                            if(!moduleContainer.IsCore)
                            {
                                if (GUILayout.Button("Remove", GUILayout.Width(90)))
                                {
                                    if (EditorUtility.DisplayDialog("This object will be removed!", "Are you sure?", "Remove", "Cancel"))
                                    {
                                        moduleContainer.OnRemoved();

                                        UnityEngine.Object removedObject = initModuleProperty.objectReferenceValue;
                                        initModuleProperty.isExpanded = false;
                                        arrayProperty.RemoveFromVariableArrayAt(i);

                                        LoadEditorsList();
                                        AssetDatabase.RemoveObjectFromAsset(removedObject);

                                        DestroyImmediate(removedObject, true);

                                        EditorUtility.SetDirty(target);

                                        AssetDatabase.SaveAssets(); 
                                        
                                        InitGenericMenu();

                                        return;
                                    }
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndVertical();

                        if(GUI.Button(new Rect(blockRect.x + blockRect.width - 20, blockRect.y + 2, 17, 17), "="))
                        {
                            int index = i;

                            GenericMenu genericMenu = new GenericMenu();
                            if (i > 0)
                            {
                                genericMenu.AddItem(new GUIContent("Move Up"), false, delegate
                                {
                                    bool expandState = arrayProperty.GetArrayElementAtIndex(index - 1).isExpanded;

                                    arrayProperty.MoveArrayElement(index, index - 1);

                                    arrayProperty.GetArrayElementAtIndex(index - 1).isExpanded = initModuleProperty.isExpanded;
                                    arrayProperty.GetArrayElementAtIndex(index).isExpanded = expandState;
                                    serializedObject.ApplyModifiedProperties();
                                });
                            }
                            else
                            {
                                genericMenu.AddDisabledItem(new GUIContent("Move Up"), false);
                            }

                            if (i + 1 < arrayProperty.arraySize)
                            {
                                genericMenu.AddItem(new GUIContent("Move Down"), false, delegate
                                {
                                    bool expandState = arrayProperty.GetArrayElementAtIndex(index + 1).isExpanded;

                                    arrayProperty.MoveArrayElement(index, index + 1);

                                    arrayProperty.GetArrayElementAtIndex(index + 1).isExpanded = initModuleProperty.isExpanded;
                                    arrayProperty.GetArrayElementAtIndex(index).isExpanded = expandState;

                                    serializedObject.ApplyModifiedProperties();
                                });
                            }
                            else
                            {
                                genericMenu.AddDisabledItem(new GUIContent("Move Down"), false);
                            }

                            InitModuleContainer moduleContainer = GetEditor(initModuleProperty.objectReferenceValue.GetType());
                            if(moduleContainer != null)
                            {
                                moduleContainer.PrepareMenuItems(ref genericMenu);
                            }

                            genericMenu.ShowAsContext();
                        }

                        moduleSeializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal(EditorCustomStyles.box);
                        EditorGUILayout.BeginHorizontal(EditorCustomStyles.padding00);
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.warnicon"), EditorCustomStyles.padding00, GUILayout.Width(16), GUILayout.Height(16));
                        EditorGUILayout.LabelField("Object referenct is null");
                        if (GUILayout.Button("Remove", EditorStyles.miniButton))
                        {
                            arrayProperty.RemoveFromVariableArrayAt(i);

                            InitGenericMenu();

                            GUIUtility.ExitGUI();
                            Event.current.Use();

                            return;
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Modules list is empty!", MessageType.Info);
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayoutCustom.BeginMenuBoxGroup("Modules", modulesGenericMenu);

            DrawModules(modulesProperty);

            EditorGUILayoutCustom.EndBoxGroup();

            GUILayout.FlexibleSpace();
        }

        public void AddModule(Type moduleType)
        {
            if(!moduleType.IsSubclassOf(typeof(InitModule)))
            {
                Debug.LogError("[Initializer]: Module type should be subclass of InitModule class!");

                return;
            }

            Undo.RecordObject(target, "Add module");

            modulesProperty = serializedObject.FindProperty(MODULES_PROPERTY_NAME);

            serializedObject.Update();

            modulesProperty.arraySize++;

            InitModule initModule = (InitModule)ScriptableObject.CreateInstance(moduleType);
            initModule.name = moduleType.ToString();
            initModule.hideFlags = HideFlags.HideInHierarchy; 
            
            AssetDatabase.AddObjectToAsset(initModule, target);

            modulesProperty.GetArrayElementAtIndex(modulesProperty.arraySize - 1).objectReferenceValue = initModule;

            serializedObject.ApplyModifiedProperties();
            LoadEditorsList();

            EditorUtility.SetDirty(target);

            AssetDatabase.SaveAssets();

            Editor editor = Editor.CreateEditor(initModule);
            InitModuleEditor initModuleEditor = editor as InitModuleEditor;
            if (initModuleEditor != null)
            {
                initModuleEditor.OnCreated();
            }

            DestroyImmediate(editor);
        }

        [MenuItem("Assets/Create/Data/Core/Project Init Settings")]
        public static void CreateAsset()
        {
            ProjectInitSettings projectInitSettings = EditorUtils.GetAsset<ProjectInitSettings>();
            if (projectInitSettings)
            {
                Debug.Log("Project Init Settings file is already exits!");

                EditorGUIUtility.PingObject(projectInitSettings);

                return;
            }

            string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            selectionPath = Path.GetDirectoryName(selectionPath);

            if (string.IsNullOrEmpty(selectionPath) || !Directory.Exists(selectionPath))
                selectionPath = "Assets";

            CreateAsset(selectionPath, true);
        }

        public static ProjectInitSettings CreateAsset(string folderPath, bool ping)
        {
            ProjectInitSettings projectInitSettings = (ProjectInitSettings)ScriptableObject.CreateInstance<ProjectInitSettings>();
            projectInitSettings.name = "Project Init Settings";

            // Create a unique file path for the ScriptableObject
            string assetPath = Path.Combine(folderPath, projectInitSettings.name + ".asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            // Save the ScriptableObject to the determined path
            AssetDatabase.CreateAsset(projectInitSettings, assetPath);
            AssetDatabase.SaveAssets();

            SerializedObject serializedObject = new SerializedObject(projectInitSettings);
            serializedObject.Update();

            SerializedProperty coreModulesProperty = serializedObject.FindProperty(MODULES_PROPERTY_NAME);

            RequiredModule[] requiredModules = GetRequiredModules(null);
            List<InitModule> initModules = new List<InitModule>();

            foreach (RequiredModule requiredModule in requiredModules)
            {
                // Create init module
                InitModule initModule = (InitModule)ScriptableObject.CreateInstance(requiredModule.Type);
                initModule.name = requiredModule.Type.ToString();
                initModule.hideFlags = HideFlags.HideInHierarchy;

                AssetDatabase.AddObjectToAsset(initModule, projectInitSettings);

                coreModulesProperty.arraySize++;
                coreModulesProperty.GetArrayElementAtIndex(coreModulesProperty.arraySize - 1).objectReferenceValue = initModule;

                initModules.Add(initModule);
            }

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(projectInitSettings);

            AssetDatabase.SaveAssets();

            foreach (var initModule in initModules)
            {
                Editor editor = Editor.CreateEditor(initModule);
                InitModuleEditor initModuleEditor = editor as InitModuleEditor;
                if (initModuleEditor != null)
                {
                    initModuleEditor.OnCreated();
                }

                DestroyImmediate(editor);
            }

            if (ping)
                EditorGUIUtility.PingObject(projectInitSettings);

            return projectInitSettings;
        }

        private static RequiredModule[] GetRequiredModules(InitModule[] coreModules)
        {
            // Get all registered init modules
            IEnumerable<Type> registeredTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(InitModule)));

            List<RequiredModule> requiredModules = new List<RequiredModule>();
            foreach (Type type in registeredTypes)
            {
                RegisterModuleAttribute[] defineAttributes = (RegisterModuleAttribute[])Attribute.GetCustomAttributes(type, typeof(RegisterModuleAttribute));
                for (int m = 0; m < defineAttributes.Length; m++)
                {
                    if (defineAttributes[m].Core)
                    {
                        bool isExists = coreModules != null && coreModules.Any(x => x != null && x.GetType() == type);
                        if (!isExists)
                        {
                            requiredModules.Add(new RequiredModule(defineAttributes[m], type));
                        }
                    }
                }
            }

            return requiredModules.OrderByDescending(x => x.Attribute.Order).ToArray();
        }

        private class InitModulesHandler
        {
            private IEnumerable<ModuleData> modulesData;

            public InitModulesHandler()
            {
                modulesData = GetModulesData();
            }

            private IEnumerable<ModuleData> GetModulesData()
            {
                IEnumerable<Type> registeredTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(InitModule)));

                foreach (Type type in registeredTypes)
                {
                    RegisterModuleAttribute defineAttribute = (RegisterModuleAttribute)Attribute.GetCustomAttribute(type, typeof(RegisterModuleAttribute));
                    if (defineAttribute != null)
                    {
                        yield return new ModuleData()
                        {
                            ClassType = type,
                            Attribute = defineAttribute
                        };
                    }
                }
            }

            public bool IsCoreModule(Type type)
            {
                foreach (var data in modulesData)
                {
                    if (type == data.ClassType && data.Attribute.Core)
                        return true;
                }

                return false;
            }

            public class ModuleData
            {
                public Type ClassType;
                public RegisterModuleAttribute Attribute;
            }
        }

        private class InitModuleContainer
        {
            public Type Type;
            public SerializedObject SerializedObject;
            public Editor Editor;

            private bool isModuleInitEditor;
            private InitModuleEditor initModuleEditor;

            public bool IsCore;

            public InitModuleContainer(Type type, SerializedObject serializedObject, Editor editor, bool isCore)
            {
                Type = type;
                SerializedObject = serializedObject;
                Editor = editor;
                IsCore = isCore;

                initModuleEditor = editor as InitModuleEditor;
                isModuleInitEditor = initModuleEditor != null;
            }

            public void OnInspectorGUI()
            {
                if(Editor != null)
                {
                    Editor.OnInspectorGUI();
                }
            }

            public void DrawButtons()
            {
                if (!isModuleInitEditor) return;

                initModuleEditor.Buttons();
            }

            public void PrepareMenuItems(ref GenericMenu genericMenu)
            {
                if (!isModuleInitEditor) return;

                initModuleEditor.PrepareMenuItems(ref genericMenu);
            }

            public void OnRemoved()
            {
                if (!isModuleInitEditor) return;

                initModuleEditor.OnRemoved();
            }
        }

        private class RequiredModule
        {
            public RegisterModuleAttribute Attribute { get; private set; }
            public Type Type { get; private set; }

            public RequiredModule(RegisterModuleAttribute attribute, Type type)
            {
                Attribute = attribute;
                Type = type;
            }
        }
    }
}