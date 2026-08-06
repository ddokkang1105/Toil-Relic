namespace ToilRelic.Models;

public sealed record EquipmentDropProfile(string Id, string EquipmentId, double Chance)
{
    public bool HasValidShape =>
        !string.IsNullOrWhiteSpace(Id) &&
        !string.IsNullOrWhiteSpace(EquipmentId) &&
        Chance > 0d &&
        Chance <= 1d;
}
