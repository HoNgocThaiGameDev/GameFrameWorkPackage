using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "Quest Database", menuName = "Data/Quest/Quest Database")]
    public sealed class QuestDatabase : ScriptableObject
    {
        [SerializeField] List<QuestDefinition> definitions = new List<QuestDefinition>();
        [SerializeField] List<QuestMilestoneDefinition> milestones = new List<QuestMilestoneDefinition>();

        public IReadOnlyList<QuestDefinition> Definitions => definitions;
        public IReadOnlyList<QuestMilestoneDefinition> Milestones => milestones;

        public QuestDefinition GetDefinition(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;

            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition != null && string.Equals(definition.Id, questId, StringComparison.Ordinal))
                    return definition;
            }

            return null;
        }

        public void GetDefinitions(QuestCategory category, List<QuestDefinition> results)
        {
            if (results == null)
                return;

            results.Clear();
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition != null && definition.Category == category && definition.IsAvailable)
                    results.Add(definition);
            }

            results.Sort(CompareDefinitions);
        }

        public QuestMilestoneDefinition GetMilestone(string milestoneId)
        {
            if (string.IsNullOrWhiteSpace(milestoneId))
                return null;

            for (int i = 0; i < milestones.Count; i++)
            {
                QuestMilestoneDefinition milestone = milestones[i];
                if (milestone != null && string.Equals(milestone.Id, milestoneId, StringComparison.Ordinal))
                    return milestone;
            }

            return null;
        }

        public void GetMilestones(QuestCategory category, List<QuestMilestoneDefinition> results)
        {
            if (results == null)
                return;

            results.Clear();
            for (int i = 0; i < milestones.Count; i++)
            {
                QuestMilestoneDefinition milestone = milestones[i];
                if (milestone != null && milestone.Category == category && milestone.IsAvailable)
                    results.Add(milestone);
            }

            results.Sort(CompareMilestones);
        }

        private static int CompareDefinitions(QuestDefinition first, QuestDefinition second)
        {
            int sortOrder = first.SortOrder.CompareTo(second.SortOrder);
            return sortOrder != 0 ? sortOrder : string.CompareOrdinal(first.Id, second.Id);
        }

        private static int CompareMilestones(QuestMilestoneDefinition first, QuestMilestoneDefinition second)
        {
            int sortOrder = first.SortOrder.CompareTo(second.SortOrder);
            if (sortOrder != 0)
                return sortOrder;

            int requiredPoints = first.RequiredPoints.CompareTo(second.RequiredPoints);
            return requiredPoints != 0 ? requiredPoints : string.CompareOrdinal(first.Id, second.Id);
        }
    }
}
