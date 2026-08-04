using System.Reflection;
using ToilRelic.Models;

namespace ToilRelic.Tests;

internal sealed class EquipmentCatalogFixtureScope : IDisposable
{
    private readonly Dictionary<string, EquipmentDefinition> _definitions;
    private readonly KeyValuePair<string, EquipmentDefinition>[] _snapshot;
    private bool _disposed;

    private EquipmentCatalogFixtureScope(Dictionary<string, EquipmentDefinition> definitions)
    {
        _definitions = definitions;
        _snapshot = definitions.ToArray();
    }

    public static EquipmentCatalogFixtureScope Install(params EquipmentDefinition[] additions) =>
        Install((IEnumerable<EquipmentDefinition>)additions);

    public static EquipmentCatalogFixtureScope Install(IEnumerable<EquipmentDefinition> additions)
    {
        var field = typeof(EquipmentCatalog).GetField("Definitions", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("EquipmentCatalog.Definitions was not found.");
        var definitions = (Dictionary<string, EquipmentDefinition>?)field.GetValue(null)
            ?? throw new InvalidOperationException("EquipmentCatalog.Definitions is unavailable.");
        var scope = new EquipmentCatalogFixtureScope(definitions);

        try
        {
            foreach (var addition in additions)
            {
                definitions.Add(addition.Id, addition);
            }

            return scope;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    public bool Remove(string equipmentId) => _definitions.Remove(equipmentId);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _definitions.Clear();
        foreach (var pair in _snapshot)
        {
            _definitions.Add(pair.Key, pair.Value);
        }

        _disposed = true;
    }
}
