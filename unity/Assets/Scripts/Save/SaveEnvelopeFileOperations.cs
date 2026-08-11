using System;
using System.IO;

namespace ToilRelic.Unity.Save
{
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
        private const string Before = "Before";
        private const string After = "After";
        private readonly Action<string, string> checkpoint;

        public SaveEnvelopeFileOperations(Action<string, string> checkpoint = null)
        {
            this.checkpoint = checkpoint;
        }

        public bool FileExists(string path) => File.Exists(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public void WriteDurableStage(string path, byte[] bytes, string operationRole)
        {
            InvokeCheckpoint("StageWrite", Before, operationRole, "Stage", path);
            try
            {
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
            catch (SaveEnvelopeInterruptionException)
            {
                throw;
            }
            catch (SaveEnvelopeOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Wrap(operationRole, "Stage", path, exception);
            }

            InvokeCheckpoint("StageWrite", After, operationRole, "Stage", path);
        }

        public void ValidateStageCheckpoint(string path, string side) =>
            InvokeCheckpoint("StageValidation", side, "StageValidation", "Stage", path);

        public void ReplaceLiveWithBackup(string stagePath, string livePath, string lastKnownGoodPath)
        {
            InvokeCheckpoint(
                "PreserveLastKnownGood",
                Before,
                "PreserveLastKnownGood",
                "LastKnownGood",
                lastKnownGoodPath);
            InvokeCheckpoint("PromoteLive", Before, "PromoteLive", "Live", livePath);
            try
            {
                File.Replace(stagePath, livePath, lastKnownGoodPath);
            }
            catch (Exception exception)
            {
                throw Wrap("PromoteLive", "Live", livePath, exception);
            }

            InvokeCheckpoint("PromoteLive", After, "PromoteLive", "Live", livePath);
            InvokeCheckpoint("AfterLivePromotion", After, "AfterLivePromotion", "Live", livePath);
        }

        public void PromoteNewLive(string stagePath, string livePath)
        {
            InvokeCheckpoint("PromoteLive", Before, "PromoteLive", "Live", livePath);
            try
            {
                File.Move(stagePath, livePath);
            }
            catch (Exception exception)
            {
                throw Wrap("PromoteLive", "Live", livePath, exception);
            }

            InvokeCheckpoint("PromoteLive", After, "PromoteLive", "Live", livePath);
            InvokeCheckpoint("AfterLivePromotion", After, "AfterLivePromotion", "Live", livePath);
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
                "CreateRecoveryMarker",
                Before,
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
                    "CreateRecoveryMarker",
                    After,
                    "CreateRecoveryMarker",
                    "RecoveryMarker",
                    markerPath);
                InvokeCheckpoint(
                    "FlushRecoveryMarker",
                    Before,
                    "FlushRecoveryMarker",
                    "RecoveryMarker",
                    markerPath);
                try
                {
                    stream.Flush(flushToDisk: true);
                }
                catch (Exception exception)
                {
                    throw Wrap("FlushRecoveryMarker", "RecoveryMarker", markerPath, exception);
                }
                InvokeCheckpoint(
                    "FlushRecoveryMarker",
                    After,
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
            catch (Exception exception)
            {
                throw Wrap("CreateRecoveryMarker", "RecoveryMarker", markerPath, exception);
            }
        }

        public void DeletePriorQuarantine(string quarantinePath)
        {
            if (!File.Exists(quarantinePath) && !Directory.Exists(quarantinePath)) return;
            InvokeCheckpoint(
                "DeletePriorQuarantine",
                Before,
                "DeletePriorQuarantine",
                "Quarantine",
                quarantinePath);
            try
            {
                File.Delete(quarantinePath);
            }
            catch (Exception exception)
            {
                throw Wrap("DeletePriorQuarantine", "Quarantine", quarantinePath, exception);
            }

            InvokeCheckpoint(
                "DeletePriorQuarantine",
                After,
                "DeletePriorQuarantine",
                "Quarantine",
                quarantinePath);
        }

        public void MoveDamagedLive(string livePath, string quarantinePath)
        {
            InvokeCheckpoint("MoveDamagedLive", Before, "MoveDamagedLive", "Live", livePath);
            try
            {
                File.Move(livePath, quarantinePath);
            }
            catch (Exception exception)
            {
                throw Wrap("MoveDamagedLive", "Live", livePath, exception);
            }

            InvokeCheckpoint("MoveDamagedLive", After, "MoveDamagedLive", "Live", livePath);
        }

        public void PromoteRecovery(string stagePath, string livePath)
        {
            InvokeCheckpoint("PromoteRecovery", Before, "PromoteRecovery", "Live", livePath);
            try
            {
                File.Move(stagePath, livePath);
            }
            catch (Exception exception)
            {
                throw Wrap("PromoteRecovery", "Live", livePath, exception);
            }

            InvokeCheckpoint("PromoteRecovery", After, "PromoteRecovery", "Live", livePath);
            InvokeCheckpoint(
                "AfterRecoveryPromotion",
                After,
                "AfterRecoveryPromotion",
                "Live",
                livePath);
        }

        public void DeleteRecoveryMarker(string markerPath)
        {
            if (!File.Exists(markerPath) && !Directory.Exists(markerPath)) return;
            InvokeCheckpoint(
                "DeleteRecoveryMarker",
                Before,
                "DeleteRecoveryMarker",
                "RecoveryMarker",
                markerPath);
            try
            {
                File.Delete(markerPath);
            }
            catch (Exception exception)
            {
                throw Wrap("DeleteRecoveryMarker", "RecoveryMarker", markerPath, exception);
            }

            InvokeCheckpoint(
                "DeleteRecoveryMarker",
                After,
                "DeleteRecoveryMarker",
                "RecoveryMarker",
                markerPath);
        }

        private void InvokeCheckpoint(
            string checkpointName,
            string mutationSide,
            string operationRole,
            string artifactRole,
            string path)
        {
            try
            {
                checkpoint?.Invoke(checkpointName, mutationSide);
            }
            catch (SaveEnvelopeInterruptionException)
            {
                throw;
            }
            catch (SaveEnvelopeOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Wrap(operationRole, artifactRole, path, exception);
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
}
