using ToilRelic.Unity.Core;

namespace ToilRelic.Unity.Save
{
    public enum SaveLoadStatus
    {
        Missing,
        Loaded,
        Recovered,
        Unreadable
    }

    public sealed class SaveLoadResult
    {
        private SaveLoadResult(
            SaveLoadStatus status,
            PlayerState player,
            string diagnostic,
            bool recoveryNoticePending)
        {
            Status = status;
            Player = player;
            Diagnostic = diagnostic;
            RecoveryNoticePending = recoveryNoticePending;
        }

        public SaveLoadStatus Status { get; }
        public PlayerState Player { get; }
        public string Diagnostic { get; }
        public bool RecoveryNoticePending { get; }

        public static SaveLoadResult Missing(string diagnostic = null) =>
            new(SaveLoadStatus.Missing, null, diagnostic, false);

        public static SaveLoadResult Loaded(PlayerState player, bool recoveryNoticePending = false)
        {
            if (player == null)
            {
                throw new System.ArgumentNullException(nameof(player));
            }

            return new SaveLoadResult(SaveLoadStatus.Loaded, player, null, recoveryNoticePending);
        }

        public static SaveLoadResult Recovered(PlayerState player, string diagnostic = null)
        {
            if (player == null)
            {
                throw new System.ArgumentNullException(nameof(player));
            }

            return new SaveLoadResult(SaveLoadStatus.Recovered, player, diagnostic, true);
        }

        public static SaveLoadResult Unreadable(string diagnostic = null) =>
            new(SaveLoadStatus.Unreadable, null, diagnostic, false);
    }

    public sealed class SaveOperationResult
    {
        private SaveOperationResult(bool succeeded, string diagnostic)
        {
            Succeeded = succeeded;
            Diagnostic = diagnostic;
        }

        public bool Succeeded { get; }
        public string Diagnostic { get; }

        public static SaveOperationResult Success() => new(true, null);
        public static SaveOperationResult Failure(string diagnostic) => new(false, diagnostic);
    }
}
