using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Linq;

namespace NebulaSoft
{
    public class SavePresetsWindow : EditorWindow
    {
        private const string LAST_SELECTED_TAB = "save_presets_window_last_selected_tab";
        private const string SORT_TYPE = "save_presets_window_sort_type__";


        private const int UPDATE_BUTTON_WIDTH = 80;
        private const int ACTIVATE_BUTTON_WIDTH = 80;
        private const int SHARE_BUTTON_WIDTH = 18;
        private const int DATE_LABEL_WIDTH = 50;
        private const int DEFAULT_SPACE = 8;

        private static readonly Vector2 WINDOW_SIZE = new Vector2(490, 495);
        private static readonly string WINDOW_TITLE = "Save Presets";
        private int tempTabIndex;
        private Vector2 scrollView;

        private List<SavePreset> allSavePresets;
        private string tempPresetName;
        private List<SavePreset> selectedSavePresets;
        private ReorderableList savePresetsList;
        private Rect workRect;
        private string[] tabNames;
        private Dictionary<string,TabSortType> sortTypes;
        private int selectedTabIndex;
        private TabSortType currentSortType;
        private GUIContent shareButtonContent;
        private GUIContent arrowDownContent;
        private Rect importRect;
        private GUIStyle spacedContentStyle;
        private GUIStyle modifiedBoxStyle;
        private float backupLabelWidth;

        [MenuItem("Tools/Save Presets")]
        [MenuItem("Window/Save Presets")]
        static void ShowWindow()
        {
            SavePresetsWindow tempWindow = (SavePresetsWindow)GetWindow(typeof(SavePresetsWindow), false, WINDOW_TITLE);
            tempWindow.minSize = WINDOW_SIZE;
            tempWindow.titleContent = new GUIContent(WINDOW_TITLE, EditorCustomStyles.GetIcon("icon_title"));
        }

        protected void OnEnable()
        {
            SavePresets.saveDataMofied = false;
            allSavePresets = new List<SavePreset>();
            string directoryPath = SavePresets.GetDirectoryPath();


            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string[] directoryEntries = Directory.GetDirectories(directoryPath);
            sortTypes = new Dictionary<string, TabSortType>();
            string directory = SavePresets.DEFAULT_DIRECTORY;
            TabSortType sortType;
            string[] fileEntries;
            DateTime creationTime = DateTime.Now;
            List<string> tempTabNames = new List<string>();
            tempTabNames.Add(SavePresets.DEFAULT_DIRECTORY); // Make sure it first

            for (int i = 0; i < directoryEntries.Length; i++)
            {
                directory = SavePresets.GetFileName(directoryEntries[i]);
                sortType = GetSortType(directory);
                fileEntries = Directory.GetFiles(directoryEntries[i]);

                if(fileEntries.Length > 0)
                {
                    if (!tempTabNames.Contains(directory))
                    {
                        tempTabNames.Add(directory);
                    }

                    sortTypes.Add(directory, sortType);
                }

                for (int j = 0; j < fileEntries.Length; j++)
                {
                    if (!fileEntries[j].EndsWith(SavePresets.META_SUFFIX))
                    {
                        creationTime = File.GetCreationTimeUtc(fileEntries[j]);
                        allSavePresets.Add(new SavePreset(SavePresets.GetFileName(fileEntries[j]), creationTime, fileEntries[j], directory));
                    }
                }
            }

            if (!sortTypes.ContainsKey(SavePresets.DEFAULT_DIRECTORY))
            {
                sortTypes.Add(SavePresets.DEFAULT_DIRECTORY, GetSortType(SavePresets.DEFAULT_DIRECTORY));
            }



            tabNames = tempTabNames.ToArray();
            selectedSavePresets = new List<SavePreset>();
            selectedTabIndex = Mathf.Clamp(EditorPrefs.GetInt(LAST_SELECTED_TAB, selectedTabIndex), 0, tabNames.Length - 1);
            SelectTab(selectedTabIndex);

            savePresetsList = new ReorderableList(selectedSavePresets, typeof(SavePreset), false, false, false, true);
            savePresetsList.elementHeight = 26;
            savePresetsList.drawElementCallback = DrawElement;
            savePresetsList.onRemoveCallback = RemoveCallback;
            savePresetsList.drawNoneElementCallback = DrawNoneElementCallback;
            workRect = new Rect();
            shareButtonContent = new GUIContent(EditorCustomStyles.GetIcon("icon_share"));
            arrowDownContent = new GUIContent(EditorCustomStyles.GetIcon("icon_arrow_down"));
            spacedContentStyle = new GUIStyle();
            spacedContentStyle.padding = new RectOffset(4, 4, 4, 4);
            modifiedBoxStyle = new GUIStyle(EditorCustomStyles.box);
            modifiedBoxStyle.margin = new RectOffset(0, 0, 0, 0);
            modifiedBoxStyle.padding = new RectOffset(8, 8, 8, 8);
            modifiedBoxStyle.overflow = new RectOffset(0, 0, 0, 0);
        }

        

