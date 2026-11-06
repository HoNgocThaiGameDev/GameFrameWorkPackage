#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft.EditorTools
{
    /// <summary>
    /// Rebuilds the Quest prefabs from the approved Quest sprite pack. It is intentionally
    /// editor-only so gameplay code remains independent from presentation layout.
    /// </summary>
    internal static class QuestPrefabBuilder
    {
        private const string QuestSpriteFolder = "Assets/Addon/UI/Sprites/Quest/";
        private const string ElementPrefabPath = "Assets/Addon/UI/Prefabs/Quests/QuestElement.prefab";
        private const string PanelPrefabPath = "Assets/Addon/UI/Prefabs/Quests/Panel_quest.prefab";
        private const string NavigationPrefabPath = "Assets/Addon/UI/Prefabs/Shared/LeaderboardBottomNavigation.prefab";

        private static TMP_FontAsset font;

        public static void BuildAll()
        {
            EnsureFolder("Assets/Addon/UI/Prefabs");
            EnsureFolder("Assets/Addon/UI/Prefabs/Quests");

            font = FindUIFont();
            QuestElementView elementPrefab = BuildQuestElement();
            QuestPanelView panelPrefab = BuildQuestPanel(elementPrefab);
            AssignPanelPrefabToNavigation(panelPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = panelPrefab.gameObject;
            Debug.Log("[Quest] QuestElement and Panel_quest prefabs were rebuilt.");
        }

        private static QuestElementView BuildQuestElement()
        {
            GameObject root = CreateRectObject("QuestElement", null);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(960f, 182f);
            LayoutElement rootLayout = root.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = 182f;
            rootLayout.minHeight = 182f;
            QuestElementView view = root.AddComponent<QuestElementView>();

            Image card = CreateImage("Card Background", root.transform, LoadSprite("progressBarPrize"), Color.white, true);
            SetRect(card.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            card.type = Image.Type.Sliced;
            card.raycastTarget = false;

            Image cardTint = CreateImage("Card Tint", root.transform, null, new Color(0.24f, 0.10f, 0.58f, 0.86f), false);
            SetRect(cardTint.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);
            cardTint.raycastTarget = false;

            Image rewardFrame = CreateImage("Reward Frame", root.transform, LoadSprite("icon_quest"), Color.white, true);
            SetRect(rewardFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -66f), new Vector2(156f, 66f));
            rewardFrame.raycastTarget = false;

            Image rewardIcon = CreateImage("Reward Icon", rewardFrame.transform, LoadSprite("coin"), Color.white, true);
            SetRect(rewardIcon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(-27f, -27f), new Vector2(27f, 27f));
            rewardIcon.raycastTarget = false;

            TextMeshProUGUI rewardAmount = CreateText("Reward Amount", rewardFrame.transform, "25", 25f, TextAlignmentOptions.Center, Color.white);
            SetRect(rewardAmount.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.32f), new Vector2(8f, 2f), new Vector2(-8f, -4f));

            TextMeshProUGUI information = CreateText("Information Text", root.transform, "Complete 3 level(s)", 27f, TextAlignmentOptions.Left, Color.white);
            information.textWrappingMode = TextWrappingModes.Normal;
            SetRect(information.rectTransform, new Vector2(0f, 0.48f), new Vector2(0.56f, 0.92f), new Vector2(174f, -2f), Vector2.zero);

            Slider slider = CreateProgressSlider(root.transform, out TextMeshProUGUI progressText);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0f, 0.15f), new Vector2(0.56f, 0.36f), new Vector2(174f, 0f), Vector2.zero);

            Button skipButton = CreateButton("Skip Button", root.transform, LoadSprite("skip_btn"), Color.white, out Image skipBackground);
            SetRect(skipButton.GetComponent<RectTransform>(), new Vector2(0.58f, 0.15f), new Vector2(0.76f, 0.85f), Vector2.zero, Vector2.zero);
            Image skipGlyph = CreateImage("Skip Glyph", skipButton.transform, LoadSprite("main_skip_sprite"), Color.white, true);
            skipGlyph.raycastTarget = false;
            SetRect(skipGlyph.rectTransform, new Vector2(0.13f, 0.29f), new Vector2(0.84f, 0.71f), Vector2.zero, Vector2.zero);
            Image skipAdBadge = CreateImage("Ad Badge", skipButton.transform, LoadSprite("skip_sprite"), Color.white, true);
            skipAdBadge.raycastTarget = false;
            SetRect(skipAdBadge.rectTransform, new Vector2(0.61f, 0.60f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);

            Button goButton = CreateButton("Go Button", root.transform, LoadSprite("yellow_btn"), Color.white, out Image goBackground);
            SetRect(goButton.GetComponent<RectTransform>(), new Vector2(0.77f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI goLabel = CreateText("Go Label", goButton.transform, "GO!", 27f, TextAlignmentOptions.Center, Color.white);
            goLabel.fontStyle = FontStyles.Bold;
            SetRect(goLabel.rectTransform, new Vector2(0.07f, 0.32f), new Vector2(0.93f, 0.68f), Vector2.zero, Vector2.zero);

            Button claimButton = CreateButton("Claim Button", root.transform, LoadSprite("claim_button"), Color.white, out Image claimBackground);
            SetRect(claimButton.GetComponent<RectTransform>(), new Vector2(0.77f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI claimLabel = CreateText("Claim Label", claimButton.transform, "CLAIM", 25f, TextAlignmentOptions.Center, Color.white);
            claimLabel.fontStyle = FontStyles.Bold;
            SetRect(claimLabel.rectTransform, new Vector2(0.05f, 0.31f), new Vector2(0.95f, 0.69f), Vector2.zero, Vector2.zero);

            SetField(view, "informationText", information);
            SetField(view, "claimButton", claimButton);
            SetField(view, "progressSlider", slider);
            SetField(view, "skipButton", skipButton);
            SetField(view, "goButton", goButton);
            SetField(view, "claimButtonRoot", claimButton.gameObject);
            SetField(view, "skipButtonRoot", skipButton.gameObject);
            SetField(view, "goButtonRoot", goButton.gameObject);
            SetField(view, "progressText", progressText);
            SetField(view, "rewardAmountText", rewardAmount);
            SetField(view, "rewardIcon", rewardIcon);
            SetField(view, "claimLabel", claimLabel);

            QuestElementView prefab = SavePrefab(root, ElementPrefabPath).GetComponent<QuestElementView>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static QuestPanelView BuildQuestPanel(QuestElementView elementPrefab)
        {
            GameObject root = CreateRectObject("Panel_quest", null);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            QuestPanelView view = root.AddComponent<QuestPanelView>();

            Image backdrop = CreateImage("Dark Quest Backdrop", root.transform, null, new Color(0.055f, 0.015f, 0.18f, 0.98f), false);
            SetRect(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image heroGlow = CreateImage("Hero Glow", root.transform, null, new Color(0.33f, 0.13f, 0.72f, 0.55f), false);
            SetRect(heroGlow.rectTransform, new Vector2(0.28f, 0.61f), new Vector2(1.03f, 0.95f), Vector2.zero, Vector2.zero);
            Image hero = CreateImage("Quest Animal", root.transform, LoadSprite("sprite_animal"), Color.white, true);
            SetRect(hero.rectTransform, new Vector2(0.40f, 0.61f), new Vector2(1.03f, 0.93f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI title = CreateText("Title", root.transform, "Daily Tasks", 57f, TextAlignmentOptions.Left, Color.white);
            title.fontStyle = FontStyles.Bold;
            SetRect(title.rectTransform, new Vector2(0f, 0.80f), new Vector2(0.67f, 0.89f), new Vector2(54f, 0f), Vector2.zero);

            Image notification = CreateImage("Info", root.transform, LoadSprite("notification"), Color.white, true);
            SetRect(notification.rectTransform, new Vector2(0.67f, 0.80f), new Vector2(0.75f, 0.85f), Vector2.zero, Vector2.zero);

            Image timerBar = CreateImage("Timer Bar", root.transform, LoadSprite("timer_bar"), Color.white, true);
            SetRect(timerBar.rectTransform, new Vector2(0f, 0.735f), new Vector2(0.36f, 0.79f), new Vector2(54f, 0f), Vector2.zero);
            Image sandClock = CreateImage("Sand Clock", timerBar.transform, LoadSprite("sand_clock"), Color.white, true);
            SetRect(sandClock.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.20f, 0.92f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI timer = CreateText("Timer Text", timerBar.transform, "23h 59m", 29f, TextAlignmentOptions.Center, Color.white);
            SetRect(timer.rectTransform, new Vector2(0.20f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            Button closeButton = CreateButton("Close Button", root.transform, null, Color.white, out Image closeTarget);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.88f, 0.86f), new Vector2(0.98f, 0.94f), Vector2.zero, Vector2.zero);
            closeTarget.color = new Color(1f, 1f, 1f, 0.01f);
            TextMeshProUGUI closeText = CreateText("Label", closeButton.transform, "×", 62f, TextAlignmentOptions.Center, Color.white);
            SetRect(closeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image tabStrip = CreateImage("Quest Tab Strip", root.transform, LoadSprite("active_tabDaily"), Color.white, true);
            SetRect(tabStrip.rectTransform, new Vector2(0f, 0.635f), new Vector2(1f, 0.73f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            Button dailyButton = CreateTransparentTabButton("Daily Button", tabStrip.transform, 0f, 1f / 3f);
            Button weeklyButton = CreateTransparentTabButton("Weekly Button", tabStrip.transform, 1f / 3f, 2f / 3f);
            Button eventButton = CreateTransparentTabButton("Events Button", tabStrip.transform, 2f / 3f, 1f);

            Image milestonePanel = CreateImage("Milestone Panel", root.transform, LoadSprite("progressBarPrize"), Color.white, true);
            SetRect(milestonePanel.rectTransform, new Vector2(0f, 0.555f), new Vector2(1f, 0.64f), new Vector2(18f, 0f), new Vector2(-18f, 0f));
            Image milestoneTrack = CreateImage("Milestone Fill", milestonePanel.transform, null, new Color(1f, 0.72f, 0.16f, 1f), false);
            milestoneTrack.type = Image.Type.Filled;
            milestoneTrack.fillMethod = Image.FillMethod.Horizontal;
            milestoneTrack.fillAmount = 0f;
            SetRect(milestoneTrack.rectTransform, new Vector2(0.11f, 0.36f), new Vector2(0.89f, 0.64f), Vector2.zero, Vector2.zero);
            for (int index = 0; index < 5; index++)
            {
                float anchor = 0.12f + index * 0.19f;
                Image chest = CreateImage("Chest " + (index + 1), milestonePanel.transform, LoadSprite("chest"), Color.white, true);
                SetRect(chest.rectTransform, new Vector2(anchor, 0.50f), new Vector2(anchor, 0.50f), new Vector2(-38f, -38f), new Vector2(38f, 38f));
            }
            TextMeshProUGUI milestoneText = CreateText("Milestone Text", milestonePanel.transform, "0/100", 26f, TextAlignmentOptions.Center, Color.white);
            SetRect(milestoneText.rectTransform, new Vector2(0.38f, 0.04f), new Vector2(0.62f, 0.32f), Vector2.zero, Vector2.zero);

            ScrollRect scroll = CreateQuestScrollView(root.transform, out RectTransform content);
            SetRect(scroll.GetComponent<RectTransform>(), new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.55f), Vector2.zero, Vector2.zero);

            GameObject emptyState = CreateRectObject("Event Empty State", root.transform);
            SetRect(emptyState.GetComponent<RectTransform>(), new Vector2(0.13f, 0.33f), new Vector2(0.87f, 0.51f), Vector2.zero, Vector2.zero);
            Image emptyBackground = CreateImage("Background", emptyState.transform, null, new Color(0.17f, 0.07f, 0.43f, 0.95f), false);
            SetRect(emptyBackground.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI emptyText = CreateText("Text", emptyState.transform, "There are currently\nno events.\nComing soon!", 34f, TextAlignmentOptions.Center, new Color(1f, 0.73f, 0.24f, 1f));
            emptyText.fontStyle = FontStyles.Bold;
            SetRect(emptyText.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            emptyState.SetActive(false);

            SetField(view, "dailyButton", dailyButton);
            SetField(view, "weeklyButton", weeklyButton);
            SetField(view, "eventButton", eventButton);
            SetField(view, "tabStripImage", tabStrip);
            SetField(view, "dailyTabSprite", LoadSprite("active_tabDaily"));
            SetField(view, "weeklyTabSprite", LoadSprite("active_tabWeekly"));
            SetField(view, "eventTabSprite", LoadSprite("active_tabEvent"));
            SetField(view, "titleText", title);
            SetField(view, "timerText", timer);
            SetField(view, "questContent", content);
            SetField(view, "questElementPrefab", elementPrefab);
            SetField(view, "emptyState", emptyState);
            SetField(view, "milestoneText", milestoneText);
            SetField(view, "milestoneFill", milestoneTrack);
            SetField(view, "closeButton", closeButton);

            QuestPanelView prefab = SavePrefab(root, PanelPrefabPath).GetComponent<QuestPanelView>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static ScrollRect CreateQuestScrollView(Transform parent, out RectTransform content)
        {
            GameObject scrollObject = CreateRectObject("Quest Scroll View", parent);
            Image scrollImage = scrollObject.AddComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0.01f);
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;

            GameObject viewportObject = CreateRectObject("Viewport", scrollObject.transform);
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportObject.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject contentObject = CreateRectObject("Content", viewportObject.transform);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 8, 12);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        private static Slider CreateProgressSlider(Transform parent, out TextMeshProUGUI progressText)
        {
            GameObject sliderObject = CreateRectObject("Progress", parent);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;

            Image background = CreateImage("Background", sliderObject.transform, LoadSprite("background_slider"), Color.white, true);
            SetRect(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject fillArea = CreateRectObject("Fill Area", sliderObject.transform);
            SetRect(fillArea.GetComponent<RectTransform>(), new Vector2(0.035f, 0.1f), new Vector2(0.965f, 0.9f), Vector2.zero, Vector2.zero);
            GameObject fill = CreateRectObject("Fill", fillArea.transform);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.sprite = LoadSprite("slider");
            fillImage.type = Image.Type.Sliced;
            fillImage.preserveAspect = false;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            SetRect(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image frame = CreateImage("Frame", sliderObject.transform, LoadSprite("slider_frame"), Color.white, true);
            frame.raycastTarget = false;
            SetRect(frame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            progressText = CreateText("Progress Text", sliderObject.transform, "0/3", 25f, TextAlignmentOptions.Center, Color.white);
            progressText.fontStyle = FontStyles.Bold;
            SetRect(progressText.rectTransform, new Vector2(0.10f, 0f), new Vector2(0.90f, 1f), Vector2.zero, Vector2.zero);

            slider.fillRect = fillRect;
            slider.targetGraphic = background;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Button CreateTransparentTabButton(string name, Transform parent, float minX, float maxX)
        {
            Button button = CreateButton(name, parent, null, new Color(1f, 1f, 1f, 0.01f), out Image target);
            target.color = new Color(1f, 1f, 1f, 0.01f);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(minX, 0f), new Vector2(maxX, 1f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Button CreateButton(string name, Transform parent, Sprite sprite, Color color, out Image image)
        {
            GameObject buttonObject = CreateRectObject(name, parent);
            image = buttonObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect)
        {
            GameObject imageObject = CreateRectObject(name, parent);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = CreateRectObject(name, parent);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = font != null ? font : TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateRectObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetField(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError("[Quest] Missing serialized property '" + propertyName + "' on " + target.GetType().Name + ".");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SavePrefab(GameObject root, string assetPath)
        {
            return PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        }

        private static void AssignPanelPrefabToNavigation(QuestPanelView panelPrefab)
        {
            if (panelPrefab == null)
                return;

            GameObject navigationContents = PrefabUtility.LoadPrefabContents(NavigationPrefabPath);
            try
            {
                LeaderboardBottomNavigationController controller = navigationContents.GetComponent<LeaderboardBottomNavigationController>();
                if (controller == null)
                {
                    Debug.LogError("[Quest] LeaderboardBottomNavigationController was not found in the navigation prefab.");
                    return;
                }

                SetField(controller, "questPanelPrefab", panelPrefab);
                PrefabUtility.SaveAsPrefabAsset(navigationContents, NavigationPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(navigationContents);
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(QuestSpriteFolder + fileName + ".png");
        }

        private static TMP_FontAsset FindUIFont()
        {
            string[] guids = AssetDatabase.FindAssets("Fredoka t:TMP_FontAsset");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

            return TMP_Settings.defaultFontAsset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
