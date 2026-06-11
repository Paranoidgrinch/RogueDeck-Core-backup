namespace RogueDeck.Core.Combat;

public sealed record TriggeredEffectActionSource(
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null)
{
    public static TriggeredEffectActionSource None { get; } = new();
}
