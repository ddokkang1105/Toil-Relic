using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private static SaveEnvelopeFileOperations fileOperations = new SaveEnvelopeFileOperations();
        private static string SavePath => savePathOverride ?? Path.Combine(Application.persistentDataPath, "toil_relic_save.json");
        private static string StagePath => SavePath + ".stage";
        private static string LastKnownGoodPath => SavePath + ".lkg";
        private static string QuarantinePath => SavePath + ".quarantine";
        private static string RecoveryMarkerPath => SavePath + ".recovery-pending";

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

        public static SaveOperationResult Delete()
        {
            try
            {
                var liveCandidate = ClassifyAuthorityCandidate(
                    SavePath,
                    "Live",
                    "ClassifyLiveAuthority");
                if (liveCandidate.State == CandidateState.Inaccessible)
                {
                    return SaveOperationResult.Failure(liveCandidate.Diagnostic);
                }

                var lastKnownGoodCandidate = ClassifyAuthorityCandidate(
                    LastKnownGoodPath,
                    "LastKnownGood",
                    "ClassifyLastKnownGoodAuthority");
                if (lastKnownGoodCandidate.State == CandidateState.Inaccessible)
                {
                    return SaveOperationResult.Failure(lastKnownGoodCandidate.Diagnostic);
                }

                DeleteEnvelopeArtifact("DeleteStage", "DeleteStage", "Stage", StagePath);
                DeleteEnvelopeArtifact(
                    "DeleteQuarantine",
                    "DeleteQuarantine",
                    "Quarantine",
                    QuarantinePath);

                if (liveCandidate.State == CandidateState.Valid)
                {
                    DeleteEnvelopeArtifact(
                        "DeleteLastKnownGood",
                        "DeleteLastKnownGood",
                        "LastKnownGood",
                        LastKnownGoodPath);
                    DeleteEnvelopeArtifact("DeleteLive", "DeleteLive", "Live", SavePath);
                }
                else
                {
                    DeleteEnvelopeArtifact("DeleteLive", "DeleteLive", "Live", SavePath);
                    DeleteEnvelopeArtifact(
                        "DeleteLastKnownGood",
                        "DeleteLastKnownGood",
                        "LastKnownGood",
                        LastKnownGoodPath);
                }

                fileOperations.DeleteRecoveryMarker(RecoveryMarkerPath);
                return SaveOperationResult.Success();
            }
            catch (SaveEnvelopeOperationException exception)
            {
                return SaveOperationResult.Failure(exception.ToDiagnostic());
            }
            catch (Exception exception)
            {
                return SaveOperationResult.Failure(CreateDiagnostic(
                    "InvalidateEnvelope",
                    "Live",
                    exception.GetType().Name,
                    SavePath));
            }
        }

        private static CandidateValidation ClassifyAuthorityCandidate(
            string path,
            string artifactRole,
            string checkpointName)
        {
            fileOperations.ClassifyAuthorityCheckpoint(
                checkpointName,
                "Before",
                artifactRole,
                path);
            fileOperations.ProbeAuthorityClassification(path, artifactRole);
            var candidate = ValidateCandidate(path, artifactRole, "ClassifyAuthority");
            if (candidate.State != CandidateState.Inaccessible)
            {
                fileOperations.ClassifyAuthorityCheckpoint(
                    checkpointName,
                    "After",
                    artifactRole,
                    path);
            }

            return candidate;
        }

        private static void DeleteEnvelopeArtifact(
            string checkpointName,
            string operationRole,
            string artifactRole,
            string path) =>
            fileOperations.DeleteEnvelopeArtifact(
                checkpointName,
                operationRole,
                artifactRole,
                path);

        public static SaveOperationResult Save(PlayerState player)
        {
            try
            {
                EnsureSaveDirectory();
                var envelope = new SaveEnvelope { version = CurrentSaveVersion, player = player };
                var candidateBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope, prettyPrint: false));
                fileOperations.WriteDurableStage(StagePath, candidateBytes, "StageWrite");

                fileOperations.ValidateStageCheckpoint(StagePath, "Before");
                var stagedCandidate = ValidateCandidate(StagePath, "Stage");
                if (stagedCandidate.State != CandidateState.Valid)
                {
                    return SaveOperationResult.Failure(CreateDiagnostic(
                        "StageValidation",
                        "Stage",
                        nameof(InvalidDataException),
                        StagePath));
                }

                fileOperations.ValidateStageCheckpoint(StagePath, "After");
                var liveCandidate = ValidateCandidate(SavePath, "Live");
                switch (liveCandidate.State)
                {
                    case CandidateState.Valid:
                        fileOperations.ReplaceLiveWithBackup(StagePath, SavePath, LastKnownGoodPath);
                        break;
                    case CandidateState.Missing:
                        fileOperations.PromoteNewLive(StagePath, SavePath);
                        break;
                    default:
                        return SaveOperationResult.Failure(liveCandidate.Diagnostic ?? CreateDiagnostic(
                            "ValidateLiveForSave",
                            "Live",
                            nameof(InvalidDataException),
                            SavePath));
                }

                fileOperations.DeleteRecoveryMarker(RecoveryMarkerPath);
                return SaveOperationResult.Success();
            }
            catch (SaveEnvelopeInterruptionException)
            {
                throw;
            }
            catch (SaveEnvelopeOperationException exception)
            {
                return SaveOperationResult.Failure(exception.ToDiagnostic());
            }
            catch (Exception exception)
            {
                return SaveOperationResult.Failure(CreateDiagnostic(
                    "SaveEnvelope",
                    "Stage",
                    exception.GetType().Name,
                    StagePath));
            }
        }

        public static SaveLoadResult Load()
        {
            try
            {
                var liveCandidate = ValidateCandidate(SavePath, "Live");
                if (liveCandidate.State == CandidateState.Valid)
                {
                    return SaveLoadResult.Loaded(
                        liveCandidate.Player,
                        recoveryNoticePending: fileOperations.FileExists(RecoveryMarkerPath));
                }

                if (liveCandidate.State == CandidateState.Inaccessible)
                {
                    return SaveLoadResult.Unreadable(liveCandidate.Diagnostic);
                }

                var lastKnownGoodCandidate = ValidateCandidate(LastKnownGoodPath, "LastKnownGood");
                if (lastKnownGoodCandidate.State == CandidateState.Valid)
                {
                    return Recover(liveCandidate, lastKnownGoodCandidate);
                }

                if (liveCandidate.State == CandidateState.Missing &&
                    lastKnownGoodCandidate.State == CandidateState.Missing)
                {
                    return SaveLoadResult.Missing();
                }

                return SaveLoadResult.Unreadable(
                    liveCandidate.State == CandidateState.Invalid
                        ? liveCandidate.Diagnostic
                        : lastKnownGoodCandidate.Diagnostic);
            }
            catch (SaveEnvelopeInterruptionException)
            {
                throw;
            }
            catch (SaveEnvelopeOperationException exception)
            {
                return SaveLoadResult.Unreadable(exception.ToDiagnostic());
            }
            catch (Exception exception)
            {
                return SaveLoadResult.Unreadable(CreateDiagnostic(
                    "LoadEnvelope",
                    "Live",
                    exception.GetType().Name,
                    SavePath));
            }
        }

        private static SaveLoadResult Recover(
            CandidateValidation liveCandidate,
            CandidateValidation lastKnownGoodCandidate)
        {
            fileOperations.WriteDurableStage(
                StagePath,
                lastKnownGoodCandidate.Bytes,
                "WriteRecoveryStage");

            var recoveryStage = ValidateCandidate(StagePath, "Stage");
            if (recoveryStage.State != CandidateState.Valid ||
                !recoveryStage.Bytes.SequenceEqual(lastKnownGoodCandidate.Bytes))
            {
                return SaveLoadResult.Unreadable(CreateDiagnostic(
                    "ValidateRecoveryStage",
                    "Stage",
                    nameof(InvalidDataException),
                    StagePath));
            }

            fileOperations.CreateDurableRecoveryMarker(RecoveryMarkerPath);
            if (liveCandidate.State == CandidateState.Invalid)
            {
                fileOperations.DeletePriorQuarantine(QuarantinePath);
                fileOperations.MoveDamagedLive(SavePath, QuarantinePath);
            }

            fileOperations.PromoteRecovery(StagePath, SavePath);
            return SaveLoadResult.Recovered(recoveryStage.Player);
        }

        private static CandidateValidation ValidateCandidate(
            string path,
            string artifactRole,
            string readFailureOperationRole = "ReadCandidate")
        {
            if (!fileOperations.FileExists(path))
            {
                if (fileOperations.DirectoryExists(path))
                {
                    return CandidateValidation.Invalid(CreateDiagnostic(
                        "ValidateCandidate",
                        artifactRole,
                        nameof(IOException),
                        path));
                }

                return CandidateValidation.Missing();
            }

            byte[] bytes;
            try
            {
                bytes = fileOperations.ReadAllBytes(path);
            }
            catch (Exception exception)
            {
                return CandidateValidation.Inaccessible(CreateDiagnostic(
                    readFailureOperationRole,
                    artifactRole,
                    exception.GetType().Name,
                    path));
            }

            try
            {
                var json = Encoding.UTF8.GetString(bytes);
                var presenceProbe = JsonUtility.FromJson<SaveEnvelopePresenceProbe>(json);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || envelope.player == null || presenceProbe?.player == null ||
                    !IsSupportedVersion(envelope.version))
                {
                    return CandidateValidation.Invalid(CreateDiagnostic(
                        "ValidateCandidate",
                        artifactRole,
                        nameof(InvalidDataException),
                        path));
                }

                var isCurrent = presenceProbe.version == CurrentSaveVersion &&
                    envelope.version == CurrentSaveVersion;
                if (isCurrent)
                {
                    if (!HasCurrentRequiredFields(json, presenceProbe.player) ||
                        !envelope.player.HasValidCurrentSaveData())
                    {
                        return CandidateValidation.Invalid(CreateDiagnostic(
                            "ValidateCandidate",
                            artifactRole,
                            nameof(InvalidDataException),
                            path));
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
                        return CandidateValidation.Invalid(CreateDiagnostic(
                            "ValidateCandidate",
                            artifactRole,
                            nameof(InvalidDataException),
                            path));
                    }

                    envelope.player.EnsureLegacyProject();
                }

                return CandidateValidation.Valid(envelope.player, bytes);
            }
            catch (Exception exception) when (!(exception is SaveEnvelopeInterruptionException))
            {
                return CandidateValidation.Invalid(CreateDiagnostic(
                    "ValidateCandidate",
                    artifactRole,
                    exception.GetType().Name,
                    path));
            }
        }

        private static void EnsureSaveDirectory()
        {
            var directory = Path.GetDirectoryName(SavePath);
            if (string.IsNullOrWhiteSpace(directory)) return;

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception exception)
            {
                throw new SaveEnvelopeOperationException(
                    "PrepareSaveDirectory",
                    "Stage",
                    exception.GetType().Name,
                    Path.GetFileName(StagePath));
            }
        }

        private static string CreateDiagnostic(
            string operationRole,
            string artifactRole,
            string exceptionType,
            string path) =>
            $"operation={operationRole}; artifact={artifactRole}; exception={exceptionType}; file={Path.GetFileName(path)}";

        private enum CandidateState
        {
            Missing,
            Valid,
            Invalid,
            Inaccessible
        }

        private sealed class CandidateValidation
        {
            private CandidateValidation(
                CandidateState state,
                PlayerState player,
                byte[] bytes,
                string diagnostic)
            {
                State = state;
                Player = player;
                Bytes = bytes;
                Diagnostic = diagnostic;
            }

            public CandidateState State { get; }
            public PlayerState Player { get; }
            public byte[] Bytes { get; }
            public string Diagnostic { get; }

            public static CandidateValidation Missing() =>
                new CandidateValidation(CandidateState.Missing, null, null, null);

            public static CandidateValidation Valid(PlayerState player, byte[] bytes) =>
                new CandidateValidation(CandidateState.Valid, player, bytes, null);

            public static CandidateValidation Invalid(string diagnostic) =>
                new CandidateValidation(CandidateState.Invalid, null, null, diagnostic);

            public static CandidateValidation Inaccessible(string diagnostic) =>
                new CandidateValidation(CandidateState.Inaccessible, null, null, diagnostic);
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
                    if (index < 0)
                    {
                        return -1;
                    }

                    continue;
                }

                if (json[index] == open)
                {
                    depth++;
                }
                else if (json[index] == close && --depth == 0)
                {
                    return index;
                }
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

                if (json[index] == '"')
                {
                    return index;
                }
            }

            return -1;
        }

        private static int SkipWhitespace(string json, int start)
        {
            while (start < json.Length && char.IsWhiteSpace(json[start]))
            {
                start++;
            }

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
