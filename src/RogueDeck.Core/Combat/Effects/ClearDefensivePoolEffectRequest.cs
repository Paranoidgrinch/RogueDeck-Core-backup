namespace RogueDeck.Core.Combat;

public sealed record ClearDefensivePoolEffectRequest(
    CombatantId TargetCombatantId,
    DefensivePoolId PoolId
) : IEffectRequest;