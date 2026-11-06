#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NebulaSoft.EditorTools
{
    /// <summary>
    /// Creates the first editable Quest Database and wires the Quest init module once.
    /// Existing designer-authored definitions are deliberately left untouched.
    /// </summary>
    internal static class QuestDataSetupEditor
    {
        private const string DatabaseFolder = "Assets/Project Files/Data/Quest";
        private const string DatabasePath = DatabaseFolder + "/Quest Database.asset";
        private const string ProjectInitSettingsPath = "Assets/Project Files/Data/Project Init Settings.asset";

        public static void CreateDefaultQuestData()
        {
            QuestDatabase database = EnsureDatabase();
            EnsureDefaultMilestones(database);
            EnsureInitModule(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = database;
            Debug.Log("[Quest] Quest Database and initialization module are ready.");
        }

        private static QuestDatabase EnsureDatabase()
        {
            QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);
            if (database != null)
                return database;

            EnsureFolder(DatabaseFolder);
            database = ScriptableObject.CreateInstance<QuestDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            PopulateDefaultDefinitions(database);
            return database;
        }

        private static void EnsureInitModule(QuestDatabase database)
        {
            ProjectInitSettings settings = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>(ProjectInitSettingsPath);
            if (settings == null)
            {
                Debug.LogError("[Quest] Project Init Settings asset was not found.");
                return;
            }

            QuestInitModule module = settings.GetModule<QuestInitModule>();
            if (module == null)
            {
                module = ScriptableObject.CreateInstance<QuestInitModule>();
                module.name = "NebulaSoft.QuestInitModule";
                AssetDatabase.AddObjectToAsset(module, settings);

                SerializedObject settingsObject = new SerializedObject(settings);
                SerializedProperty modules = settingsObject.FindProperty("modules");
                modules.InsertArrayElementAtIndex(modules.arraySize);
                modules.GetArrayElementAtIndex(modules.arraySize - 1).objectReferenceValue = module;
                settingsObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SerializedObject moduleObject = new SerializedObject(module);
            moduleObject.FindProperty("questDatabase").objectReferenceValue = database;
            moduleObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(module);
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureDefaultMilestones(QuestDatabase database)
        {
            if (database == null || database.Milestones.Count > 0)
                return;

            PopulateDefaultMilestones(database);
        }

        private static void PopulateDefaultDefinitions(QuestDatabase database)
        {
            SerializedObject databaseObject = new SerializedObject(database);
            SerializedProperty definitions = databaseObject.FindProperty("definitions");
            definitions.arraySize = DefaultQuestDefinitions.Length;

            for (int index = 0; index < DefaultQuestDefinitions.Length; index++)
            {
                DefaultQuestDefinition source = DefaultQuestDefinitions[index];
                SerializedProperty target = definitions.GetArrayElementAtIndex(index);
                target.FindPropertyRelative("id").stringValue = source.Id;
                target.FindPropertyRelative("category").enumValueIndex = (int)source.Category;
                target.FindPropertyRelative("goalType").enumValueIndex = (int)source.GoalType;
                target.FindPropertyRelative("title").stringValue = source.Title;
                target.FindPropertyRelative("targetValue").intValue = source.TargetValue;
                target.FindPropertyRelative("sortOrder").intValue = source.SortOrder;
                target.FindPropertyRelative("rotationSlot").intValue = source.RotationSlot;
                target.FindPropertyRelative("enabled").boolValue = true;
                target.FindPropertyRelative("milestonePoints").intValue = source.MilestonePoints;
                target.FindPropertyRelative("goTarget").enumValueIndex = (int)source.GoTarget;

                SetReward(target.FindPropertyRelative("rewardData"), source.Reward);
                SetLegacyCurrencyReward(target.FindPropertyRelative("reward"), CurrencyType.Coins, 0);
            }

            databaseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void PopulateDefaultMilestones(QuestDatabase database)
        {
            SerializedObject databaseObject = new SerializedObject(database);
            SerializedProperty milestones = databaseObject.FindProperty("milestones");
            milestones.arraySize = DefaultQuestMilestones.Length;

            for (int index = 0; index < DefaultQuestMilestones.Length; index++)
            {
                DefaultQuestMilestone source = DefaultQuestMilestones[index];
                SerializedProperty target = milestones.GetArrayElementAtIndex(index);
                target.FindPropertyRelative("id").stringValue = source.Id;
                target.FindPropertyRelative("category").enumValueIndex = (int)source.Category;
                target.FindPropertyRelative("requiredPoints").intValue = source.RequiredPoints;
                target.FindPropertyRelative("sortOrder").intValue = source.SortOrder;

                SetReward(target.FindPropertyRelative("rewardData"), source.Reward);
                SetLegacyCurrencyReward(target.FindPropertyRelative("reward"), CurrencyType.Coins, 0);
            }

            databaseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void SetReward(SerializedProperty target, DefaultQuestReward reward)
        {
            if (target == null)
                return;

            target.FindPropertyRelative("type").enumValueIndex = (int)reward.Type;
            target.FindPropertyRelative("currencyType").enumValueIndex = (int)reward.CurrencyType;
            target.FindPropertyRelative("powerUpType").enumValueIndex = (int)reward.PowerUpType;
            target.FindPropertyRelative("amount").intValue = reward.Amount;
        }

        private static void SetLegacyCurrencyReward(SerializedProperty target, CurrencyType currencyType, int amount)
        {
            if (target == null)
                return;

            target.FindPropertyRelative("currencyType").enumValueIndex = (int)currencyType;
            target.FindPropertyRelative("amount").intValue = amount;
        }

        private static void EnsureFolder(string targetFolder)
        {
            if (AssetDatabase.IsValidFolder(targetFolder))
                return;

            string parent = System.IO.Path.GetDirectoryName(targetFolder)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(targetFolder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private readonly struct DefaultQuestDefinition
        {
            public readonly string Id;
            public readonly QuestCategory Category;
            public readonly QuestGoalType GoalType;
            public readonly string Title;
            public readonly int TargetValue;
            public readonly DefaultQuestReward Reward;
            public readonly int MilestonePoints;
            public readonly int SortOrder;
            public readonly int RotationSlot;
            public readonly QuestGoTarget GoTarget;

            public DefaultQuestDefinition(string id, QuestCategory category, QuestGoalType goalType, string title,
                int targetValue, DefaultQuestReward reward, int milestonePoints, int sortOrder, int rotationSlot,
                QuestGoTarget goTarget)
            {
                Id = id;
                Category = category;
                GoalType = goalType;
                Title = title;
                TargetValue = targetValue;
                Reward = reward;
                MilestonePoints = milestonePoints;
                SortOrder = sortOrder;
                RotationSlot = rotationSlot;
                GoTarget = goTarget;
            }
        }

        private readonly struct DefaultQuestMilestone
        {
            public readonly string Id;
            public readonly QuestCategory Category;
            public readonly int RequiredPoints;
            public readonly DefaultQuestReward Reward;
            public readonly int SortOrder;

            public DefaultQuestMilestone(string id, QuestCategory category, int requiredPoints, DefaultQuestReward reward,
                int sortOrder)
            {
                Id = id;
                Category = category;
                RequiredPoints = requiredPoints;
                Reward = reward;
                SortOrder = sortOrder;
            }
        }

        private readonly struct DefaultQuestReward
        {
            public readonly QuestRewardType Type;
            public readonly CurrencyType CurrencyType;
            public readonly PUType PowerUpType;
            public readonly int Amount;

            public DefaultQuestReward(CurrencyType currencyType, int amount)
            {
                Type = QuestRewardType.Currency;
                CurrencyType = currencyType;
                PowerUpType = PUType.FreezeTimer;
                Amount = amount;
            }

            public DefaultQuestReward(PUType powerUpType, int amount)
            {
                Type = QuestRewardType.PowerUp;
                CurrencyType = global::NebulaSoft.CurrencyType.Coins;
                PowerUpType = powerUpType;
                Amount = amount;
            }
        }

        private static readonly DefaultQuestDefinition[] DefaultQuestDefinitions =
        {
            new DefaultQuestDefinition("daily_complete_levels", QuestCategory.Daily, QuestGoalType.CompleteLevels,
                "Complete 3 level(s)", 3, new DefaultQuestReward(CurrencyType.Coins, 75), 30, 0, 0, QuestGoTarget.Home),
            new DefaultQuestDefinition("daily_complete_levels_alt", QuestCategory.Daily, QuestGoalType.CompleteLevels,
                "Complete 4 level(s)", 4, new DefaultQuestReward(CurrencyType.Coins, 100), 30, 0, 0, QuestGoTarget.Home),
            new DefaultQuestDefinition("daily_spend_coins", QuestCategory.Daily, QuestGoalType.SpendCoins,
                "Spend 250 coins", 250, new DefaultQuestReward(PUType.FreeMovement, 4), 35, 1, 1, QuestGoTarget.Store),
            new DefaultQuestDefinition("daily_spend_coins_alt", QuestCategory.Daily, QuestGoalType.SpendCoins,
                "Spend 350 coins", 350, new DefaultQuestReward(PUType.FreeMovement, 5), 35, 1, 1, QuestGoTarget.Store),
            new DefaultQuestDefinition("daily_use_powerups", QuestCategory.Daily, QuestGoalType.UsePowerUp,
                "Use 2 power-up(s)", 2, new DefaultQuestReward(PUType.FreezeTimer, 1), 35, 2, 2, QuestGoTarget.PowerUp),
            new DefaultQuestDefinition("daily_buy_powerup", QuestCategory.Daily, QuestGoalType.PurchasePowerUp,
                "Buy 1 power-up", 1, new DefaultQuestReward(PUType.Merge, 1), 35, 2, 2, QuestGoTarget.Store),
            new DefaultQuestDefinition("weekly_complete_levels", QuestCategory.Weekly, QuestGoalType.CompleteLevels,
                "Complete 15 level(s)", 15, new DefaultQuestReward(CurrencyType.Coins, 250), 30, 0, 0, QuestGoTarget.Home),
            new DefaultQuestDefinition("weekly_complete_levels_alt", QuestCategory.Weekly, QuestGoalType.CompleteLevels,
                "Complete 20 level(s)", 20, new DefaultQuestReward(CurrencyType.Coins, 325), 30, 0, 0, QuestGoTarget.Home),
            new DefaultQuestDefinition("weekly_spend_coins", QuestCategory.Weekly, QuestGoalType.SpendCoins,
                "Spend 1200 coins", 1200, new DefaultQuestReward(PUType.FreezeTimer, 1), 35, 1, 1, QuestGoTarget.Store),
            new DefaultQuestDefinition("weekly_spend_coins_alt", QuestCategory.Weekly, QuestGoalType.SpendCoins,
                "Spend 1500 coins", 1500, new DefaultQuestReward(PUType.Merge, 1), 35, 1, 1, QuestGoTarget.Store),
            new DefaultQuestDefinition("weekly_buy_powerups", QuestCategory.Weekly, QuestGoalType.PurchasePowerUp,
                "Buy 2 power-up(s)", 2, new DefaultQuestReward(PUType.FreeMovement, 10), 35, 2, 2, QuestGoTarget.Store),
            new DefaultQuestDefinition("weekly_use_powerups", QuestCategory.Weekly, QuestGoalType.UsePowerUp,
                "Use 6 power-up(s)", 6, new DefaultQuestReward(PUType.FreeMovement, 10), 35, 2, 2, QuestGoTarget.PowerUp)
        };

        private static readonly DefaultQuestMilestone[] DefaultQuestMilestones =
        {
            new DefaultQuestMilestone("daily_milestone_20", QuestCategory.Daily, 20, new DefaultQuestReward(CurrencyType.Coins, 10), 0),
            new DefaultQuestMilestone("daily_milestone_40", QuestCategory.Daily, 40, new DefaultQuestReward(CurrencyType.Coins, 20), 1),
            new DefaultQuestMilestone("daily_milestone_60", QuestCategory.Daily, 60, new DefaultQuestReward(CurrencyType.Coins, 30), 2),
            new DefaultQuestMilestone("daily_milestone_80", QuestCategory.Daily, 80, new DefaultQuestReward(CurrencyType.Coins, 40), 3),
            new DefaultQuestMilestone("daily_milestone_100", QuestCategory.Daily, 100, new DefaultQuestReward(CurrencyType.Coins, 50), 4),
            new DefaultQuestMilestone("weekly_milestone_20", QuestCategory.Weekly, 20, new DefaultQuestReward(CurrencyType.Coins, 20), 0),
            new DefaultQuestMilestone("weekly_milestone_40", QuestCategory.Weekly, 40, new DefaultQuestReward(CurrencyType.Coins, 40), 1),
            new DefaultQuestMilestone("weekly_milestone_60", QuestCategory.Weekly, 60, new DefaultQuestReward(CurrencyType.Coins, 60), 2),
            new DefaultQuestMilestone("weekly_milestone_80", QuestCategory.Weekly, 80, new DefaultQuestReward(CurrencyType.Coins, 80), 3),
            new DefaultQuestMilestone("weekly_milestone_100", QuestCategory.Weekly, 100, new DefaultQuestReward(PUType.Hammer, 1), 4)
        };
    }
}
#endif
