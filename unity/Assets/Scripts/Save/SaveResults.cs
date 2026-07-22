using ToilRelic.Unity.Core;

namespace ToilRelic.Unity.Save
{
    public enum SaveLoadStatus
    {
        Missing,
        Loaded,
        Unreadable
    }

    public sealed class SaveLoadResult
    {
        private SaveLoadResult(SaveLoadStatus status, PlayerState player, string diagnostic)
        {
            Status = status;
            Player = player;
            Diagnostic = diagnostic;
        }

        public SaveLoadStatus Status { get; }
        public PlayerState Player { get; }
        public string Diagnostic { get; }

        public static SaveLoadResult Missing() => new(SaveLoadStatus.Missing, null, null);
        public static SaveLoadResult Loaded(PlayerState player) => new(SaveLoadStatus.Loaded, player, null);
        public static SaveLoadResult Unreadable(string diagnostic) => new(SaveLoadStatus.Unreadable, null, diagnostic);
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
