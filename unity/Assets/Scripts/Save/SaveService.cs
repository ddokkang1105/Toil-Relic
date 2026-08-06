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

        private static bool HasCurrentRequiredFields(string json, PlayerStatePresenceProbe player) =>
            player != null && player.HasModernRequiredFields() &&
            player.ownedEquipmentIds != null && player.equippedEquipment != null &&
            player.relicProject?.completedContributionIds != null &&
            HasBooleanProperty(json, "equipmentInitialized") &&
            HasBooleanProperty(json, "forged");

        private static bool HasBooleanProperty(string json, string propertyName) =>
            Regex.IsMatch(json, $"\\\"{Regex.Escape(propertyName)}\\\"\\s*:\\s*(true|false)(?=\\s*[,}}])");

        private static bool HasJsonProperty(string json, string propertyName) =>
            Regex.IsMatch(json, $"\\\"{Regex.Escape(propertyName)}\\\"\\s*:");
    }
}