        private void SelectTab(int index)
        {
            allSavePresets.Sort(TypeAndCustomSort);
            selectedTabIndex = index;
            currentSortType = sortTypes[tabNames[selectedTabIndex]];
            EditorPrefs.SetInt(LAST_SELECTED_TAB, selectedTabIndex);
            selectedSavePresets.Clear();

            for (int i = 0; i < allSavePresets.Count; i++)
            {
                if (allSavePresets[i].folderName.Equals(tabNames[selectedTabIndex]))
                {
                    selectedSavePresets.Add(allSavePresets[i]);
                }
            }
        }
        

        private void RemoveCallback(ReorderableList list)
        {
            if (EditorUtility.DisplayDialog("This preset will be removed!", "Are you sure?", "Remove", "Cancel"))
            {
                if (list.index >= 0 && list.index < selectedSavePresets.Count)
                {
                    SavePresets.RemoveSave(selectedSavePresets[list.index].fileName, selectedSavePresets[list.index].folderName);
                }

                savePresetsList.ClearSelection();
            }
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            workRect.Set(rect.x + rect.width, rect.y + 4, 0, 18);

            workRect.x -= SHARE_BUTTON_WIDTH + DEFAULT_SPACE;
            workRect.width = SHARE_BUTTON_WIDTH;

            if(GUI.Button(workRect, shareButtonContent))
            {
                EditorUtility.RevealInFinder(selectedSavePresets[index].path);
            }

            if (selectedTabIndex == 0)
            {
                workRect.x -= UPDATE_BUTTON_WIDTH + DEFAULT_SPACE;
                workRect.width = UPDATE_BUTTON_WIDTH;


                if (GUI.Button(workRect, "Update"))
                {
                    if (EditorUtility.DisplayDialog("This preset will rewrited!", "Are you sure?", "Rewrite", "Cancel"))
                    {
                        SavePresets.CreateSave(selectedSavePresets[index].fileName, selectedSavePresets[index].folderName);
                    }
                }
            }

            workRect.x -= ACTIVATE_BUTTON_WIDTH + DEFAULT_SPACE;
            workRect.width = ACTIVATE_BUTTON_WIDTH;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            if (GUI.Button(workRect, "Activate", EditorCustomStyles.buttonGreen))
            {
                SavePresets.LoadSave(selectedSavePresets[index].fileName, tabNames[selectedTabIndex]);
            }

            EditorGUI.EndDisabledGroup();

            workRect.x -= DATE_LABEL_WIDTH + DEFAULT_SPACE;
            workRect.width = DATE_LABEL_WIDTH;

            GUI.Label(workRect, selectedSavePresets[index].creationDate.ToString("dd.MM"));

            workRect.x -= DEFAULT_SPACE;
            workRect.width = workRect.x - rect.x;
            workRect.x = rect.x;

            GUI.Label(workRect, selectedSavePresets[index].fileName);
            
        }

        private void DrawNoneElementCallback(Rect rect)
        {
            GUI.Label(rect, "There are no saves yet");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(spacedContentStyle);

            if (tabNames.Length > 1)
            {
                tempTabIndex = GUILayout.Toolbar(selectedTabIndex, tabNames, EditorCustomStyles.tab);
                GUILayout.Space(5);

                if (tempTabIndex != selectedTabIndex)
                {
                    SelectTab(tempTabIndex);
                }
            }


            DisplayOptionsLine();

            scrollView = EditorGUILayout.BeginScrollView(scrollView);
            savePresetsList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            DisplayAddSaveLine();

            EditorGUILayout.EndVertical();

            if (SavePresets.saveDataMofied)
            {
                OnEnable();
            }
        }

        

        private void DisplayOptionsLine()
        {
            EditorGUILayout.BeginHorizontal();

            importRect = EditorGUILayout.BeginHorizontal(GUI.skin.button, GUILayout.Width(50));
            EditorGUILayout.LabelField("Import", GUILayout.Width(40));
            EditorGUILayout.LabelField(arrowDownContent, GUILayout.Width(14));

            EditorGUILayout.EndHorizontal();

            if (GUI.Button(importRect, GUIContent.none, GUIStyle.none))
            {
                ImportFile();
            }

            if (selectedSavePresets.Count == 0)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }

            backupLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50;

