using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ToilRelic.Tests")]

namespace ToilRelic.Systems;

internal enum SaveEnvelopeCheckpoint
{
    StageWrite,
    StageValidation,
    PreserveLastKnownGood,
    PromoteLive,
    AfterLivePromotion,
    PromoteRecovery,
    AfterRecoveryPromotion,
    CreateRecoveryMarker,
    FlushRecoveryMarker,
    DeletePriorQuarantine,
    MoveDamagedLive,
    DeleteRecoveryMarker,
    ClassifyLiveAuthority,
    ClassifyLastKnownGoodAuthority,
    DeleteStage,
    DeleteQuarantine,
    DeleteLastKnownGood,
    DeleteLive
}

internal enum SaveEnvelopeMutationSide
{
    Before,
    After
}

internal sealed class SaveEnvelopeInterruptionException : IOException
{
    public SaveEnvelopeInterruptionException(string checkpoint)
        : base(checkpoint)
    {
    }
}

internal sealed class SaveEnvelopeOperationException : IOException
{
    public SaveEnvelopeOperationException(
        string operationRole,
        string artifactRole,
        string exceptionType,
        string relativeFilename)
    {
        OperationRole = operationRole;
        ArtifactRole = artifactRole;
        ExceptionType = exceptionType;
        RelativeFilename = relativeFilename;
    }

    public string OperationRole { get; }
    public string ArtifactRole { get; }
    public string ExceptionType { get; }
    public string RelativeFilename { get; }

    public string ToDiagnostic() =>
        $"operation={OperationRole}; artifact={ArtifactRole}; exception={ExceptionType}; file={RelativeFilename}";
}

internal sealed class SaveEnvelopeFileOperations
{
    private readonly Action<SaveEnvelopeCheckpoint, SaveEnvelopeMutationSide>? _checkpoint;

