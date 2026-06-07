namespace RogueDeck.Core.Combat;

public sealed record TurnStartedCombatEvent(
    CombatantId CombatantId,
    int Round,
    int Turn
) : ICombatEvent;

public sealed record TurnEndedCombatEvent(
    CombatantId CombatantId,
    int Round,
    int Turn
) : ICombatEvent;

public sealed record DamageDealtCombatEvent(
    CombatantId TargetCombatantId,
    int HealthDamage,
    int BlockedDamage,
    int RequestedAmount,
    DamageKind Kind = DamageKind.Direct,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : ICombatEvent;


public sealed record CombatantLifecycleChangedCombatEvent(
    CombatantId CombatantId,
    CombatantLifecycleState OldState,
    CombatantLifecycleState NewState
) : ICombatEvent;

public sealed record HealedCombatEvent(
    CombatantId TargetCombatantId,
    int HealedAmount,
    int RequestedAmount,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : ICombatEvent;

public sealed record StatusAppliedCombatEvent(
    CombatantId TargetCombatantId,
    StatusInstanceId StatusInstanceId,
    StatusDefinitionId StatusDefinitionId,
    int Stacks,
    int DurationTurns,
    int Charges,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : ICombatEvent;

public sealed record StatusMergedCombatEvent(
    CombatantId TargetCombatantId,
    StatusInstanceId StatusInstanceId,
    StatusDefinitionId StatusDefinitionId,
    int Stacks,
    int DurationTurns,
    int Charges,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : ICombatEvent;

public sealed record StatusExpiredCombatEvent(
    CombatantId TargetCombatantId,
    StatusInstanceId StatusInstanceId,
    StatusDefinitionId StatusDefinitionId
) : ICombatEvent;
