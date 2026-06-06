namespace RogueDeck.Core.Combat;

public sealed class CombatDefinitionRegistry
{
    private readonly Dictionary<StatusDefinitionId, StatusDefinition> _statusDefinitions = new();

    public IReadOnlyDictionary<StatusDefinitionId, StatusDefinition> StatusDefinitions => _statusDefinitions;

    public void RegisterStatus(StatusDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_statusDefinitions.ContainsKey(definition.Id))
            throw new InvalidOperationException($"Status definition '{definition.Id}' is already registered.");

        _statusDefinitions.Add(definition.Id, definition);
    }

    public StatusDefinition GetStatus(StatusDefinitionId id)
    {
        if (!_statusDefinitions.TryGetValue(id, out var definition))
            throw new InvalidOperationException($"Status definition '{id}' is not registered.");

        return definition;
    }

    public bool TryGetStatus(StatusDefinitionId id, out StatusDefinition? definition)
    {
        return _statusDefinitions.TryGetValue(id, out definition);
    }
}