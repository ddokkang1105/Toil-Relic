using System;
using System.Collections.Generic;
using System.IO;
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
        public const int CurrentSaveVersion = 2;
        private static string savePathOverride;
        private static string SavePath => savePathOverride ?? Path.Combine(Application.persistentDataPath, "toil_relic_save.json");

        [Serializable]
        private sealed class SaveEnvelopePresenceProbe
        {
            public PlayerStatePresenceProbe player = new();
        }

        [Serializable]
        private sealed class PlayerStatePresenceProbe
        {
            private const int MissingValue = int.MinValue;

            public int maxHp = MissingValue;
            public int hp = MissingValue;
            public int level = MissingValue;
            public int experience = MissingValue;
            public int score = MissingValue;
            public int treasureCount = MissingValue;
            public List<InventorySlot> inventory;

            internal bool HasModernRequiredFields() =>
                maxHp != MissingValue &&
                hp != MissingValue &&
                level != MissingValue &&
                experience != MissingValue &&
                treasureCount != MissingValue &&
                inventory != null;

            internal bool HasHistoricalRequiredFields() =>
                maxHp != MissingValue &&
                hp != MissingValue &&
                score != MissingValue &&
                treasureCount != MissingValue &&
                inventory != null;
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
                var json = JsonUtility.ToJson(envelope, prettyPrint: false);
                File.WriteAllText(SavePath, json);
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
                var presenceProbe = new SaveEnvelopePresenceProbe();
                JsonUtility.FromJsonOverwrite(json, presenceProbe);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || envelope.player == null)
                {
                    return SaveLoadResult.Unreadable("The save did not contain player data.");
                }

                if (!IsSupportedVersion(envelope.version))
                {
                    return SaveLoadResult.Unreadable($"Unsupported save version: {envelope.version}.");
                }

                if (!HasRequiredPlayerFields(envelope.version, presenceProbe.player))
                {
                    return SaveLoadResult.Unreadable("The save did not contain all required player fields.");
                }

                if (!envelope.player.HasValidSaveData())
                {
                    return SaveLoadResult.Unreadable("The save did not contain valid player data.");
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
            version == VersionlessSaveVersion ||
            version == LegacySaveVersion ||
            version == CurrentSaveVersion;

        private static bool HasRequiredPlayerFields(int version, PlayerStatePresenceProbe player) =>
            player != null &&
            (player.HasModernRequiredFields() ||
             version == VersionlessSaveVersion && player.HasHistoricalRequiredFields());
    }
}
