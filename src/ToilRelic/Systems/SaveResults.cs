using ToilRelic.Models;

namespace ToilRelic.Systems;

public enum LoadStatus
{
    Missing,
    Loaded,
    Unreadable
}

public sealed record LoadResult(LoadStatus Status, Player? Player, string? Diagnostic)
{
    public static LoadResult Missing() => new(LoadStatus.Missing, null, null);
    public static LoadResult Loaded(Player player) => new(LoadStatus.Loaded, player, null);
    public static LoadResult Unreadable(string diagnostic) => new(LoadStatus.Unreadable, null, diagnostic);
}

public sealed record PersistenceResult(bool Succeeded, string? Diagnostic)
{
    public static PersistenceResult Success() => new(true, null);
    public static PersistenceResult Failure(string diagnostic) => new(false, diagnostic);
}
