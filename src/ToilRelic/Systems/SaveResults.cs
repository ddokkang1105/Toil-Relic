using ToilRelic.Models;

namespace ToilRelic.Systems;

public enum LoadStatus
{
    Missing,
    Loaded,
    Recovered,
    Unreadable
}

public sealed record LoadResult
{
    private LoadResult(
        LoadStatus status,
        Player? player,
        string? diagnostic,
        bool recoveryNoticePending = false)
    {
        Status = status;
        Player = player;
        Diagnostic = diagnostic;
        RecoveryNoticePending = recoveryNoticePending;
    }

    public LoadStatus Status { get; }
    public Player? Player { get; }
    public string? Diagnostic { get; }
    public bool RecoveryNoticePending { get; }

    public static LoadResult Missing(string? diagnostic = null) =>
        new(LoadStatus.Missing, null, diagnostic);

    public static LoadResult Loaded(Player player, bool recoveryNoticePending = false) =>
        new(
            LoadStatus.Loaded,
            player ?? throw new ArgumentNullException(nameof(player)),
            null,
            recoveryNoticePending);

    public static LoadResult Recovered(Player player, string? diagnostic = null) =>
        new(
            LoadStatus.Recovered,
            player ?? throw new ArgumentNullException(nameof(player)),
            diagnostic,
            recoveryNoticePending: true);

    public static LoadResult Unreadable(string? diagnostic = null) =>
        new(LoadStatus.Unreadable, null, diagnostic);
}

public sealed record PersistenceResult(bool Succeeded, string? Diagnostic)
{
    public static PersistenceResult Success() => new(true, null);
    public static PersistenceResult Failure(string diagnostic) => new(false, diagnostic);
}
