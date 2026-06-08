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

public sealed record RoundStartedCombatEvent(
    int Round
) : ICombatEvent;

public sealed record RoundEndedCombatEvent(
    int Round
) : ICombatEvent;

public sealed record ResourceRefilledCombatEvent(
    CombatantId CombatantId,
    ResourceId ResourceId,
    int PreviousCurrent,
    int NewCurrent,
    int? Max
) : ICombatEvent;
public sealed record CardInstanceCreatedCombatEvent(
    CombatantId CombatantId,
    CardDefinitionId CardDefinitionId,
    IReadOnlyCollection<CardInstanceId> CardInstanceIds,
    CardZone ToZone
) : ICombatEvent;
public sealed record CardPlayedCombatEvent(
    CardDefinitionId CardDefinitionId,
    CombatantId SourceCombatantId,
    CombatantId? TargetCombatantId,
    CardInstanceId? CardInstanceId = null
) : ICombatEvent;

public sealed record CardsDrawnCombatEvent(
    CombatantId CombatantId,
    IReadOnlyCollection<CardInstanceId> CardInstanceIds
) : ICombatEvent;

public sealed record DiscardPileShuffledIntoDrawPileCombatEvent(
    CombatantId CombatantId,
    IReadOnlyCollection<CardInstanceId> CardInstanceIds
) : ICombatEvent;

public sealed record HandDiscardedCombatEvent(
    CombatantId CombatantId,
    IReadOnlyCollection<CardInstanceId> CardInstanceIds
) : ICombatEvent;

public sealed record CardsMovedBetweenZonesCombatEvent(
    CombatantId CombatantId,
    IReadOnlyCollection<CardInstanceId> CardInstanceIds,
    CardZone FromZone,
    CardZone ToZone
) : ICombatEvent;
public sealed record CardMovedToZoneCombatEvent(
    CombatantId CombatantId,
    CardInstanceId CardInstanceId,
    CardZone FromZone,
    CardZone ToZone
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

public sealed record StatusRemovedCombatEvent(
    CombatantId TargetCombatantId,
    IReadOnlyCollection<StatusInstanceId> StatusInstanceIds,
    StatusDefinitionId StatusDefinitionId
) : ICombatEvent;
public sealed record StatusExpiredCombatEvent(
    CombatantId TargetCombatantId,
    StatusInstanceId StatusInstanceId,
    StatusDefinitionId StatusDefinitionId
) : ICombatEvent;

public sealed record CombatantLifecycleChangedCombatEvent(
    CombatantId CombatantId,
    CombatantLifecycleState OldState,
    CombatantLifecycleState NewState
) : ICombatEvent;






