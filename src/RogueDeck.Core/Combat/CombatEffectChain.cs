namespace RogueDeck.Core.Combat;

public readonly record struct CombatEffectChainId(long Value)
{
    public override string ToString() => Value.ToString();
}

public sealed class CombatEffectChainContext
{
    private readonly IReadOnlyList<TriggeredEffectDefinitionId> _triggeredEffectDefinitionIds;

    public CombatEffectChainId Id { get; }

    public int TriggerDepth => _triggeredEffectDefinitionIds.Count;

    public IReadOnlyList<TriggeredEffectDefinitionId> TriggeredEffectDefinitionIds =>
        _triggeredEffectDefinitionIds;

    internal CombatEffectChainContext(
        CombatEffectChainId id,
        IEnumerable<TriggeredEffectDefinitionId> triggeredEffectDefinitionIds)
    {
        ArgumentNullException.ThrowIfNull(triggeredEffectDefinitionIds);

        Id = id;
        _triggeredEffectDefinitionIds =
            Array.AsReadOnly(triggeredEffectDefinitionIds.ToArray());
    }

    public bool ContainsTriggeredEffectDefinition(
        TriggeredEffectDefinitionId definitionId)
    {
        return _triggeredEffectDefinitionIds.Contains(definitionId);
    }

    internal CombatEffectChainContext AppendTriggeredEffectDefinition(
        TriggeredEffectDefinitionId definitionId)
    {
        return new CombatEffectChainContext(
            Id,
            _triggeredEffectDefinitionIds.Append(definitionId));
    }
}

internal readonly record struct PendingEffectQueueEntry(
    IEffectRequest Request,
    CombatEffectChainContext EffectChain);

internal readonly record struct PendingEventQueueEntry(
    ICombatEvent CombatEvent,
    CombatEffectChainContext EffectChain);
