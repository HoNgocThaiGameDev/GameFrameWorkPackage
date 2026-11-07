#pragma warning disable 0649

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace NebulaSoft
{
    public class AudioSettingsEditorWindow : EditorWindow
    {
        private static AudioSettingsEditorWindow window;

        private readonly Vector2 WINDOW_SIZE = new Vector2(420, 100);
        private readonly string[] PRESETS_NAME = new string[] { "Standalone", "iOS", "Android" };

        private readonly Preset[] PRESETS = new Preset[]
        {
            new Preset()
            {
                name = "Music CPU",
                description = "For Music/Ambient audio files. Requires some CPU and disk space.",
                forceToMono = true,
                presets = new Preset.Settings[]
                {
                    new Preset.Settings()
                    {
                        name = "Default",
                        loadType = AudioClipLoadType.Streaming,
                        compressionFormat = AudioCompressionFormat.Vorbis,
                        quality = 1,
                    }
                }
            },
            new Preset()
            {
                name = "Music RAM",
                description = "For Music/Ambient audio files. Requires RAM without disk space.",
                presets = new Preset.Settings[]
                {
                    new Preset.Settings()
                    {
                        name = "Default",
                        loadType = AudioClipLoadType.CompressedInMemory,
                        compressionFormat = AudioCompressionFormat.Vorbis,
                        quality = 1
                    }
                }
            },
            new Preset()
            {
                name = "Frequently Played FX (short)",
                description = "For short sound effects (bullet shots, footsteps).",
                presets = new Preset.Settings[]
                {
                    new Preset.Settings()
                    {
                        name = "Default",
                        loadType = AudioClipLoadType.DecompressOnLoad,
                        compressionFormat = AudioCompressionFormat.PCM,
                    }
                }
            },
            new Preset()
            {
                name = "Frequently Played FX (medium)",
                description = "For medium sound effects (spell casts, frequent voices).",
                presets = new Preset.Settings[]
                {
                    new Preset.Settings()
                    {
                        name = "Default",
                        loadType = AudioClipLoadType.CompressedInMemory,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                    }
                }
            },
            new Preset()
            {
                name = "Rarely Played FX (short)",
                description = "For short sound effects (win/loose situation).",
                presets = new Preset.Settings[]
                {
                    new Preset.Settings()
                    {
                        name = "Default",
                        loadType = AudioClipLoadType.CompressedInMemory,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                    }
                }
            },
            new Preset()
            {
                name = "Rarely Played FX (medium)",
                description = "For medium sound effects (win/loose situation short music).",
                presets = new Preset.Settings[]
                {
                    new Preset.Settings()
                    {
                        name = "Default",
                        loadType = AudioClipLoadType.CompressedInMemory,
                        compressionFormat = AudioCompressionFormat.Vorbis,
                        quality = 1
                    }
                }
            },
        };

        private List<AudioImporter> importedAudio = new List<AudioImporter>();

        private int selectedPresetID = -1;
        private int selectedAudioID = 0;
        private AudioClip selectedAudioClip;

        private MethodInfo playClipMethod;
        private MethodInfo stopClipMethod;

        private void OnEnable()
        {
            playClipMethod = PlayClipMethod();
            stopClipMethod = StopClipMethod();
        }

        private void OnDisable()
        {
            if (selectedAudioClip != null)
                StopClip();
        }

        private static void Open(UnityEngine.Object[] objects)
        {
            if (window == null)
            {
                window = (AudioSettingsEditorWindow)GetWindow(typeof(AudioSettingsEditorWindow), true, "Audio Import", true);
                window.importedAudio = new List<AudioImporter>();

                Vector2 windowSize = new Vector2(window.WINDOW_SIZE.x, window.WINDOW_SIZE.y + window.GetHeight());
                window.minSize = windowSize;
                window.maxSize = windowSize;

                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i] is AudioClip)
                    {
                        window.importedAudio.Add((AudioImporter)AudioImporter.GetAtPath(AssetDatabase.GetAssetPath(objects[i])));
                    }
                }

                window.SelectAudio(0);
            }
        }

        private static void Open(AudioImporter audioImporter)
        {
            if (window == null)
            {
                window = (AudioSettingsEditorWindow)GetWindow(typeof(AudioSettingsEditorWindow), true, "Audio Import", true);
                window.importedAudio = new List<AudioImporter>();

                Vector2 windowSize = new Vector2(window.WINDOW_SIZE.x, window.WINDOW_SIZE.y + window.GetHeight());
                window.minSize = windowSize;
                window.maxSize = windowSize;

                window.importedAudio.Add(audioImporter);

                window.SelectAudio(0);
            }
            else
            {
                window.importedAudio.Add(audioImporter);

                window.titleContent = new GUIContent("Imported audio: " + (window.selectedAudioID + 1) + "/" + window.importedAudio.Count);
            }
        }

        private void SelectAudio(int index)
        {
            selectedAudioID = index;

            window.titleContent = new GUIContent("Imported audio: " + (selectedAudioID + 1) + "/" + importedAudio.Count);

            selectedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(importedAudio[selectedAudioID].assetPath);

            Repaint();
        }

        private void OnGUI()
        {
            if (selectedAudioClip == null)
                selectedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(importedAudio[selectedAudioID].assetPath);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name: ", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField(selectedAudioClip == null ? "Unknown audio" : selectedAudioClip.name);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("▶", GUILayout.Width(18), GUILayout.Height(18)))
            {
                PlayClip(selectedAudioClip);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            for (int i = 0; i < PRESETS.Length; i++)
            {
                if (selectedPresetID == i)
                    GUI.color = Color.green;

                Rect clickRect = EditorGUILayout.BeginVertical(GUI.skin.textField);
                EditorGUILayout.LabelField(new GUIContent(PRESETS[i].name), EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(PRESETS[i].description))
                    EditorGUILayout.LabelField(new GUIContent(PRESETS[i].description), GUI.skin.textField);

                EditorGUILayout.EndVertical();

                if (GUI.Button(clickRect, new GUIContent("", PRESETS[i].description), GUIStyle.none))
                {
                    SelectPreset(i);
                }

                if (selectedPresetID == i)
                    GUI.color = Color.white;
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Skip"))
            {
                StopClip();

                NextAudio();
            }

            if (GUILayout.Button("Init"))
            {
                StopClip();

                Init(selectedAudioID);
            }

            if (GUILayout.Button("Init All"))
            {
                StopClip();

                InitAll(selectedAudioID);
            }
            EditorGUILayout.EndHorizontal();
        }

        private float GetHeight()
        {
            float height = 0;

            for (int i = 0; i < PRESETS.Length; i++)
            {
                if (!string.IsNullOrEmpty(PRESETS[i].name)) height += 20;
                if (!string.IsNullOrEmpty(PRESETS[i].description)) height += 20;
            }

            return height;
        }

        private void SelectPreset(int index)
        {
            selectedPresetID = index;
        }

        private void NextAudio()
        {
            if (selectedAudioID + 1 < importedAudio.Count)
            {
                SelectAudio(selectedAudioID + 1);
            }
            else
            {
                Close();
            }
        }

        private void InitAll(int startIndex = 0)
        {
            int importedAudioCount = importedAudio.Count;
            for (int i = startIndex; i < importedAudioCount; i++)
            {
                Init(i);
            }
        }

        private void Init(int audioID)
        {
            AudioImporter audioImporter = importedAudio[audioID];

            if (selectedPresetID != -1)
            {
                AssetDatabase.StartAssetEditing();

                audioImporter.forceToMono = PRESETS[selectedPresetID].forceToMono;
                audioImporter.loadInBackground = PRESETS[selectedPresetID].loadInBackground;
                audioImporter.ambisonic = PRESETS[selectedPresetID].ambisonic;

                SerializedObject serializedObject = new SerializedObject(audioImporter);
                SerializedProperty normalizeProperty = serializedObject.FindProperty("m_Normalize");

                serializedObject.Update();
                normalizeProperty.boolValue = PRESETS[selectedPresetID].normalize;
                serializedObject.ApplyModifiedProperties();

                for(int i = 0; i < PRESETS_NAME.Length; i++)
                {
                    audioImporter.ClearSampleSettingOverride(PRESETS_NAME[i]);
                }

                for (int i = 0; i < PRESETS[selectedPresetID].presets.Length; i++)
                {
                    AudioImporterSampleSettings audioImporterSettings = audioImporter.defaultSampleSettings;

#if UNITY_2022_1_OR_NEWER
                    audioImporterSettings.preloadAudioData = PRESETS[selectedPresetID].presets[i].preloadAudioData;
#elif UNITY_2021
                    audioImporter.preloadAudioData = PRESETS[selectedPresetID].presets[i].preloadAudioData;
#endif

                    audioImporterSettings.loadType = PRESETS[selectedPresetID].presets[i].loadType;
                    audioImporterSettings.compressionFormat = PRESETS[selectedPresetID].presets[i].compressionFormat;
                    audioImporterSettings.quality = (float)PRESETS[selectedPresetID].presets[i].quality / 100;
                    audioImporterSettings.sampleRateSetting = PRESETS[selectedPresetID].presets[i].sampleRate;

                    if (PRESETS[selectedPresetID].presets[i].name == "Default")
                    {
                        audioImporter.defaultSampleSettings = audioImporterSettings;
                    }
                    else
                    {
                        audioImporter.SetOverrideSampleSettings(PRESETS[selectedPresetID].presets[i].name, audioImporterSettings);
                    }
                }
                AssetDatabase.StopAssetEditing();

                audioImporter.SaveAndReimport();
            }

            NextAudio();
        }

        [MenuItem("Assets/Initialize Audio", priority = 80)]
        public static void InitAudioFiles()
        {
            Open(Selection.objects);
        }

        [MenuItem("Assets/Initialize Audio", true, 0)]
        public static bool ValidateInitAudioFiles()
        {
            return Selection.objects != null && Selection.activeObject is AudioClip;
        }

        private void PlayClip(AudioClip clip)
        {
            playClipMethod.Invoke(null, new object[] { clip, 1, false });
        }

        private void StopClip()
        {
            stopClipMethod.Invoke(null, null);
        }

        public static MethodInfo PlayClipMethod()
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            return audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        }

        public static MethodInfo StopClipMethod()
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            return audioUtilClass.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
        }

        private class Preset
        {
            public string name;
            public string description;

            public bool forceToMono;
            public bool normalize;

            public bool loadInBackground;
            public bool ambisonic;

            public Settings[] presets;

            public class Settings
            {
                public string name;

                public AudioClipLoadType loadType = AudioClipLoadType.DecompressOnLoad;
                public bool preloadAudioData;
                public AudioCompressionFormat compressionFormat = AudioCompressionFormat.Vorbis;

                public int quality;
                public AudioSampleRateSetting sampleRate = AudioSampleRateSetting.PreserveSampleRate;
            }
        }
    }
}
