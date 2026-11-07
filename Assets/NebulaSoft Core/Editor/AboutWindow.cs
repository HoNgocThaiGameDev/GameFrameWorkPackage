using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NebulaSoft
{

    public class AboutWindow : EditorWindow
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(500, 155);

        private const string WINDOW_TITLE = "About";

        private const string DEFAULT_VALUE = "[unknown]";
        private const string DEFAULT_DOCUMENTATION_URL = "";
        private static readonly string PROJECT_DESCRIPTION = @"Thank you for purchasing {0}.\nBefore you start working with project, read the documentation.\nPlease, leave a review and rate the asset.";

        private GUIStyle descriptionStyle;
        private GUIStyle projectStyle;
        private GUIStyle boxStyle;

        private GUIContent documentationButtonContent;

        private string description;
        
        private string coreVersion;
        private string projectVersion;
        private string documentationUrl;
        private float defaultLength;

        [MenuItem("Window/NebulaSoft Core/About", priority = 10000)]
        static void ShowWindow()
        {
            AboutWindow tempWindow = (AboutWindow)GetWindow(typeof(AboutWindow), true, WINDOW_TITLE);
            tempWindow.minSize = WINDOW_SIZE;
            tempWindow.maxSize = WINDOW_SIZE;
            tempWindow.titleContent = new GUIContent(WINDOW_TITLE, EditorCustomStyles.GetIcon("icon_title"));
        }

        protected void OnEnable()
        {
            EditorCustomStyles.CheckStyles();
                        
            TextAsset coreChangelogText = EditorUtils.GetAsset<TextAsset>("Core Changelog");
            if(coreChangelogText != null && !string.IsNullOrEmpty(coreChangelogText.text))
            {
                string[] lines = coreChangelogText.text.Split('\n');
                if (lines.Length > 0)
                {
                    coreVersion = lines[0];
                }
                else
                {
                    coreVersion = DEFAULT_VALUE;
                }
            }
            else
            {
                coreVersion = DEFAULT_VALUE;
            }

            TextAsset templateChangelogText = EditorUtils.GetAsset<TextAsset>("Template Changelog");
            if (templateChangelogText != null && !string.IsNullOrEmpty(templateChangelogText.text))
            {
                string[] lines = templateChangelogText.text.Split('\n');
                if(lines.Length > 0)
                {
                    projectVersion = lines[0];
                }
                else
                {
                    projectVersion = DEFAULT_VALUE;
                }
            }
            else
            {
                projectVersion = DEFAULT_VALUE;
            }

            TextAsset documentationText = EditorUtils.GetAsset<TextAsset>("DOCUMENTATION");
            if (documentationText != null && !string.IsNullOrEmpty(documentationText.text))
            {
                string[] lines = documentationText.text.Split('\n');
                if(lines.Length > 0)
                {
                    documentationUrl = lines[^1];
                }
                else
                {
                    documentationUrl = DEFAULT_DOCUMENTATION_URL;
                }
            }
            else
            {
                documentationUrl = DEFAULT_DOCUMENTATION_URL;
            }

            boxStyle = new GUIStyle(EditorCustomStyles.Skin.box);
            boxStyle.margin = new RectOffset(5, 5, 5, 5);
            boxStyle.overflow = new RectOffset(0, 0, 0, 0);
            boxStyle.padding = new RectOffset(5, 5, 5, 5);

            description = string.Format(PROJECT_DESCRIPTION, Application.productName).Replace("\\n", "\n");

            descriptionStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            descriptionStyle.wordWrap = true;

            documentationButtonContent = new GUIContent(EditorCustomStyles.ICON_SPACE + "Documentation", EditorCustomStyles.GetIcon("icon_documentation"));

            projectStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            projectStyle.alignment = TextAnchor.MiddleCenter;
            projectStyle.wordWrap = false;
            projectStyle.clipping = TextClipping.Overflow;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("NebulaSoft Core", projectStyle, GUILayout.Width(110), GUILayout.Height(80));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(EditorCustomStyles.padding05, GUILayout.Height(21), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("GREETINGS!", EditorCustomStyles.labelBold, GUILayout.ExpandHeight(true), GUILayout.Width(110));

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(documentationUrl) && GUILayout.Button(documentationButtonContent, EditorCustomStyles.button, GUILayout.Height(22)))
            {
                Application.OpenURL(documentationUrl);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(description, descriptionStyle);

            defaultLength = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 115;

            EditorGUILayout.LabelField("Project version", projectVersion);
            EditorGUILayout.LabelField("Core version", coreVersion);

            EditorGUIUtility.labelWidth = defaultLength;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }
    }
}
