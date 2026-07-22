using System;
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
        public const int CurrentSaveVersion = 2;
        private static string savePathOverride;
        private static string SavePath => savePathOverride ?? Path.Combine(Application.persistentDataPath, "toil_relic_save.json");

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
                var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || envelope.player == null)
                {
                    return SaveLoadResult.Unreadable("The save did not contain player data.");
                }

                if (envelope.version != CurrentSaveVersion)
                {
                    return SaveLoadResult.Unreadable($"Unsupported save version: {envelope.version}.");
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
    }
}
