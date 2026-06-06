namespace RogueDeck.Core.Combat;

public sealed class CombatantState
{
    public CombatantId Id { get; }
    public CombatantDefinitionId DefinitionId { get; }
    public string DisplayNameKey { get; }

    public TeamId TeamId { get; private set; }
    public CombatantId? OwnerId { get; private set; }
    public CombatantId? ControllerId { get; private set; }

    public CombatantLifecycleState LifecycleState { get; private set; }
    public HealthState Health { get; }

    public Dictionary<ResourceId, ValuePoolState> Resources { get; } = new();
    public Dictionary<DefensivePoolId, ValuePoolState> DefensivePools { get; } = new();

    public List<StatusInstance> Statuses { get; } = new();
    public HashSet<TagId> Tags { get; } = new();
    public Dictionary<CounterId, int> Counters { get; } = new();

    public string? PositionKey { get; private set; }
    public string? IntentKey { get; private set; }

    public bool IsAlive => LifecycleState == CombatantLifecycleState.Alive;

    public CombatantState(
        CombatantId id,
        CombatantDefinitionId definitionId,
        string displayNameKey,
        TeamId teamId,
        HealthState health)
    {
        if (string.IsNullOrWhiteSpace(displayNameKey))
            throw new ArgumentException("Display name key cannot be empty.", nameof(displayNameKey));

        Id = id;
        DefinitionId = definitionId;
        DisplayNameKey = displayNameKey;
        TeamId = teamId;
        Health = health;
        LifecycleState = CombatantLifecycleState.Alive;
    }

    public void SetTeam(TeamId teamId)
    {
        TeamId = teamId;
    }

    public void SetOwner(CombatantId? ownerId)
    {
        OwnerId = ownerId;
    }

    public void SetController(CombatantId? controllerId)
    {
        ControllerId = controllerId;
    }

    public void SetLifecycleState(CombatantLifecycleState lifecycleState)
    {
        LifecycleState = lifecycleState;
    }

    public void SetPosition(string? positionKey)
    {
        PositionKey = positionKey;
    }

    public void SetIntent(string? intentKey)
    {
        IntentKey = intentKey;
    }
}