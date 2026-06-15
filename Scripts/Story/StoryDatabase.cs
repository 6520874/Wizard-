using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：从 Resources/Story/story_database.json 加载剧情文本、任务文本和选择题配置，方便不用改 C# 就能调整剧情内容。
    public static class StoryDatabase
    {
        private const string ResourcePath = "Story/story_database";

        private static StoryDatabaseFile database;
        private static bool loaded;

        [Serializable]
        private class StoryDatabaseFile
        {
            public string[] openingNarration;
            public StoryDialogueEntry[] dialogues;
            public StoryQuestEntry[] quests;
            public StoryObjectiveEntry[] objectives;
            public StoryChoiceEntry[] choices;
        }

        [Serializable]
        private class StoryDialogueEntry
        {
            public string id;
            public StoryLineEntry[] lines;
        }

        [Serializable]
        private class StoryLineEntry
        {
            public string speaker;
            public string text;
        }

        [Serializable]
        private class StoryQuestEntry
        {
            public string id;
            public string title;
            public string description;
            public string[] objectiveIds;
        }

        [Serializable]
        private class StoryObjectiveEntry
        {
            public string id;
            public string text;
        }

        [Serializable]
        private class StoryChoiceEntry
        {
            public string id;
            public string summary;
            public string[] labels;
            public int correctIndex;
        }

        public class ChoiceData
        {
            public string Summary;
            public string[] Labels;
            public int CorrectIndex;
        }

        public static string[] GetOpeningNarration(string[] fallback)
        {
            EnsureLoaded();
            return database != null && database.openingNarration != null && database.openingNarration.Length > 0
                ? database.openingNarration
                : fallback;
        }

        public static DialogueLine[] GetDialogue(string id, DialogueLine[] fallback)
        {
            EnsureLoaded();
            StoryDialogueEntry entry = FindById(database?.dialogues, id);
            if (entry == null || entry.lines == null || entry.lines.Length == 0)
            {
                return fallback;
            }

            DialogueLine[] lines = new DialogueLine[entry.lines.Length];
            for (int i = 0; i < entry.lines.Length; i++)
            {
                StoryLineEntry line = entry.lines[i];
                lines[i] = new DialogueLine(line.speaker, line.text);
            }

            return lines;
        }

        public static QuestManager.QuestData GetQuest(string id, string fallbackTitle, string fallbackDescription, params string[] fallbackObjectiveIds)
        {
            EnsureLoaded();
            StoryQuestEntry entry = FindById(database?.quests, id);
            string[] objectiveIds = entry != null && entry.objectiveIds != null && entry.objectiveIds.Length > 0
                ? entry.objectiveIds
                : fallbackObjectiveIds;

            QuestManager.QuestData quest = new QuestManager.QuestData
            {
                title = string.IsNullOrWhiteSpace(entry?.title) ? fallbackTitle : entry.title,
                description = string.IsNullOrWhiteSpace(entry?.description) ? fallbackDescription : entry.description,
                objectives = new List<QuestManager.QuestObjective>()
            };

            if (objectiveIds != null)
            {
                foreach (string objectiveId in objectiveIds)
                {
                    quest.objectives.Add(new QuestManager.QuestObjective(objectiveId, GetObjectiveText(objectiveId, objectiveId)));
                }
            }

            return quest;
        }

        public static string GetObjectiveText(string id, string fallback)
        {
            EnsureLoaded();
            StoryObjectiveEntry entry = FindById(database?.objectives, id);
            return string.IsNullOrWhiteSpace(entry?.text) ? fallback : entry.text;
        }

        public static ChoiceData GetChoice(string id, string fallbackSummary, int fallbackCorrectIndex, params string[] fallbackLabels)
        {
            EnsureLoaded();
            StoryChoiceEntry entry = FindById(database?.choices, id);
            return new ChoiceData
            {
                Summary = string.IsNullOrWhiteSpace(entry?.summary) ? fallbackSummary : entry.summary,
                Labels = entry != null && entry.labels != null && entry.labels.Length >= 3 ? entry.labels : fallbackLabels,
                CorrectIndex = entry == null ? fallbackCorrectIndex : entry.correctIndex
            };
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"Story database not found at Resources/{ResourcePath}. Using code fallbacks.");
                return;
            }

            try
            {
                database = JsonUtility.FromJson<StoryDatabaseFile>(asset.text);
            }
            catch (Exception exception)
            {
                database = null;
                Debug.LogWarning($"Failed to parse story database: {exception.Message}");
            }
        }

        private static T FindById<T>(T[] entries, string id) where T : class
        {
            if (entries == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                T entry = entries[i];
                string entryId = entry switch
                {
                    StoryDialogueEntry dialogue => dialogue.id,
                    StoryQuestEntry quest => quest.id,
                    StoryObjectiveEntry objective => objective.id,
                    StoryChoiceEntry choice => choice.id,
                    _ => null
                };

                if (entryId == id)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