    public SaveEnvelopeFileOperations(
        Action<SaveEnvelopeCheckpoint, SaveEnvelopeMutationSide>? checkpoint = null)
    {
        _checkpoint = checkpoint;
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public void WriteDurableStage(string path, byte[] bytes, string operationRole)
    {
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.StageWrite,
            SaveEnvelopeMutationSide.Before,
            operationRole,
            "Stage",
            path);

        try
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }
        catch (SaveEnvelopeInterruptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Wrap(operationRole, "Stage", path, ex);
        }

        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.StageWrite,
            SaveEnvelopeMutationSide.After,
            operationRole,
            "Stage",
            path);
    }

    public void ValidateStageCheckpoint(string path, SaveEnvelopeMutationSide side) =>
        InvokeCheckpoint(SaveEnvelopeCheckpoint.StageValidation, side, "StageValidation", "Stage", path);

    public void ReplaceLiveWithBackup(string stagePath, string livePath, string lastKnownGoodPath)
    {
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.PreserveLastKnownGood,
            SaveEnvelopeMutationSide.Before,
            "PreserveLastKnownGood",
            "LastKnownGood",
            lastKnownGoodPath);
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.PromoteLive,
            SaveEnvelopeMutationSide.Before,
            "PromoteLive",
            "Live",
            livePath);

        try
        {
            File.Replace(stagePath, livePath, lastKnownGoodPath);
        }
        catch (Exception ex)
        {
            throw Wrap("PromoteLive", "Live", livePath, ex);
        }

        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.PromoteLive,
            SaveEnvelopeMutationSide.After,
            "PromoteLive",
            "Live",
            livePath);
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.AfterLivePromotion,
            SaveEnvelopeMutationSide.After,
            "AfterLivePromotion",
            "Live",
            livePath);
    }

    public void PromoteNewLive(string stagePath, string livePath)
    {
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.PromoteLive,
            SaveEnvelopeMutationSide.Before,
            "PromoteLive",
            "Live",
            livePath);
        try
        {
            File.Move(stagePath, livePath, overwrite: false);
        }
        catch (Exception ex)
        {
            throw Wrap("PromoteLive", "Live", livePath, ex);
        }

        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.PromoteLive,
            SaveEnvelopeMutationSide.After,
            "PromoteLive",
            "Live",
            livePath);
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.AfterLivePromotion,
            SaveEnvelopeMutationSide.After,
            "AfterLivePromotion",
            "Live",
            livePath);
    }

    public void CreateDurableRecoveryMarker(string markerPath)
    {
        if (File.Exists(markerPath)) return;
        if (Directory.Exists(markerPath))
        {
            throw new SaveEnvelopeOperationException(
                "CreateRecoveryMarker",
                "RecoveryMarker",
                nameof(IOException),
                Path.GetFileName(markerPath));
        }

        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.CreateRecoveryMarker,
            SaveEnvelopeMutationSide.Before,
            "CreateRecoveryMarker",
            "RecoveryMarker",
            markerPath);
        try
        {
            using var stream = new FileStream(
                markerPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            InvokeCheckpoint(
                SaveEnvelopeCheckpoint.CreateRecoveryMarker,
                SaveEnvelopeMutationSide.After,
                "CreateRecoveryMarker",
                "RecoveryMarker",
                markerPath);
            InvokeCheckpoint(
                SaveEnvelopeCheckpoint.FlushRecoveryMarker,
                SaveEnvelopeMutationSide.Before,
                "FlushRecoveryMarker",
                "RecoveryMarker",
                markerPath);
            stream.Flush(flushToDisk: true);
            InvokeCheckpoint(
                SaveEnvelopeCheckpoint.FlushRecoveryMarker,
                SaveEnvelopeMutationSide.After,
                "FlushRecoveryMarker",
                "RecoveryMarker",
                markerPath);
        }
        catch (SaveEnvelopeInterruptionException)
        {
            throw;
        }
        catch (SaveEnvelopeOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Wrap("CreateRecoveryMarker", "RecoveryMarker", markerPath, ex);
        }
    }

    public void DeletePriorQuarantine(string quarantinePath)
    {
        if (!File.Exists(quarantinePath) && !Directory.Exists(quarantinePath)) return;
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.DeletePriorQuarantine,
            SaveEnvelopeMutationSide.Before,
            "DeletePriorQuarantine",
            "Quarantine",
            quarantinePath);
        try
        {
            File.Delete(quarantinePath);
        }
        catch (Exception ex)
        {
            throw Wrap("DeletePriorQuarantine", "Quarantine", quarantinePath, ex);
        }

        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.DeletePriorQuarantine,
            SaveEnvelopeMutationSide.After,
            "DeletePriorQuarantine",
            "Quarantine",
            quarantinePath);
    }

    public void MoveDamagedLive(string livePath, string quarantinePath)
    {
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.MoveDamagedLive,
            SaveEnvelopeMutationSide.Before,
            "MoveDamagedLive",
            "Live",
            livePath);
        try
        {
            File.Move(livePath, quarantinePath, overwrite: false);
        }
        catch (Exception ex)
        {
            throw Wrap("MoveDamagedLive", "Live", livePath, ex);
        }

        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.MoveDamagedLive,
            SaveEnvelopeMutationSide.After,
            "MoveDamagedLive",
            "Live",
            livePath);
    }

    public void PromoteRecovery(string stagePath, string livePath)
    {
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.PromoteRecovery,
            SaveEnvelopeMutationSide.Before,
            "PromoteRecovery",
            "Live",
            livePath);
        try
        {
            File.Move(stagePath, livePath, overwrite: false);
        }
        catch (Exception ex)
        {
            throw Wrap("PromoteRecovery", "Live", livePath, ex);
        }

        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.PromoteRecovery,
            SaveEnvelopeMutationSide.After,
            "PromoteRecovery",
            "Live",
            livePath);
        InvokeCheckpoint(
            SaveEnvelopeCheckpoint.AfterRecoveryPromotion,
            SaveEnvelopeMutationSide.After,
            "AfterRecoveryPromotion",
            "Live",
            livePath);
    }

    public void DeleteRecoveryMarker(string markerPath)
    {
        DeleteEnvelopeArtifact(
            SaveEnvelopeCheckpoint.DeleteRecoveryMarker,
            "DeleteRecoveryMarker",
            "RecoveryMarker",
            markerPath);
    }

    public void ClassifyAuthorityCheckpoint(
        SaveEnvelopeCheckpoint checkpoint,
        SaveEnvelopeMutationSide side,
        string artifactRole,
        string path)
    {
        if (checkpoint is not SaveEnvelopeCheckpoint.ClassifyLiveAuthority and
            not SaveEnvelopeCheckpoint.ClassifyLastKnownGoodAuthority)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint));
        }

        InvokeCheckpoint(checkpoint, side, "ClassifyAuthority", artifactRole, path);
    }

    public void ProbeAuthorityClassification(string path, string artifactRole)
    {
        try
        {
            _ = File.GetAttributes(path);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception ex)
        {
            throw Wrap("ClassifyAuthority", artifactRole, path, ex);
        }
    }

    public void DeleteEnvelopeArtifact(
        SaveEnvelopeCheckpoint checkpoint,
        string operationRole,
        string artifactRole,
        string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return;
        InvokeCheckpoint(
            checkpoint,
            SaveEnvelopeMutationSide.Before,
            operationRole,
            artifactRole,
            path);
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            throw Wrap(operationRole, artifactRole, path, ex);
        }

        InvokeCheckpoint(
            checkpoint,
            SaveEnvelopeMutationSide.After,
            operationRole,
            artifactRole,
            path);
    }

    private void InvokeCheckpoint(
        SaveEnvelopeCheckpoint checkpoint,
        SaveEnvelopeMutationSide side,
        string operationRole,
        string artifactRole,
        string path)
    {
        try
        {
            _checkpoint?.Invoke(checkpoint, side);
        }
        catch (SaveEnvelopeInterruptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Wrap(operationRole, artifactRole, path, ex);
        }
    }

    private static SaveEnvelopeOperationException Wrap(
        string operationRole,
        string artifactRole,
        string path,
        Exception exception) =>
        exception as SaveEnvelopeOperationException ?? new SaveEnvelopeOperationException(
            operationRole,
            artifactRole,
            exception.GetType().Name,
            Path.GetFileName(path));
}