            EditorGUILayout.PrefixLabel("Sort by");
            EditorGUIUtility.labelWidth = backupLabelWidth;

            EditorGUI.BeginChangeCheck();
            currentSortType = (TabSortType)EditorGUILayout.EnumPopup(currentSortType);
            if (EditorGUI.EndChangeCheck())
            {
                if (sortTypes[tabNames[selectedTabIndex]] != currentSortType)
                {
                    SetSortType(currentSortType, tabNames[selectedTabIndex]);
                    sortTypes[tabNames[selectedTabIndex]] = currentSortType;
                    OnEnable();
                }
            }

            GUILayout.FlexibleSpace();


            if (GUILayout.Button("Remove all"))
            {
                if (EditorUtility.DisplayDialog("All presets will be removed!", "Are you sure?", "Remove", "Cancel"))
                {
                    savePresetsList.ClearSelection();

                    for (int i = selectedSavePresets.Count - 1; i >= 0; i--)
                    {
                        SavePresets.RemoveSave(selectedSavePresets[i].fileName, selectedSavePresets[i].folderName);
                    }

                    OnEnable();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DisplayAddSaveLine()
        {
            if (selectedTabIndex == 0)
            {
                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginVertical(modifiedBoxStyle);
                EditorGUILayout.LabelField("Add new save");
                EditorGUILayout.BeginHorizontal();

                if (GUI.GetNameOfFocusedControl().Equals("tempPresetName"))
                {
                    if ((Event.current.type == EventType.KeyDown) && (Event.current.keyCode == KeyCode.Return))
                    {
                        ConfirmButtonPressed();
                        Repaint();
                    }
                }

                GUI.SetNextControlName("tempPresetName");
                tempPresetName = EditorGUILayout.TextField(tempPresetName);

                if (GUILayout.Button("Confirm"))
                {
                    ConfirmButtonPressed();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void ConfirmButtonPressed()
        {
            SavePresets.CreateSave(tempPresetName);
            GUI.FocusControl(null);
            tempPresetName = string.Empty;
        }

        private void ImportFile()
        {
            string originalFilePath =  EditorUtility.OpenFilePanel("Select save to import", string.Empty, string.Empty);

            if(originalFilePath.Length == 0)
            {
                return;
            }

            string name = originalFilePath.Substring(originalFilePath.LastIndexOf('/') + 1);

            if (name.Contains('.'))
            {
                Debug.LogError("Selected invalid save preset.");
                return;
            }

            string defaultDirectoryPath = SavePresets.GetDirectoryPath(SavePresets.DEFAULT_DIRECTORY);

            if (!Directory.Exists(defaultDirectoryPath)) //Creating SavePresets folder
            {
                Directory.CreateDirectory(defaultDirectoryPath);
            }

            string newFilePath = Path.Combine(defaultDirectoryPath, name);
            File.Copy(originalFilePath, newFilePath, true);
            SavePresets.SetId(name, SavePresets.DEFAULT_DIRECTORY, name);
            SavePresets.saveDataMofied = true;
            SelectTab(0);
            
        }

        private int TypeAndCustomSort(SavePreset x, SavePreset y)
        {
            int value = x.folderName.CompareTo(y.folderName);

            if (value == 0)
            {
                if (sortTypes[x.folderName] == TabSortType.Name)
                {
                    return x.fileName.CompareTo(y.fileName);
                }
                else if (sortTypes[x.folderName] == TabSortType.CreationDate)
                {
                    return x.creationDate.CompareTo(y.creationDate);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return value;
            }
        }

        private TabSortType GetSortType(string folderName = SavePresets.DEFAULT_DIRECTORY)
        {
            if(folderName.Equals(SavePresets.DEFAULT_DIRECTORY))
            {
                return (TabSortType)EditorPrefs.GetInt(SORT_TYPE + folderName, (int)TabSortType.CreationDate);
            }
            else
            {
                return (TabSortType)EditorPrefs.GetInt(SORT_TYPE + folderName, (int)TabSortType.Name);
            }
        }

        private void SetSortType(TabSortType tabSortType, string folderName = SavePresets.DEFAULT_DIRECTORY)
        {
            EditorPrefs.SetInt(SORT_TYPE + folderName, (int)tabSortType);
        }

        private class SavePreset
        {
            public string fileName;
            public DateTime creationDate;
            public string path;
            public string folderName;

            public SavePreset(string fileName, DateTime creationDate, string path, string folderName)
            {
                this.fileName = fileName;
                this.creationDate = creationDate;
                this.path = path;
                this.folderName = folderName;
            }
        }

        [System.Serializable]
        private enum TabSortType
        {
            CreationDate = 0,
            Name = 1,
        }
    }
}