using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NebulaSoft;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class QuestRotationTests
{
    private const string DatabasePath = "Assets/Project Files/Data/Quest/Quest Database.asset";
    private const string PanelPrefabPath = "Assets/Project Files/Game/Features/Quest/Prefabs/Panel_quest.prefab";
    private const string QuestElementPrefabPath = "Assets/Project Files/Game/Features/Quest/Prefabs/QuestElement.prefab";
    private const string QuestRewardPopupPrefabPath = "Assets/Project Files/Game/Features/Quest/Prefabs/QuestRewardClaimPopup.prefab";
    private const string ClaimButtonSpritePath = "Assets/Project Files/Game/Features/Quest/Art/Sprites/Claim_btn.png";
    private const string ClaimedButtonSpritePath = "Assets/Project Files/Game/Features/Quest/Art/Sprites/Claimed_btn.png";
    private const string GoButtonSpritePath = "Assets/NebulaSoft Core/Shared/Images/Primitives/WhiteSprite.png";
    private const string PatternQuestTexturePath = "Assets/Project Files/Game/Features/Quest/Art/Textures/PatternQuest.png";

    [Test]
    public void PeriodSelections_RoundTripThroughJsonUtility()
    {
        QuestProgressSave source = new QuestProgressSave
        {
            PeriodSelections = new List<QuestPeriodSelectionEntry>
            {
                new QuestPeriodSelectionEntry
                {
                    Category = QuestCategory.Daily,
                    PeriodKey = "daily:20260714",
                    QuestIds = new List<string>
                    {
                        "daily_complete_levels",
                        "daily_spend_coins_alt",
                        "daily_use_powerups"
                    }
                }
            }
        };

        QuestProgressSave restored = JsonUtility.FromJson<QuestProgressSave>(JsonUtility.ToJson(source));

        Assert.That(restored.PeriodSelections, Has.Count.EqualTo(1));
        Assert.That(restored.PeriodSelections[0].PeriodKey, Is.EqualTo("daily:20260714"));
        Assert.That(restored.PeriodSelections[0].QuestIds, Is.EqualTo(source.PeriodSelections[0].QuestIds));
    }

    [Test]
    public void OnlineSeconds_RoundTripThroughJsonUtility()
    {
        QuestProgressSave source = new QuestProgressSave
        {
            Entries = new List<QuestProgressEntry>
            {
                new QuestProgressEntry
                {
                    QuestId = "daily_online_20_minutes",
                    PeriodKey = "daily:20260714",
                    Progress = 7,
                    OnlineSeconds = 454.5f
                }
            }
        };

        QuestProgressSave restored = JsonUtility.FromJson<QuestProgressSave>(JsonUtility.ToJson(source));

        Assert.That(restored.Entries[0].OnlineSeconds, Is.EqualTo(454.5f));
    }

    [TestCase(QuestCategory.Daily, 6, 185)]
    [TestCase(QuestCategory.Weekly, 5, 155)]
    public void BuiltInRotation_HasConfiguredSlotsAndCanReachEveryMilestone(QuestCategory category, int slotCount, int minimumPoints)
    {
        QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);
        Assert.That(database, Is.Not.Null);

        List<IGrouping<int, QuestDefinition>> slots = database.Definitions
            .Where(definition => definition != null && definition.IsAvailable
                && definition.Category == category && definition.RotationSlot >= 0)
            .GroupBy(definition => definition.RotationSlot)
            .OrderBy(group => group.Key)
            .ToList();

        Assert.That(slots.Select(slot => slot.Key), Is.EqualTo(Enumerable.Range(0, slotCount)));
        int worstCasePoints = slots.Sum(slot => slot.Min(definition => definition.MilestonePoints));
        Assert.That(worstCasePoints, Is.EqualTo(minimumPoints));
        var milestones = new List<QuestMilestoneDefinition>();
        database.GetMilestones(category, milestones);
        Assert.That(milestones, Is.Not.Empty);
        Assert.That(worstCasePoints, Is.GreaterThanOrEqualTo(milestones.Max(milestone => milestone.RequiredPoints)));
    }

    [Test]
    public void QuestDatabase_IncludesCoinRewards()
    {
        QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);
        Assert.That(database, Is.Not.Null);

        int coinRewardCount = database.Definitions.Count(definition => definition != null
            && definition.Reward.Type == QuestRewardType.Currency
            && definition.Reward.CurrencyType == CurrencyType.Coins
            && definition.Reward.Amount > 0);

        Assert.That(coinRewardCount, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void QuestDatabase_OnlineTimeQuestsGrantConfiguredCoins()
    {
        QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);
        QuestDefinition quest = database.GetDefinition("daily_online_20_minutes");

        Assert.That(quest, Is.Not.Null);
        Assert.That(quest.Category, Is.EqualTo(QuestCategory.Daily));
        Assert.That(quest.GoalType, Is.EqualTo(QuestGoalType.OnlineMinutes));
        Assert.That(quest.TargetValue, Is.EqualTo(20));
        Assert.That(quest.RotationSlot, Is.EqualTo(3));
        Assert.That(quest.GoTarget, Is.EqualTo(QuestGoTarget.Home));
        Assert.That(quest.Reward.Type, Is.EqualTo(QuestRewardType.Currency));
        Assert.That(quest.Reward.CurrencyType, Is.EqualTo(CurrencyType.Coins));
        Assert.That(quest.Reward.Amount, Is.EqualTo(70));

        QuestDefinition weeklyQuest = database.GetDefinition("weekly_online_20_minutes");
        Assert.That(weeklyQuest, Is.Not.Null);
        Assert.That(weeklyQuest.RotationSlot, Is.EqualTo(3));
        Assert.That(weeklyQuest.GoTarget, Is.EqualTo(QuestGoTarget.Home));
        Assert.That(weeklyQuest.Reward.Type, Is.EqualTo(QuestRewardType.Currency));
        Assert.That(weeklyQuest.Reward.CurrencyType, Is.EqualTo(CurrencyType.Coins));
        Assert.That(weeklyQuest.Reward.Amount, Is.EqualTo(180));
    }

    [Test]
    public void PanelQuest_BindsMilestonePanelReference()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        Assert.That(prefab, Is.Not.Null);

        QuestPanelView view = prefab.GetComponent<QuestPanelView>();
        Assert.That(view, Is.Not.Null);

        SerializedObject serializedView = new SerializedObject(view);
        GameObject milestonePanel = serializedView.FindProperty("milestonePanel").objectReferenceValue as GameObject;
        Assert.That(milestonePanel, Is.Not.Null);
        Assert.That(milestonePanel.transform, Is.EqualTo(prefab.transform.Find("Safe Area/Milestone Panel")));
    }

    [Test]
    public void PanelQuest_UsesScrollingQuestPattern()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        Transform pattern = prefab.transform.Find("Quest Pattern");
        Assert.That(pattern, Is.Not.Null);

        UnityEngine.UI.RawImage rawImage = pattern.GetComponent<UnityEngine.UI.RawImage>();
        Assert.That(rawImage, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(rawImage.texture), Is.EqualTo(PatternQuestTexturePath));
        Assert.That(rawImage.uvRect.size, Is.EqualTo(Vector2.one * 4f));
        Assert.That(rawImage.color.a, Is.EqualTo(0.16862746f).Within(0.0001f));

        QuestPatternScroller scroller = pattern.GetComponent<QuestPatternScroller>();
        Assert.That(scroller, Is.Not.Null);
    }

    [Test]
    public void QuestElement_BindsQuestRewardPopupPrefab()
    {
        QuestElementView view = AssetDatabase.LoadAssetAtPath<QuestElementView>(QuestElementPrefabPath);
        Assert.That(view, Is.Not.Null);

        SerializedObject serializedView = new SerializedObject(view);
        Object popupPrefab = serializedView.FindProperty("rewardClaimPopupPrefab").objectReferenceValue;
        Assert.That(popupPrefab, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(popupPrefab), Is.EqualTo(QuestRewardPopupPrefabPath));

        Object claimedButtonSprite = serializedView.FindProperty("claimedButtonSprite").objectReferenceValue;
        Assert.That(claimedButtonSprite, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(claimedButtonSprite), Is.EqualTo(ClaimedButtonSpritePath));

        Object claimButtonSprite = serializedView.FindProperty("claimButtonSprite").objectReferenceValue;
        Assert.That(claimButtonSprite, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(claimButtonSprite), Is.EqualTo(ClaimButtonSpritePath));

        UnityEngine.UI.Button claimButton = serializedView.FindProperty("claimButton").objectReferenceValue
            as UnityEngine.UI.Button;
        TMPro.TextMeshProUGUI claimLabel = serializedView.FindProperty("claimLabel").objectReferenceValue
            as TMPro.TextMeshProUGUI;
        Assert.That(claimButton.colors.disabledColor.a, Is.EqualTo(1f));
        Assert.That(claimLabel, Is.Not.Null);

        UnityEngine.UI.AspectRatioFitter aspectRatioFitter = view.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        Assert.That(aspectRatioFitter, Is.Not.Null);
        Assert.That(aspectRatioFitter.aspectMode,
            Is.EqualTo(UnityEngine.UI.AspectRatioFitter.AspectMode.WidthControlsHeight));
        Assert.That(aspectRatioFitter.aspectRatio, Is.EqualTo(960f / 180f).Within(0.001f));

        UnityEngine.UI.Button goButton = serializedView.FindProperty("goButton").objectReferenceValue
            as UnityEngine.UI.Button;
        Assert.That(AssetDatabase.GetAssetPath((goButton.targetGraphic as UnityEngine.UI.Image).sprite),
            Is.EqualTo(GoButtonSpritePath));
    }

    [Test]
    public void PanelQuest_HidesMilestonesForEventsAndRestoresThemForDaily()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        GameObject instance = Object.Instantiate(prefab);

        try
        {
            QuestPanelView view = instance.GetComponent<QuestPanelView>();
            SerializedObject serializedView = new SerializedObject(view);
            GameObject milestonePanel = serializedView.FindProperty("milestonePanel").objectReferenceValue as GameObject;
            MethodInfo refresh = typeof(QuestPanelView).GetMethod("RefreshMilestoneProgress",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo activeCategory = typeof(QuestPanelView).GetField("activeCategory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(refresh, Is.Not.Null);
            Assert.That(activeCategory, Is.Not.Null);

            activeCategory.SetValue(view, QuestCategory.Event);
            refresh.Invoke(view, null);
            Assert.That(milestonePanel.activeSelf, Is.False);

            activeCategory.SetValue(view, QuestCategory.Daily);
            refresh.Invoke(view, null);
            Assert.That(milestonePanel.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
