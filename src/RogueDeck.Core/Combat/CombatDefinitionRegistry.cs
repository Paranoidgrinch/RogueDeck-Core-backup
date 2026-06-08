namespace RogueDeck.Core.Combat;

public sealed class CombatDefinitionRegistry
{
    private readonly Dictionary<StatusDefinitionId, StatusDefinition> _statusDefinitions = new();
    private readonly Dictionary<CardDefinitionId, CardDefinition> _cardDefinitions = new();
    private readonly Dictionary<Type, IEffectRequestHandler> _effectRequestHandlers = new();
    private readonly Dictionary<Type, List<ICombatEventHandler>> _combatEventHandlers = new();

    public IReadOnlyDictionary<StatusDefinitionId, StatusDefinition> StatusDefinitions => _statusDefinitions;

    public IReadOnlyDictionary<CardDefinitionId, CardDefinition> CardDefinitions => _cardDefinitions;

    public IReadOnlyDictionary<Type, IEffectRequestHandler> EffectRequestHandlers => _effectRequestHandlers;

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

    public void RegisterCard(CardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_cardDefinitions.ContainsKey(definition.Id))
            throw new InvalidOperationException($"Card definition '{definition.Id}' is already registered.");

        _cardDefinitions.Add(definition.Id, definition);
    }

    public CardDefinition GetCard(CardDefinitionId id)
    {
        if (!_cardDefinitions.TryGetValue(id, out var definition))
            throw new InvalidOperationException($"Card definition '{id}' is not registered.");

        return definition;
    }

    public bool TryGetCard(CardDefinitionId id, out CardDefinition? definition)
    {
        return _cardDefinitions.TryGetValue(id, out definition);
    }

    public void RegisterEffectRequestHandler(IEffectRequestHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_effectRequestHandlers.ContainsKey(handler.RequestType))
            throw new InvalidOperationException(
                $"Effect request handler for '{handler.RequestType.Name}' is already registered.");

        _effectRequestHandlers.Add(handler.RequestType, handler);
    }

    public IEffectRequestHandler GetEffectRequestHandler(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        if (!_effectRequestHandlers.TryGetValue(requestType, out var handler))
            throw new InvalidOperationException(
                $"Effect request handler for '{requestType.Name}' is not registered.");

        return handler;
    }

    public bool TryGetEffectRequestHandler(
        Type requestType,
        out IEffectRequestHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        return _effectRequestHandlers.TryGetValue(requestType, out handler);
    }

    public void RegisterCombatEventHandler(ICombatEventHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_combatEventHandlers.TryGetValue(handler.EventType, out var handlers))
        {
            handlers = new List<ICombatEventHandler>();
            _combatEventHandlers.Add(handler.EventType, handlers);
        }

        if (handlers.Any(existing => existing.GetType() == handler.GetType()))
            throw new InvalidOperationException(
                $"Combat event handler '{handler.GetType().Name}' for event '{handler.EventType.Name}' is already registered.");

        handlers.Add(handler);
    }

    public IReadOnlyCollection<ICombatEventHandler> GetCombatEventHandlers(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        if (!_combatEventHandlers.TryGetValue(eventType, out var handlers))
            return Array.Empty<ICombatEventHandler>();

        return handlers;
    }
}
