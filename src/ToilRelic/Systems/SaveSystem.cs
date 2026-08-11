using System.Text.Json;
using ToilRelic.Models;

namespace ToilRelic.Systems;

public sealed class SaveSystem
{
    public const int CurrentSchemaVersion = 1;
    private const string SaveFileName = "savegame.json";
    private readonly string _savePath;
    private readonly string _stagePath;
    private readonly string _lastKnownGoodPath;
    private readonly string _quarantinePath;
    private readonly string _recoveryMarkerPath;
    private readonly SaveEnvelopeFileOperations _fileOperations;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SaveSystem(string? savePath = null)
        : this(
            savePath ?? Path.Combine(Directory.GetCurrentDirectory(), SaveFileName),
            new SaveEnvelopeFileOperations())
    {
    }

    internal SaveSystem(string savePath, SaveEnvelopeFileOperations fileOperations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);
        _savePath = savePath;
        _stagePath = savePath + ".stage";
        _lastKnownGoodPath = savePath + ".lkg";
        _quarantinePath = savePath + ".quarantine";
        _recoveryMarkerPath = savePath + ".recovery-pending";
        _fileOperations = fileOperations ?? throw new ArgumentNullException(nameof(fileOperations));
    }

    public PersistenceResult Save(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        try
        {
            EnsureSaveDirectory();
            var candidateBytes = JsonSerializer.SerializeToUtf8Bytes(player.ToSaveData(), _jsonOptions);
            _fileOperations.WriteDurableStage(_stagePath, candidateBytes, "StageWrite");

            _fileOperations.ValidateStageCheckpoint(_stagePath, SaveEnvelopeMutationSide.Before);
            var stagedCandidate = ValidateCandidate(_stagePath, "Stage");
            if (stagedCandidate.State != CandidateState.Valid)
            {
                return PersistenceResult.Failure(CreateDiagnostic(
                    "StageValidation",
                    "Stage",
                    nameof(InvalidDataException),
                    _stagePath));
            }

            _fileOperations.ValidateStageCheckpoint(_stagePath, SaveEnvelopeMutationSide.After);
            var liveCandidate = ValidateCandidate(_savePath, "Live");
            switch (liveCandidate.State)
            {
                case CandidateState.Valid:
                    _fileOperations.ReplaceLiveWithBackup(_stagePath, _savePath, _lastKnownGoodPath);
                    break;
                case CandidateState.Missing:
                    _fileOperations.PromoteNewLive(_stagePath, _savePath);
                    break;
                default:
                    return PersistenceResult.Failure(liveCandidate.Diagnostic ?? CreateDiagnostic(
                        "ValidateLiveForSave",
                        "Live",
                        nameof(InvalidDataException),
                        _savePath));
            }

            _fileOperations.DeleteRecoveryMarker(_recoveryMarkerPath);
            return PersistenceResult.Success();
        }
        catch (SaveEnvelopeInterruptionException)
        {
            throw;
        }
        catch (SaveEnvelopeOperationException ex)
        {
            return PersistenceResult.Failure(ex.ToDiagnostic());
        }
        catch (Exception ex)
        {
            return PersistenceResult.Failure(CreateDiagnostic(
                "SaveEnvelope",
                "Stage",
                ex.GetType().Name,
                _stagePath));
        }
    }

    public LoadResult Load()
    {
        try
        {
            var liveCandidate = ValidateCandidate(_savePath, "Live");
            if (liveCandidate.State == CandidateState.Valid)
            {
                return LoadResult.Loaded(
                    liveCandidate.Player!,
                    recoveryNoticePending: _fileOperations.FileExists(_recoveryMarkerPath));
            }

            if (liveCandidate.State == CandidateState.Inaccessible)
            {
                return LoadResult.Unreadable(liveCandidate.Diagnostic);
            }

            var lastKnownGoodCandidate = ValidateCandidate(_lastKnownGoodPath, "LastKnownGood");
            if (lastKnownGoodCandidate.State == CandidateState.Valid)
            {
                return Recover(liveCandidate, lastKnownGoodCandidate);
            }

            if (liveCandidate.State == CandidateState.Missing &&
                lastKnownGoodCandidate.State == CandidateState.Missing)
            {
                return LoadResult.Missing();
            }

            return LoadResult.Unreadable(
                liveCandidate.State == CandidateState.Invalid
                    ? liveCandidate.Diagnostic
                    : lastKnownGoodCandidate.Diagnostic);
        }
        catch (SaveEnvelopeInterruptionException)
        {
            throw;
        }
        catch (SaveEnvelopeOperationException ex)
        {
            return LoadResult.Unreadable(ex.ToDiagnostic());
        }
        catch (Exception ex)
        {
            return LoadResult.Unreadable(CreateDiagnostic(
                "LoadEnvelope",
                "Live",
                ex.GetType().Name,
                _savePath));
        }
    }

    public PersistenceResult Delete()
    {
        try
        {
            var liveCandidate = ClassifyAuthorityCandidate(
                _savePath,
                "Live",
                SaveEnvelopeCheckpoint.ClassifyLiveAuthority);
            if (liveCandidate.State == CandidateState.Inaccessible)
            {
                return PersistenceResult.Failure(liveCandidate.Diagnostic!);
            }

            var lastKnownGoodCandidate = ClassifyAuthorityCandidate(
                _lastKnownGoodPath,
                "LastKnownGood",
                SaveEnvelopeCheckpoint.ClassifyLastKnownGoodAuthority);
            if (lastKnownGoodCandidate.State == CandidateState.Inaccessible)
            {
                return PersistenceResult.Failure(lastKnownGoodCandidate.Diagnostic!);
            }

            DeleteEnvelopeArtifact(
                SaveEnvelopeCheckpoint.DeleteStage,
                "DeleteStage",
                "Stage",
                _stagePath);
            DeleteEnvelopeArtifact(
                SaveEnvelopeCheckpoint.DeleteQuarantine,
                "DeleteQuarantine",
                "Quarantine",
                _quarantinePath);

            if (liveCandidate.State == CandidateState.Valid)
            {
                DeleteEnvelopeArtifact(
                    SaveEnvelopeCheckpoint.DeleteLastKnownGood,
                    "DeleteLastKnownGood",
                    "LastKnownGood",
                    _lastKnownGoodPath);
                DeleteEnvelopeArtifact(
                    SaveEnvelopeCheckpoint.DeleteLive,
                    "DeleteLive",
                    "Live",
                    _savePath);
            }
            else
            {
                DeleteEnvelopeArtifact(
                    SaveEnvelopeCheckpoint.DeleteLive,
                    "DeleteLive",
                    "Live",
                    _savePath);
                DeleteEnvelopeArtifact(
                    SaveEnvelopeCheckpoint.DeleteLastKnownGood,
                    "DeleteLastKnownGood",
                    "LastKnownGood",
                    _lastKnownGoodPath);
            }

            _fileOperations.DeleteRecoveryMarker(_recoveryMarkerPath);
            return PersistenceResult.Success();
        }
        catch (SaveEnvelopeOperationException ex)
        {
            return PersistenceResult.Failure(ex.ToDiagnostic());
        }
        catch (Exception ex)
        {
            return PersistenceResult.Failure(CreateDiagnostic(
                "InvalidateEnvelope",
                "Live",
                ex.GetType().Name,
                _savePath));
        }
    }

    private CandidateValidation ClassifyAuthorityCandidate(
        string path,
        string artifactRole,
        SaveEnvelopeCheckpoint checkpoint)
    {
        _fileOperations.ClassifyAuthorityCheckpoint(
            checkpoint,
            SaveEnvelopeMutationSide.Before,
            artifactRole,
            path);
        _fileOperations.ProbeAuthorityClassification(path, artifactRole);
        var candidate = ValidateCandidate(path, artifactRole, "ClassifyAuthority");
        if (candidate.State != CandidateState.Inaccessible)
        {
            _fileOperations.ClassifyAuthorityCheckpoint(
                checkpoint,
                SaveEnvelopeMutationSide.After,
                artifactRole,
                path);
        }

        return candidate;
    }

    private void DeleteEnvelopeArtifact(
        SaveEnvelopeCheckpoint checkpoint,
        string operationRole,
        string artifactRole,
        string path) =>
        _fileOperations.DeleteEnvelopeArtifact(
            checkpoint,
            operationRole,
            artifactRole,
            path);

    private LoadResult Recover(CandidateValidation liveCandidate, CandidateValidation lastKnownGoodCandidate)
    {
        _fileOperations.WriteDurableStage(
            _stagePath,
            lastKnownGoodCandidate.Bytes!,
            "WriteRecoveryStage");

        var recoveryStage = ValidateCandidate(_stagePath, "Stage");
        if (recoveryStage.State != CandidateState.Valid ||
            !recoveryStage.Bytes!.AsSpan().SequenceEqual(lastKnownGoodCandidate.Bytes))
        {
            return LoadResult.Unreadable(CreateDiagnostic(
                "ValidateRecoveryStage",
                "Stage",
                nameof(InvalidDataException),
                _stagePath));
        }

        _fileOperations.CreateDurableRecoveryMarker(_recoveryMarkerPath);
        if (liveCandidate.State == CandidateState.Invalid)
        {
            _fileOperations.DeletePriorQuarantine(_quarantinePath);
            _fileOperations.MoveDamagedLive(_savePath, _quarantinePath);
        }

        _fileOperations.PromoteRecovery(_stagePath, _savePath);
        return LoadResult.Recovered(recoveryStage.Player!);
    }

    private CandidateValidation ValidateCandidate(
        string path,
        string artifactRole,
        string readFailureOperationRole = "ReadCandidate")
    {
        if (!_fileOperations.FileExists(path))
        {
            if (_fileOperations.DirectoryExists(path))
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
            bytes = _fileOperations.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            return CandidateValidation.Inaccessible(CreateDiagnostic(
                readFailureOperationRole,
                artifactRole,
                ex.GetType().Name,
                path));
        }

        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var isCurrent = root.TryGetProperty(nameof(PlayerSaveData.SchemaVersion), out var versionElement);
            if (isCurrent)
            {
                if (versionElement.ValueKind != JsonValueKind.Number ||
                    !versionElement.TryGetInt32(out var version) ||
                    version != CurrentSchemaVersion ||
                    !HasCurrentSaveShape(root))
                {
                    return CandidateValidation.Invalid(CreateDiagnostic(
                        "ValidateCandidate",
                        artifactRole,
                        nameof(InvalidDataException),
                        path));
                }
            }
            else if (root.TryGetProperty(nameof(PlayerSaveData.RelicProject), out _) || !HasCoreSaveShape(root))
            {
                return CandidateValidation.Invalid(CreateDiagnostic(
                    "ValidateCandidate",
                    artifactRole,
                    nameof(InvalidDataException),
                    path));
            }

            var saveData = root.Deserialize<PlayerSaveData>(_jsonOptions);
            if (saveData is null ||
                !HasValidCoreValues(saveData) ||
                isCurrent && !HasValidCurrentState(saveData) ||
                !isCurrent && ContainsRelicState(saveData))
            {
                return CandidateValidation.Invalid(CreateDiagnostic(
                    "ValidateCandidate",
                    artifactRole,
                    nameof(InvalidDataException),
                    path));
            }

            return CandidateValidation.Valid(Player.FromSaveData(saveData), bytes);
        }
        catch (Exception ex) when (ex is not SaveEnvelopeInterruptionException)
        {
            return CandidateValidation.Invalid(CreateDiagnostic(
                "ValidateCandidate",
                artifactRole,
                ex.GetType().Name,
                path));
        }
    }

    private void EnsureSaveDirectory()
    {
        var directory = Path.GetDirectoryName(_savePath);
        if (string.IsNullOrWhiteSpace(directory)) return;

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception ex)
        {
            throw new SaveEnvelopeOperationException(
                "PrepareSaveDirectory",
                "Stage",
                ex.GetType().Name,
                Path.GetFileName(_stagePath));
        }
    }

    private static string CreateDiagnostic(
        string operationRole,
        string artifactRole,
        string exceptionType,
        string path) =>
        $"operation={operationRole}; artifact={artifactRole}; exception={exceptionType}; file={Path.GetFileName(path)}";

    private static bool HasCurrentSaveShape(JsonElement root) =>
        HasCoreSaveShape(root) &&
        HasStringArray(root, nameof(PlayerSaveData.OwnedEquipmentIds)) &&
        HasEquippedEquipmentArray(root, nameof(PlayerSaveData.EquippedEquipment)) &&
        HasProperty(root, nameof(PlayerSaveData.EquipmentInitialized), JsonValueKind.True, JsonValueKind.False) &&
        root.TryGetProperty(nameof(PlayerSaveData.RelicProject), out var project) &&
        project.ValueKind == JsonValueKind.Object &&
        HasStringArray(project, nameof(RelicProjectSaveData.CompletedContributionIds)) &&
        HasProperty(project, nameof(RelicProjectSaveData.Forged), JsonValueKind.True, JsonValueKind.False);

    private static bool HasCoreSaveShape(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object &&
        HasProperty(root, nameof(PlayerSaveData.Name), JsonValueKind.String) &&
        HasProperty(root, nameof(PlayerSaveData.MaxHp), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Hp), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Level), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Experience), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.TreasureCount), JsonValueKind.Number) &&
        HasProperty(root, nameof(PlayerSaveData.Inventory), JsonValueKind.Object);

    private static bool HasProperty(JsonElement root, string name, params JsonValueKind[] expectedKinds) =>
        root.TryGetProperty(name, out var property) && expectedKinds.Contains(property.ValueKind);

    private static bool HasStringArray(JsonElement root, string name) =>
        root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Array &&
        property.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String);

    private static bool HasEquippedEquipmentArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array) return false;
        return property.EnumerateArray().All(item =>
            item.ValueKind == JsonValueKind.Object &&
            HasProperty(item, nameof(EquippedEquipmentEntry.Slot), JsonValueKind.Number) &&
            HasProperty(item, nameof(EquippedEquipmentEntry.EquipmentId), JsonValueKind.String));
    }

    private static bool HasValidCoreValues(PlayerSaveData saveData) =>
        saveData.MaxHp > 0 && saveData.Hp >= 0 && saveData.Hp <= saveData.MaxHp &&
        saveData.Level > 0 && saveData.Experience >= 0 &&
        saveData.Experience < Player.RequiredExperience(saveData.Level) &&
        saveData.TreasureCount >= 0 && saveData.Inventory is not null &&
        saveData.Inventory.All(pair => Enum.IsDefined(pair.Key) && pair.Value >= 0);

    private static bool HasValidCurrentState(PlayerSaveData saveData)
    {
        if (saveData.SchemaVersion != CurrentSchemaVersion || !saveData.EquipmentInitialized ||
            saveData.OwnedEquipmentIds is null || saveData.EquippedEquipment is null || saveData.RelicProject is null)
        {
            return false;
        }

        var owned = new HashSet<string>(StringComparer.Ordinal);
        if (saveData.OwnedEquipmentIds.Any(id =>
                string.IsNullOrWhiteSpace(id) || !owned.Add(id) || !EquipmentCatalog.TryGet(id, out _)))
        {
            return false;
        }

        var equippedIds = new HashSet<string>(StringComparer.Ordinal);
        var equippedSlots = new HashSet<EquipmentSlot>();
        foreach (var entry in saveData.EquippedEquipment)
        {
            if (entry is null || !Enum.IsDefined(entry.Slot) || string.IsNullOrWhiteSpace(entry.EquipmentId) ||
                !owned.Contains(entry.EquipmentId) || !EquipmentCatalog.TryGet(entry.EquipmentId, out var equipment) ||
                !equipment.CanEquipTo(entry.Slot) || !equippedIds.Add(entry.EquipmentId) || !equippedSlots.Add(entry.Slot))
            {
                return false;
            }
        }

        if (!equippedSlots.Contains(EquipmentSlot.PrimaryWeapon)) return false;
        return RelicProjectState.HasValidSaveState(
            saveData.RelicProject,
            saveData.OwnedEquipmentIds,
            saveData.EquippedEquipment);
    }

    private static bool ContainsRelicState(PlayerSaveData saveData) =>
        saveData.OwnedEquipmentIds?.Contains(EquipmentCatalog.ToilboundRelicId, StringComparer.Ordinal) == true ||
        saveData.EquippedEquipment?.Any(entry =>
            string.Equals(entry.EquipmentId, EquipmentCatalog.ToilboundRelicId, StringComparison.Ordinal)) == true;

    private enum CandidateState
    {
        Missing,
        Valid,
        Invalid,
        Inaccessible
    }

    private sealed record CandidateValidation(
        CandidateState State,
        Player? Player,
        byte[]? Bytes,
        string? Diagnostic)
    {
        public static CandidateValidation Missing() => new(CandidateState.Missing, null, null, null);
        public static CandidateValidation Valid(Player player, byte[] bytes) =>
            new(CandidateState.Valid, player, bytes, null);
        public static CandidateValidation Invalid(string diagnostic) =>
            new(CandidateState.Invalid, null, null, diagnostic);
        public static CandidateValidation Inaccessible(string diagnostic) =>
            new(CandidateState.Inaccessible, null, null, diagnostic);
    }
}
