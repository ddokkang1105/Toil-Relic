using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ToilRelic.Unity.Core;
using UnityEngine;

namespace ToilRelic.Unity.Save
{
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int version;
        public PlayerState player;
    }

    public static class SaveService
    {
        private const int VersionlessSaveVersion = 0;
        private const int LegacySaveVersion = 1;
        private const int PreviousSaveVersion = 2;
        public const int CurrentSaveVersion = 3;
        private static string savePathOverride;
        private static string SavePath => savePathOverride ?? Path.Combine(Application.persistentDataPath, "toil_relic_save.json");

        [Serializable]
        private sealed class SaveEnvelopePresenceProbe
        {
            public const int MissingValue = int.MinValue;
            public int version = MissingValue;
            public PlayerStatePresenceProbe player;
        }

        [Serializable]
        private sealed class PlayerStatePresenceProbe
        {
            public int maxHp = SaveEnvelopePresenceProbe.MissingValue;
            public int hp = SaveEnvelopePresenceProbe.MissingValue;
            public int level = SaveEnvelopePresenceProbe.MissingValue;
            public int experience = SaveEnvelopePresenceProbe.MissingValue;
            public int score = SaveEnvelopePresenceProbe.MissingValue;
            public int treasureCount = SaveEnvelopePresenceProbe.MissingValue;
            public List<InventorySlot> inventory;
            public List<string> ownedEquipmentIds;
            public List<EquippedEquipmentEntry> equippedEquipment;
            public RelicProjectPresenceProbe relicProject;

            internal bool HasModernRequiredFields() =>
                maxHp != SaveEnvelopePresenceProbe.MissingValue &&
                hp != SaveEnvelopePresenceProbe.MissingValue &&
                level != SaveEnvelopePresenceProbe.MissingValue &&
                experience != SaveEnvelopePresenceProbe.MissingValue &&
                treasureCount != SaveEnvelopePresenceProbe.MissingValue &&
                inventory != null;

            internal bool HasHistoricalRequiredFields() =>
                maxHp != SaveEnvelopePresenceProbe.MissingValue &&
                hp != SaveEnvelopePresenceProbe.MissingValue &&
                score != SaveEnvelopePresenceProbe.MissingValue &&
                treasureCount != SaveEnvelopePresenceProbe.MissingValue &&
                inventory != null;
        }

        [Serializable]
        private sealed class RelicProjectPresenceProbe
        {
            public List<string> completedContributionIds;
        }

        public static SaveOperationResult Delete()
        {
            try
            {
                File.Delete(SavePath);
                return SaveOperationResult.Success();
            }
            catch (Exception exception)
            {
                return SaveOperationResult.Failure(exception.ToString());
            }
        }

        public static SaveOperationResult Save(PlayerState player)
        {
            try
            {
                var envelope = new SaveEnvelope { version = CurrentSaveVersion, player = player };
                File.WriteAllText(SavePath, JsonUtility.ToJson(envelope, prettyPrint: false));
                return SaveOperationResult.Success();
            }
            catch (Exception exception)
            {
                return SaveOperationResult.Failure(exception.ToString());
            }
        }

        public static SaveLoadResult Load()
        {
            try
            {
                var json = File.ReadAllText(SavePath);
                var presenceProbe = JsonUtility.FromJson<SaveEnvelopePresenceProbe>(json);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || envelope.player == null || presenceProbe?.player == null)
                {
                    return SaveLoadResult.Unreadable("The save did not contain player data.");
                }

                if (!IsSupportedVersion(envelope.version))
                {
                    return SaveLoadResult.Unreadable($"Unsupported save version: {envelope.version}.");
                }

                var isCurrent = presenceProbe.version == CurrentSaveVersion && envelope.version == CurrentSaveVersion;
                if (isCurrent)
                {
                    if (!HasCurrentRequiredFields(json, presenceProbe.player) || !envelope.player.HasValidCurrentSaveData())
                    {
                        return SaveLoadResult.Unreadable("The current save did not contain valid project and player data.");
                    }
                }
                else
                {
                    if (envelope.version == CurrentSaveVersion ||
                        !HasLegacyRequiredFields(envelope.version, presenceProbe.player) ||
                        HasJsonProperty(json, "relicProject") ||
                        envelope.player.HasLegacyRelicState() ||
                        !envelope.player.HasValidSaveData())
                    {
                        return SaveLoadResult.Unreadable("The legacy save did not contain valid player data.");
                    }

                    envelope.player.EnsureLegacyProject();
                }

                return SaveLoadResult.Loaded(envelope.player);
            }
            catch (FileNotFoundException)
            {
                return SaveLoadResult.Missing();
            }
            catch (DirectoryNotFoundException)
            {
                return SaveLoadResult.Missing();
            }
            catch (Exception exception)
            {
                return SaveLoadResult.Unreadable(exception.ToString());
            }
        }

        private static bool IsSupportedVersion(int version) =>
            version == VersionlessSaveVersion || version == LegacySaveVersion ||
            version == PreviousSaveVersion || version == CurrentSaveVersion;

        private static bool HasLegacyRequiredFields(int version, PlayerStatePresenceProbe player) =>
            player != null &&
            (player.HasModernRequiredFields() ||
             version == VersionlessSaveVersion && player.HasHistoricalRequiredFields());

        private static bool HasCurrentRequiredFields(string json, PlayerStatePresenceProbe player)
        {
            if (player == null || !player.HasModernRequiredFields() ||
                player.ownedEquipmentIds == null || player.equippedEquipment == null ||
                !TryGetTopLevelObjectProperty(json, "player", out var playerJson) ||
                !TryGetTopLevelObjectProperty(playerJson, "relicProject", out var projectJson))
            {
                return false;
            }

            return HasTopLevelBooleanProperty(playerJson, "equipmentInitialized") &&
                HasTopLevelArrayProperty(projectJson, "completedContributionIds") &&
                HasTopLevelBooleanProperty(projectJson, "forged");
        }

        private static bool TryGetTopLevelObjectProperty(string json, string propertyName, out string objectJson)
        {
            objectJson = null;
            if (!TryFindTopLevelPropertyValue(json, propertyName, out var valueStart) ||
                valueStart >= json.Length || json[valueStart] != '{')
            {
                return false;
            }

            var valueEnd = FindMatchingContainer(json, valueStart, '{', '}');
            if (valueEnd < 0)
            {
                return false;
            }

            objectJson = json.Substring(valueStart, valueEnd - valueStart + 1);
            return true;
        }

        private static bool HasTopLevelArrayProperty(string json, string propertyName)
        {
            if (!TryFindTopLevelPropertyValue(json, propertyName, out var valueStart) ||
                valueStart >= json.Length || json[valueStart] != '[')
            {
                return false;
            }

            return FindMatchingContainer(json, valueStart, '[', ']') >= 0;
        }

        private static bool HasTopLevelBooleanProperty(string json, string propertyName)
        {
            if (!TryFindTopLevelPropertyValue(json, propertyName, out var valueStart))
            {
                return false;
            }

            return HasDelimitedLiteral(json, valueStart, "true") ||
                HasDelimitedLiteral(json, valueStart, "false");
        }

        private static bool TryFindTopLevelPropertyValue(string json, string propertyName, out int valueStart)
        {
            valueStart = -1;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var objectDepth = 0;
            for (var index = 0; index < json.Length; index++)
            {
                if (json[index] == '{')
                {
                    objectDepth++;
                    continue;
                }

                if (json[index] == '}')
                {
                    objectDepth--;
                    continue;
                }

                if (json[index] != '"')
                {
                    continue;
                }

                var stringEnd = FindStringEnd(json, index);
                if (stringEnd < 0)
                {
                    return false;
                }

                if (objectDepth == 1 &&
                    string.Equals(json.Substring(index + 1, stringEnd - index - 1), propertyName, StringComparison.Ordinal))
                {
                    var cursor = SkipWhitespace(json, stringEnd + 1);
                    if (cursor < json.Length && json[cursor] == ':')
                    {
                        valueStart = SkipWhitespace(json, cursor + 1);
                        return valueStart < json.Length;
                    }
                }

                index = stringEnd;
            }

            return false;
        }

        private static int FindMatchingContainer(string json, int start, char open, char close)
        {
            var depth = 0;
            for (var index = start; index < json.Length; index++)
            {
                if (json[index] == '"')
                {
                    index = FindStringEnd(json, index);
                    if (index < 0) return -1;
                    continue;
                }

                if (json[index] == open) depth++;
                else if (json[index] == close && --depth == 0) return index;
            }

            return -1;
        }

        private static int FindStringEnd(string json, int start)
        {
            var escaped = false;
            for (var index = start + 1; index < json.Length; index++)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (json[index] == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (json[index] == '"') return index;
            }

            return -1;
        }

        private static int SkipWhitespace(string json, int start)
        {
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
            return start;
        }

        private static bool HasDelimitedLiteral(string json, int start, string literal)
        {
            if (start + literal.Length > json.Length ||
                !string.Equals(json.Substring(start, literal.Length), literal, StringComparison.Ordinal))
            {
                return false;
            }

            var end = SkipWhitespace(json, start + literal.Length);
            return end < json.Length && (json[end] == ',' || json[end] == '}');
        }

        private static bool HasJsonProperty(string json, string propertyName) =>
            Regex.IsMatch(json, $"\\\"{Regex.Escape(propertyName)}\\\"\\s*:");
    }
}
