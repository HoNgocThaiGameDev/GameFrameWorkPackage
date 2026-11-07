#pragma warning disable 0414

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace NebulaSoft
{
    [CustomEditor(typeof(PoolManager))]
    sealed internal class PoolManagerEditor : Editor
    {
        private SerializedProperty poolsListProperty;
        private MethodInfo isAllPrefabsAssignedAtPoolMethodInfo;
        private MethodInfo recalculateWeightsAtPoolMethodInfo;

        //private PoolSettings newPoolBuilder;
        private Rect inspectorRect = new Rect();
        private Rect dragAndDropRect = new Rect();

        private bool isNameAllowed = true;
        private bool isNameAlreadyExisting = false;
        private bool isSettingsExpanded = false;
        private bool dragAndDropActive = false;
        private bool skipEmptyNameWarning = false;

        private int selectedPoolIndex;

        private const string POOLS_LIST_PROPERTY_NAME = "poolsList";
        private const string RENAMING_EMPTY_STRING = "[PoolManager: empty]";
        private const string EMPTY_POOL_BUILDER_NAME = "[PoolBuilder: empty]";

        private string searchText = string.Empty;
        private string prevNewPoolName = string.Empty;
        private string prevSelectedPoolName = string.Empty;
        private string lastRenamingName = string.Empty;

        private Color defaultColor;

        private GUIStyle boldStyle = new GUIStyle();
        private GUIStyle headerStyle = new GUIStyle();
        private GUIStyle bigHeaderStyle = new GUIStyle();
        private GUIStyle centeredTextStyle = new GUIStyle();
        private GUIStyle multiListLablesStyle = new GUIStyle();
        private GUIStyle dragAndDropBoxStyle = new GUIStyle();

        private GUIContent warningIconGUIContent;
        private GUIContent lockedIconGUIContent;
        private GUIContent unlockedIconGUIContent;

        private void OnEnable()
        {
            poolsListProperty = serializedObject.FindProperty(POOLS_LIST_PROPERTY_NAME);
            isAllPrefabsAssignedAtPoolMethodInfo = serializedObject.targetObject.GetType().GetMethod("IsAllPrefabsAssignedAtPool", BindingFlags.NonPublic | BindingFlags.Instance);
            recalculateWeightsAtPoolMethodInfo = serializedObject.targetObject.GetType().GetMethod("RecalculateWeightsAtPool", BindingFlags.NonPublic | BindingFlags.Instance);

            lastRenamingName = RENAMING_EMPTY_STRING;
            isNameAllowed = true;
            isNameAlreadyExisting = false;

            selectedPoolIndex = -1;
            //newPoolBuilder = new PoolSettings().SetName(EMPTY_POOL_BUILDER_NAME);

            PrepareStyles();
        }

        private void PrepareStyles()
        {
            Color labelColor = EditorGUIUtility.isProSkin ? new Color(1.0f, 1.0f, 1.0f) : new Color(0.12f, 0.12f, 0.12f);

            boldStyle.fontStyle = FontStyle.Bold;

            headerStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            headerStyle.normal.textColor = labelColor;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 12;

            bigHeaderStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            bigHeaderStyle.normal.textColor = labelColor;
            bigHeaderStyle.normal.textColor = labelColor;
            bigHeaderStyle.alignment = TextAnchor.MiddleCenter;
            bigHeaderStyle.fontStyle = FontStyle.Bold;
            bigHeaderStyle.fontSize = 14;

            centeredTextStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            centeredTextStyle.normal.textColor = labelColor;
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;

            multiListLablesStyle.fontSize = 8;
            multiListLablesStyle.normal.textColor = labelColor;

            Texture warningIconTexture = EditorCustomStyles.GetIcon("icon_warning");// AssetDatabase.LoadAssetAtPath<Texture>(@"Assets\Project Name\NebulaSoft Core\Plugins\Editor\Resources\UI\Sprites\Icons\icon_warning.png");
            warningIconGUIContent = new GUIContent(warningIconTexture);


            Texture lockedTexture = EditorCustomStyles.GetIcon("icon_locked"); // AssetDatabase.LoadAssetAtPath<Texture>(@"Assets\Project Name\NebulaSoft Core\Plugins\Editor\Resources\UI\Sprites\Icons\icon_locked.png");
            lockedIconGUIContent = new GUIContent(lockedTexture);

            Texture unlockedTexture = EditorCustomStyles.GetIcon("icon_unlocked");//AssetDatabase.LoadAssetAtPath<Texture>(@"Assets\Project Name\NebulaSoft Core\Plugins\Editor\Resources\UI\Sprites\Icons\icon_unlocked.png");
            unlockedIconGUIContent = new GUIContent(unlockedTexture);


            defaultColor = GUI.contentColor;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (dragAndDropActive)
            {
                dragAndDropBoxStyle = GUI.skin.box;
                dragAndDropBoxStyle.alignment = TextAnchor.MiddleCenter;
                dragAndDropBoxStyle.fontStyle = FontStyle.Bold;
                dragAndDropBoxStyle.fontSize = 12;

                GUILayout.Box("Drag objects here", dragAndDropBoxStyle, GUILayout.Width(EditorGUIUtility.currentViewWidth - 21), GUILayout.Height(inspectorRect.size.y));
            }
            else
            {
                inspectorRect = EditorGUILayout.BeginVertical();


                // Control bar /////////////////////////////////////////////////////////////////////////////
                EditorGUILayout.BeginVertical(GUI.skin.box);

                //// if we are not setuping a new pool now - than displaying settings interface
                //if (newPoolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME))
                //{
                //    EditorGUI.indentLevel++;

                //    isSettingsExpanded = EditorGUILayout.Foldout(isSettingsExpanded, "Settings");

                //    if (isSettingsExpanded)
                //    {
                //        EditorGUI.BeginChangeCheck();

                //        // [CACHE IS CURRENTLY DISABLED]
                //        //poolManagerRef.useCache = EditorGUILayout.Toggle("Use cache :", poolManagerRef.useCache);

                //        if (EditorGUI.EndChangeCheck())
                //        {
                //            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                //        }

                //        EditorGUILayout.Space();
                //    }

                //    EditorGUI.indentLevel--;


                //    if (GUILayout.Button("Add pool", GUILayout.Height(30)))
                //    {
                //        skipEmptyNameWarning = true;
                //        //AddNewSinglePool();
                //    }
                //}

                //// Pool creation bar //////////////////////////////////////////////////////////////////////////
                //if (!newPoolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME))
                //{
                //    //EditorGUILayout.BeginVertical(GUI.skin.box);

                //    GUILayout.Space(3f);
                //    EditorGUILayout.BeginHorizontal();
                //    EditorGUILayout.LabelField("Pool creation:", headerStyle, GUILayout.Width(100));
                //    GUILayout.FlexibleSpace();
                //    if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                //    {
                //        CancelNewPoolCreation();

                //        return;
                //    }
                //    EditorGUILayout.EndHorizontal();

                //    GUILayout.Space(4f);

                //    //newPoolBuilder = DrawPool(newPoolBuilder, null, 0);

                //    GUILayout.Space(5f);

                //    if (GUILayout.Button("Confirm", GUILayout.Height(25)))
                //    {
                //        GUI.FocusControl(null);
                //        ConfirmPoolCreation();

                //        return;
                //    }

                //    GUILayout.Space(5f);
                //    //EditorGUILayout.EndVertical();
                //}

                EditorGUILayout.EndVertical();


                // Pools displaying region /////////////////////////////////////////////////////////////////////

                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField("Pools list", headerStyle);

                GUILayout.BeginHorizontal();

                // searching
                searchText = EditorGUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSearchTextField"));

                if (!string.IsNullOrEmpty(searchText))
                {
                    if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton")))
                    {
                        // Remove focus if cleared
                        searchText = "";
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    GUILayout.Button(GUIContent.none, GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty"));
                }

                GUILayout.EndHorizontal();

                if (poolsListProperty.arraySize == 0)
                {
                    if (string.IsNullOrEmpty(searchText))
                    {
                        EditorGUILayout.HelpBox("There's no pools.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Pool \"" + searchText + "\" is not found.", MessageType.Info);
                    }
                }
                else
                {
                    for (int currentPoolIndex = 0; currentPoolIndex < poolsListProperty.arraySize; currentPoolIndex++)
                    {
                        SerializedProperty poolProperty = poolsListProperty.GetArrayElementAtIndex(currentPoolIndex);

                        if (searchText == string.Empty || (searchText != string.Empty && poolProperty.FindPropertyRelative("name").stringValue.Contains(searchText)))
                        {
                            Rect clickRect = EditorGUILayout.BeginVertical(GUI.skin.box);
                            EditorGUI.indentLevel++;

                            if (selectedPoolIndex == -1 || currentPoolIndex != selectedPoolIndex)
                            {
                                if (selectedPoolIndex != -1)
                                {
                                    CancelNewPoolCreation();
                                }


                                if ((bool)isAllPrefabsAssignedAtPoolMethodInfo.Invoke(serializedObject.targetObject, new object[] { currentPoolIndex }))
                                {
                                    string runtimeCreatedNameAddition = poolProperty.FindPropertyRelative("isRuntimeCreated").boolValue ? "   [Runtime]" : "";
                                    EditorGUILayout.LabelField(GetPoolName(currentPoolIndex) + runtimeCreatedNameAddition, centeredTextStyle);
                                }
                                else
                                {
                                    EditorGUILayout.BeginHorizontal();

                                    GUI.contentColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                                    EditorGUILayout.LabelField(warningIconGUIContent, GUILayout.Width(30));
                                    GUI.contentColor = defaultColor;

                                    GUILayout.Space(-35f);
                                    EditorGUILayout.LabelField(GetPoolName(currentPoolIndex), centeredTextStyle);

                                    EditorGUILayout.EndHorizontal();
                                }

                            }
                            else
                            {
                                GUILayout.Space(5);

                                // pool drawing
                                //DrawPool(newPoolBuilder, poolProperty, currentPoolIndex);

                                GUILayout.Space(5);

                                // cache system region ///////////
                                // [CURRENTLY DISABLED]
                                //if (poolManagerRef.useCache && poolsCacheList[currentPoolIndex] != null)
                                //{
                                //    EditorGUI.BeginChangeCheck();
                                //    poolsCacheList[currentPoolIndex].ignoreCache = EditorGUILayout.Toggle("Ignore cache: ", poolsCacheList[currentPoolIndex].ignoreCache);

                                //    if (EditorGUI.EndChangeCheck())
                                //    {
                                //        UpdateIgnoreCacheStateOfPool(poolsCacheList[currentPoolIndex].poolName, poolsCacheList[currentPoolIndex].ignoreCache);
                                //        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                                //    }
                                //}

                                //if (poolManagerRef.useCache && poolsCacheDeltaList[currentPoolIndex] != 0 && poolsCacheList[currentPoolIndex] != null)
                                //{
                                //    if (poolsCacheList[currentPoolIndex].ignoreCache)
                                //    {
                                //        GUI.enabled = false;
                                //        EditorGUILayout.LabelField("Cached value: " + poolsCacheList[currentPoolIndex].poolSize);
                                //        GUI.enabled = true;
                                //    }
                                //    else
                                //    {
                                //        if (GUILayout.Button("Apply cache: " + (pool.Size + poolsCacheDeltaList[currentPoolIndex])))
                                //        {
                                //            Undo.RecordObject(target, "Apply cache");

                                //            poolManagerRef.pools[currentPoolIndex].Size = poolsCacheList[currentPoolIndex].poolSize;

                                //            ClearObsoleteCache();
                                //            UpdateCacheStateList();
                                //        }
                                //    }
                                //}

                                // delete button ///////////

                                if (GUILayout.Button("Delete"))
                                {
                                    if (EditorUtility.DisplayDialog("This pool will be removed!", "Are you sure?", "Remove", "Cancel"))
                                    {
                                        DeletePool(currentPoolIndex);

                                        EditorApplication.delayCall += delegate
                                        {
                                            EditorUtility.FocusProjectWindow();
                                        };
                                    }
                                }

                                GUILayout.Space(5);
                            }

                            EditorGUI.indentLevel--;
                            EditorGUILayout.EndVertical();

                            if (GUI.Button(clickRect, GUIContent.none, GUIStyle.none))
                            {
                                GUI.FocusControl(null);

                                if (selectedPoolIndex == -1 || selectedPoolIndex != currentPoolIndex)
                                {
                                    selectedPoolIndex = currentPoolIndex;
                                    lastRenamingName = RENAMING_EMPTY_STRING;
                                    isNameAlreadyExisting = false;
                                    isNameAllowed = true;
                                    //newPoolBuilder = newPoolBuilder.Reset().SetName(EMPTY_POOL_BUILDER_NAME);
                                }
                                else
                                {
                                    selectedPoolIndex = -1;
                                    lastRenamingName = RENAMING_EMPTY_STRING;
                                    isNameAlreadyExisting = false;
                                    isNameAllowed = true;
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.EndVertical();

                if (GUI.changed)
                {
                    EditorUtility.SetDirty(target);
                }
                EditorGUILayout.EndVertical();

            }


            serializedObject.ApplyModifiedProperties();

            // Drag n Drop region /////////////////////////////////////////////////////////////////////

            //Event currentEvent = Event.current;

            //if (inspectorRect.Contains(currentEvent.mousePosition) && selectedPoolIndex == -1 && newPoolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME) && !isSettingsExpanded)
            //{
            //    if (currentEvent.type == EventType.DragUpdated)
            //    {
            //        dragAndDropActive = true;
            //        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            //        currentEvent.Use();
            //    }
            //    else if (currentEvent.type == EventType.DragPerform)
            //    {
            //        dragAndDropActive = false;
            //        List<Pool.MultiPoolPrefab> draggedObjects = new List<Pool.MultiPoolPrefab>();

            //        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            //        {
            //            if (obj.GetType() == typeof(GameObject))
            //            {
            //                draggedObjects.Add(new Pool.MultiPoolPrefab(obj as GameObject, 0, false));
            //            }
            //        }

            //        if (draggedObjects.Count == 1)
            //        {
            //            AddNewSinglePool(draggedObjects[0].prefab);
            //        }
            //        else
            //        {
            //            AddNewMultiPool(draggedObjects);
            //        }

            //        currentEvent.Use();
            //    }
            //}
            //else
            //{
            //    if (currentEvent.type == EventType.Repaint)
            //    {
            //        dragAndDropActive = false;
            //    }
            //}

        }


        //private PoolSettings DrawPool(PoolSettings poolBuilder, SerializedProperty poolProperty, int poolIndex)
        //{
        //    EditorGUI.BeginChangeCheck();

        //    // name ///////////
        //    string poolName = poolProperty != null ? poolProperty.FindPropertyRelative("name").stringValue : poolBuilder.name;

        //    GUILayout.BeginHorizontal();

        //    string newName = EditorGUILayout.TextField("Name: ", lastRenamingName != RENAMING_EMPTY_STRING ? lastRenamingName : poolName);

        //    if (newName == poolName && (!newPoolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME) ? newName.Equals(string.Empty) : true))
        //    {
        //        lastRenamingName = RENAMING_EMPTY_STRING;
        //        isNameAllowed = true;
        //        isNameAlreadyExisting = false;
        //    }

        //    if (!isNameAllowed || newName == string.Empty || newName != poolName || lastRenamingName != RENAMING_EMPTY_STRING)
        //    {
        //        lastRenamingName = newName;
        //        isNameAllowed = IsNameAllowed(newName);

        //        EditorGUI.BeginDisabledGroup(!isNameAllowed);

        //        // if name is emplty or it's pool creation - do not show rename button

        //        if (!(!isNameAllowed && !isNameAlreadyExisting || !newPoolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME)))
        //        {
        //            if (GUILayout.Button("rename"))
        //            {
        //                //RenamePool(poolProperty, poolBuilder, newName);

        //                lastRenamingName = RENAMING_EMPTY_STRING;
        //            }
        //        }

        //        EditorGUI.EndDisabledGroup();
        //        GUILayout.EndHorizontal();

        //        if (isNameAllowed)
        //        {
        //            // [CACHE IS CURRENTLY DISABLED]
        //            //if (poolManagerRef.useCache)
        //            //{
        //            //    RenameCachedPool(poolName, newName);
        //            //}
        //        }
        //        else
        //        {
        //            if (isNameAlreadyExisting)
        //            {
        //                EditorGUILayout.HelpBox("Name already exists", MessageType.Warning);
        //            }
        //            else
        //            {
        //                if (!skipEmptyNameWarning)
        //                {
        //                    EditorGUILayout.HelpBox("Name can't be empty", MessageType.Warning);
        //                }
        //            }
        //        }

        //    }
        //    else
        //    {
        //        EditorGUILayout.EndHorizontal();
        //    }

        //    if (!poolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME))
        //    {
        //        poolBuilder = poolBuilder.SetName(newName);
        //    }



        //    // type ///////////
        //    //Pool.PoolType poolType = poolProperty != null ? (Pool.PoolType)poolProperty.FindPropertyRelative("type").enumValueIndex : poolBuilder.type;
        //    //Pool.PoolType currentPoolType = (Pool.PoolType)EditorGUILayout.EnumPopup("Pool type:", poolType);

        //    //if (currentPoolType != poolType)
        //    //{
        //    //    if (poolProperty != null)
        //    //    {
        //    //        poolProperty.FindPropertyRelative("type").enumValueIndex = (int)currentPoolType;
        //    //    }
        //    //    else
        //    //    {
        //    //        poolBuilder = poolBuilder.SetType(currentPoolType);
        //    //    }
        //    //}

        //    // prefabs field ///////////
        //    //if (currentPoolType == Pool.PoolType.Single)
        //    if(true)
        //    {
        //        // single prefab pool editor
        //        GameObject currentPrefab = poolProperty != null ? (GameObject)poolProperty.FindPropertyRelative("singlePoolPrefab").objectReferenceValue : poolBuilder.singlePoolPrefab;
        //        GameObject prefab = (GameObject)EditorGUILayout.ObjectField("Prefab: ", currentPrefab, typeof(GameObject), false);

        //        if (currentPrefab != prefab)
        //        {
        //            if (poolProperty != null)
        //            {
        //                poolProperty.FindPropertyRelative("singlePoolPrefab").objectReferenceValue = prefab;
        //            }
        //            else
        //            {
        //                poolBuilder = poolBuilder.SetSinglePrefab(prefab);
        //            }

        //            string currentName = poolProperty != null ? poolProperty.FindPropertyRelative("name").stringValue : poolBuilder.name;

        //            if (currentName == string.Empty)
        //            {
        //                //RenamePool(poolProperty, poolBuilder, prefab.name);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // multiple prefabs pool editor
        //        GUILayout.Space(5f);

        //        int currentPrefabsAmount = poolProperty != null ? poolProperty.FindPropertyRelative("multiPoolPrefabsList").arraySize : poolBuilder.multiPoolPrefabsList.Count;

        //        EditorGUILayout.BeginHorizontal();

        //        EditorGUI.BeginDisabledGroup(true);
        //        EditorGUILayout.IntField("Prefabs amount:", currentPrefabsAmount);
        //        EditorGUI.EndDisabledGroup();

        //        int newPrefabsAmount = currentPrefabsAmount;

        //        if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(20)) && newPrefabsAmount > 0)
        //        {
        //            GUI.FocusControl(null);
        //            newPrefabsAmount--;
        //        }

        //        if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(20)))
        //        {
        //            GUI.FocusControl(null);
        //            newPrefabsAmount++;
        //        }

        //        EditorGUILayout.EndHorizontal();

        //        if (newPrefabsAmount != currentPrefabsAmount)
        //        {
        //            if (poolProperty != null)
        //            {
        //                poolProperty.FindPropertyRelative("multiPoolPrefabsList").arraySize = newPrefabsAmount;
        //            }
        //            else
        //            {
        //                if (newPrefabsAmount == 0)
        //                {
        //                    poolBuilder.multiPoolPrefabsList.Clear();
        //                }
        //                else if (newPrefabsAmount < poolBuilder.multiPoolPrefabsList.Count)
        //                {
        //                    int itemsToRemove = poolBuilder.multiPoolPrefabsList.Count - newPrefabsAmount;
        //                    poolBuilder.multiPoolPrefabsList.RemoveRange(poolBuilder.multiPoolPrefabsList.Count - itemsToRemove - 1, itemsToRemove);
        //                }
        //                else
        //                {
        //                    int itemsToAdd = newPrefabsAmount - poolBuilder.multiPoolPrefabsList.Count;
        //                    for (int j = 0; j < itemsToAdd; j++)
        //                    {
        //                        //poolBuilder.multiPoolPrefabsList.Add(new Pool.MultiPoolPrefab());
        //                    }
        //                }
        //            }

        //            if (poolProperty != null)
        //            {
        //                if (newPrefabsAmount > currentPrefabsAmount)
        //                {
        //                    for (int i = 0; i < newPrefabsAmount - currentPrefabsAmount; i++)
        //                    {
        //                        poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(currentPrefabsAmount + i).FindPropertyRelative("prefab").objectReferenceValue = null;
        //                        poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(currentPrefabsAmount + i).FindPropertyRelative("weight").intValue = 0;
        //                        poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(currentPrefabsAmount + i).FindPropertyRelative("isWeightLocked").boolValue = false;
        //                    }
        //                }

        //                serializedObject.ApplyModifiedProperties();
        //                recalculateWeightsAtPoolMethodInfo.Invoke(serializedObject.targetObject, new object[] { poolIndex });
        //            }
        //            else
        //            {
        //                poolBuilder.RecalculateWeights();
        //            }

        //            currentPrefabsAmount = newPrefabsAmount;
        //        }

        //        // prefabs list
        //        GUILayout.Space(-2f);
        //        EditorGUILayout.BeginHorizontal();
        //        EditorGUILayout.LabelField("objects", multiListLablesStyle, GUILayout.MaxHeight(10f));
        //        GUILayout.Space(-25);
        //        EditorGUILayout.LabelField("weights", multiListLablesStyle, GUILayout.Width(75), GUILayout.MaxHeight(10f));
        //        EditorGUILayout.EndHorizontal();
        //        float weightsSum = 0f;

        //        for (int j = 0; j < currentPrefabsAmount; j++)
        //        {
        //            EditorGUILayout.BeginHorizontal();

        //            // object 
        //            GameObject currentPrefab = poolProperty != null ? (GameObject)poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(j).FindPropertyRelative("prefab").objectReferenceValue : poolBuilder.multiPoolPrefabsList[j].prefab;
        //            GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(currentPrefab, typeof(GameObject), true);

        //            if (newPrefab != currentPrefab)
        //            {
        //                if (poolProperty != null)
        //                {
        //                    poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(j).FindPropertyRelative("prefab").objectReferenceValue = newPrefab;
        //                }
        //                else
        //                {
        //                    //poolBuilder.multiPoolPrefabsList[j] = new Pool.MultiPoolPrefab(newPrefab, poolBuilder.multiPoolPrefabsList[j].weight, poolBuilder.multiPoolPrefabsList[j].isWeightLocked);
        //                }
        //            }

        //            // weight
        //            bool isWeightLocked = poolProperty != null ? poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(j).FindPropertyRelative("isWeightLocked").boolValue : poolBuilder.multiPoolPrefabsList[j].isWeightLocked;
        //            EditorGUI.BeginDisabledGroup(isWeightLocked);

        //            int currentWeight = poolProperty != null ? poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(j).FindPropertyRelative("weight").intValue : poolBuilder.multiPoolPrefabsList[j].weight;


        //            int newWeight = EditorGUILayout.DelayedIntField(Math.Abs(currentWeight), GUILayout.Width(75));
        //            if (newWeight != currentWeight)
        //            {
        //                if (poolProperty != null)
        //                {
        //                    poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(j).FindPropertyRelative("weight").intValue = newWeight;
        //                }
        //                else
        //                {
        //                    //poolBuilder.multiPoolPrefabsList[j] = new Pool.MultiPoolPrefab(newPrefab, newWeight, poolBuilder.multiPoolPrefabsList[j].isWeightLocked);
        //                }
        //            }

        //            EditorGUI.EndDisabledGroup();

        //            weightsSum += newWeight;

        //            // lock
        //            GUI.contentColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        //            if (GUILayout.Button(isWeightLocked ? lockedIconGUIContent : unlockedIconGUIContent, centeredTextStyle, GUILayout.Height(13f), GUILayout.Width(13f)))
        //            {
        //                GUI.FocusControl(null);

        //                if (poolProperty != null)
        //                {
        //                    poolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(j).FindPropertyRelative("isWeightLocked").boolValue = !isWeightLocked;
        //                }
        //                else
        //                {
        //                    //poolBuilder.multiPoolPrefabsList[j] = new Pool.MultiPoolPrefab(newPrefab, poolBuilder.multiPoolPrefabsList[j].weight, !poolBuilder.multiPoolPrefabsList[j].isWeightLocked);
        //                }
        //            }
        //            GUI.contentColor = defaultColor;

        //            EditorGUILayout.EndHorizontal();
        //        }

        //        GUILayout.Space(5f);

        //        if (currentPrefabsAmount != 0 && weightsSum != 100)
        //        {
        //            EditorGUILayout.BeginHorizontal();

        //            EditorGUILayout.HelpBox("Weights sum should be 100 (current " + weightsSum + ").", MessageType.Warning);

        //            if (GUILayout.Button("Recalculate", GUILayout.Height(40f), GUILayout.Width(76)))
        //            {
        //                GUI.FocusControl(null);

        //                recalculateWeightsAtPoolMethodInfo.Invoke(serializedObject.targetObject, new object[] { poolIndex });

        //                // pool.RecalculateWeights();
        //            }

        //            EditorGUILayout.EndHorizontal();
        //        }
        //    }

        //    if (!(bool)isAllPrefabsAssignedAtPoolMethodInfo.Invoke(serializedObject.targetObject, new object[] { poolIndex }) && newPoolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME))
        //    {
        //        EditorGUILayout.HelpBox("Please assign all prefabs references.", MessageType.Warning);
        //    }


        //    // pool size ///////////
        //    int currentSize = poolProperty != null ? poolProperty.FindPropertyRelative("size").intValue : poolBuilder.size;

        //    //if (currentPoolType == Pool.PoolType.Single)
        //    //{
        //    //    int newSize = EditorGUILayout.IntField("Pool size: ", currentSize);

        //    //    if (poolProperty != null)
        //    //    {
        //    //        poolProperty.FindPropertyRelative("size").intValue = newSize;
        //    //    }
        //    //    else
        //    //    {
        //    //        poolBuilder.size = newSize;
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    EditorGUILayout.BeginHorizontal();

        //    //    int newSize = EditorGUILayout.IntField("Pool size: ", currentSize);

        //    //    newSize = newSize >= 0 ? newSize : 0;

        //    //    if (poolProperty != null)
        //    //    {
        //    //        poolProperty.FindPropertyRelative("size").intValue = newSize;
        //    //    }
        //    //    else
        //    //    {
        //    //        poolBuilder.size = newSize;
        //    //    }

        //    //    GUILayout.FlexibleSpace();

        //    //    int multiPrefabsAmount = poolProperty != null ? poolProperty.FindPropertyRelative("multiPoolPrefabsList").arraySize : poolBuilder.multiPoolPrefabsList != null ? poolBuilder.multiPoolPrefabsList.Count : 0;
        //    //    string lableString = " x " + multiPrefabsAmount + " = " + (newSize * multiPrefabsAmount);
        //    //    GUILayout.Space(-18);
        //    //    EditorGUILayout.LabelField(lableString);

        //    //    EditorGUILayout.EndHorizontal();
        //    //}

        //    // [CACHE IS CURRENTLY DISABLED]
        //    //if (poolManagerRef.useCache && currentSize != poolBuilder.size)
        //    //{
        //    //    UpdateCacheStateList();
        //    //}

        //    // auto size increment toggle ///////////
        //    bool currentAutoSizeIncrementState = poolProperty != null ? poolProperty.FindPropertyRelative("autoSizeIncrement").boolValue : poolBuilder.autoSizeIncrement;
        //    bool newAutoSizeIncrementState = EditorGUILayout.Toggle("Will grow: ", currentAutoSizeIncrementState);

        //    if (poolProperty != null)
        //    {
        //        poolProperty.FindPropertyRelative("autoSizeIncrement").boolValue = newAutoSizeIncrementState;
        //    }
        //    else
        //    {
        //        poolBuilder.autoSizeIncrement = newAutoSizeIncrementState;
        //    }

        //    // objects parrent ///////////
        //    Transform currentContainer = poolProperty != null ? (Transform)poolProperty.FindPropertyRelative("objectsContainer").objectReferenceValue : poolBuilder.objectsContainer;
        //    Transform newContainer = (Transform)EditorGUILayout.ObjectField("Objects parrent", currentContainer, typeof(Transform), true);

        //    if (poolProperty != null)
        //    {
        //        poolProperty.FindPropertyRelative("objectsContainer").objectReferenceValue = newContainer;
        //    }
        //    else
        //    {
        //        poolBuilder.objectsContainer = newContainer;
        //    }


        //    if (EditorGUI.EndChangeCheck())
        //    {
        //        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        //    }

        //    return poolBuilder;
        //}

        //private void RenamePool(SerializedProperty poolProperty, PoolSettings poolBuilder, string newName)
        //{
        //    if (poolProperty != null)
        //    {
        //        poolProperty.FindPropertyRelative("name").stringValue = newName;
        //        serializedObject.ApplyModifiedProperties();

        //        // sorting pools list

        //        int newIndex = -1;
        //        int oldIndex = -1;

        //        for (int i = 0; i < poolsListProperty.arraySize; i++)
        //        {
        //            int comparingResult = newName.CompareTo(poolsListProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);

        //            if (newIndex == -1 && comparingResult == -1)
        //            {
        //                newIndex = i;

        //                if (oldIndex != -1)
        //                    break;
        //            }

        //            if (comparingResult == 0)
        //            {
        //                oldIndex = i;

        //                if (newIndex != -1)
        //                    break;
        //            }
        //        }

        //        if (newIndex == -1)
        //        {
        //            newIndex = poolsListProperty.arraySize - 1;
        //        }

        //        selectedPoolIndex = newIndex;
        //        poolsListProperty.MoveArrayElement(oldIndex, newIndex);
        //        serializedObject.ApplyModifiedProperties();
        //    }
        //    else
        //    {
        //        poolBuilder = poolBuilder.SetName(newName);
        //    }
        //}

        private void CancelNewPoolCreation()
        {
            //if (newPoolBuilder.name.Equals(EMPTY_POOL_BUILDER_NAME))
            //    return;

            //newPoolBuilder = newPoolBuilder.Reset().SetName(EMPTY_POOL_BUILDER_NAME);
            lastRenamingName = RENAMING_EMPTY_STRING;
            isNameAllowed = true;
            isNameAlreadyExisting = false;
            skipEmptyNameWarning = false;
        }

        private string GetPoolName(int poolIndex)
        {
            string poolName = poolsListProperty.GetArrayElementAtIndex(poolIndex).FindPropertyRelative("name").stringValue;
            //poolsList[poolIndex].Name;

            // [CACHE IS CURRENTLY DISABLED]
            //if (poolManagerRef.useCache)
            //{
            //    if (poolsCacheList.IsNullOrEmpty() || poolsCacheDeltaList.IsNullOrEmpty() || poolIndex > poolsCacheDeltaList.Count || poolIndex > poolsCacheList.Count)
            //    {
            //        UpdateCacheStateList();
            //    }

            //    // there is not cache for current scene returning
            //    if (poolsCacheList.IsNullOrEmpty())
            //    {
            //        return poolName;
            //    }

            //    int delta = poolsCacheDeltaList[poolIndex];

            //    if (poolsCacheList[poolIndex] != null && poolsCacheList[poolIndex].ignoreCache)
            //    {
            //        poolName += "   [cache ignored]";
            //    }
            //    else if (delta != 0)
            //    {
            //        poolName += "   " + CacheDeltaToState(delta);
            //    }
            //}

            return poolName;
        }

        //private void AddNewSinglePool(GameObject prefab = null)
        //{
        //    selectedPoolIndex = -1;

        //    newPoolBuilder = new PoolSettings(prefab != null ? prefab.name : string.Empty, prefab, 10, true);
        //    IsNameAllowed(newPoolBuilder.name);
        //}

        //private void AddNewMultiPool(List<Pool.MultiPoolPrefab> prefabs = null)
        //{
        //    selectedPoolIndex = -1;

        //    string name = (prefabs != null && prefabs.Count != 0) ? prefabs[0].prefab.name : string.Empty;
        //    newPoolBuilder = new PoolSettings(name, prefabs, 10, true);

        //    IsNameAllowed(newPoolBuilder.name);
        //}

        private void ConfirmPoolCreation()
        {
            skipEmptyNameWarning = false;

            //if (IsNameAllowed(newPoolBuilder.name))
            //{
            //    Undo.RecordObject(target, "New pool added");

            //    int poolsAmount = serializedObject.FindProperty("poolsList").arraySize;
            //    poolsAmount++;
            //    serializedObject.FindProperty("poolsList").arraySize = poolsAmount;

            //    SerializedProperty newPoolProperty = serializedObject.FindProperty("poolsList").GetArrayElementAtIndex(poolsAmount - 1);

            //    newPoolProperty.FindPropertyRelative("name").stringValue = newPoolBuilder.name;
            //    newPoolProperty.FindPropertyRelative("type").enumValueIndex = (int)newPoolBuilder.type;
            //    newPoolProperty.FindPropertyRelative("singlePoolPrefab").objectReferenceValue = newPoolBuilder.singlePoolPrefab;
            //    newPoolProperty.FindPropertyRelative("multiPoolPrefabsList").arraySize = newPoolBuilder.multiPoolPrefabsList.Count;

            //    for (int i = 0; i < newPoolBuilder.multiPoolPrefabsList.Count; i++)
            //    {
            //        newPoolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = newPoolBuilder.multiPoolPrefabsList[i].prefab;
            //        newPoolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(i).FindPropertyRelative("weight").intValue = newPoolBuilder.multiPoolPrefabsList[i].weight;
            //        newPoolProperty.FindPropertyRelative("multiPoolPrefabsList").GetArrayElementAtIndex(i).FindPropertyRelative("isWeightLocked").boolValue = newPoolBuilder.multiPoolPrefabsList[i].isWeightLocked;
            //    }

            //    newPoolProperty.FindPropertyRelative("size").intValue = newPoolBuilder.size;
            //    newPoolProperty.FindPropertyRelative("autoSizeIncrement").boolValue = newPoolBuilder.autoSizeIncrement;
            //    newPoolProperty.FindPropertyRelative("objectsContainer").objectReferenceValue = newPoolBuilder.objectsContainer;

            //    serializedObject.ApplyModifiedProperties();

            //    recalculateWeightsAtPoolMethodInfo.Invoke(serializedObject.targetObject, new object[] { poolsAmount - 1 });

            //    for (int i = 0; i < poolsListProperty.arraySize; i++)
            //    {
            //        if (poolsListProperty.GetArrayElementAtIndex(poolsAmount - 1).FindPropertyRelative("name").stringValue.CompareTo(poolsListProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue) == -1)
            //        {
            //            poolsListProperty.MoveArrayElement(poolsAmount - 1, i);
            //            break;
            //        }
            //    }

            //    serializedObject.ApplyModifiedProperties();

            //    newPoolBuilder = newPoolBuilder.Reset().SetName(EMPTY_POOL_BUILDER_NAME);
            //    prevNewPoolName = string.Empty;

            //    lastRenamingName = RENAMING_EMPTY_STRING;
            //    isNameAllowed = true;
            //    isNameAlreadyExisting = false;

            //    searchText = "";
            //}
        }

        private void DeletePool(int indexOfPoolToRemove)
        {
            Undo.RecordObject(target, "Pool deleted");

            serializedObject.FindProperty("poolsList").RemoveFromVariableArrayAt(indexOfPoolToRemove);

            selectedPoolIndex = -1;
            lastRenamingName = RENAMING_EMPTY_STRING;
            isNameAllowed = true;
            isNameAlreadyExisting = false;
        }

        private bool IsNameAllowed(string nameToCheck)
        {
            if (nameToCheck.Equals(string.Empty))
            {
                isNameAllowed = false;
                isNameAlreadyExisting = false;
                return false;
            }

            if (serializedObject.FindProperty("poolsList").arraySize == 0)
            {
                isNameAllowed = true;
                isNameAlreadyExisting = false;
                return true;
            }

            if (IsNameAlreadyExisting(nameToCheck))
            {
                isNameAllowed = false;
                isNameAlreadyExisting = true;
                return false;
            }
            else
            {
                isNameAllowed = true;
                isNameAlreadyExisting = false;
                return true;
            }
        }

        private bool IsNameAlreadyExisting(string nameToCheck)
        {
            for (int i = 0; i < poolsListProperty.arraySize; i++)
            {
                if (poolsListProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue.Equals(nameToCheck))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

// -----------------
// Pool Manager v 1.6.5
// -----------------