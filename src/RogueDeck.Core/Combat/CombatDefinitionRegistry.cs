namespace RogueDeck.Core.Combat;

public sealed class CombatDefinitionRegistry
{
    private readonly Dictionary<StatusDefinitionId, StatusDefinition> _statusDefinitions = new();
    private readonly Dictionary<CardDefinitionId, CardDefinition> _cardDefinitions = new();
    private readonly Dictionary<Type, ICardEffectDefinitionHandler> _cardEffectDefinitionHandlers = new();
    private readonly Dictionary<Type, IEffectRequestHandler> _effectRequestHandlers = new();
    private readonly Dictionary<Type, List<ICombatEventHandler>> _combatEventHandlers = new();
    private readonly List<IDamageAmountModifier> _damageAmountModifiers = new();
    private readonly List<ICardPlayValidator> _cardPlayValidators = new();
    private readonly List<ICardCostModifier> _cardCostModifiers = new();

    
    private readonly List<IStatusApplicationInterceptor> _statusApplicationInterceptors = new();
public IReadOnlyDictionary<StatusDefinitionId, StatusDefinition> StatusDefinitions => _statusDefinitions;
    public IReadOnlyDictionary<CardDefinitionId, CardDefinition> CardDefinitions => _cardDefinitions;
    public IReadOnlyDictionary<Type, ICardEffectDefinitionHandler> CardEffectDefinitionHandlers => _cardEffectDefinitionHandlers;
    public IReadOnlyDictionary<Type, IEffectRequestHandler> EffectRequestHandlers => _effectRequestHandlers;
    public IReadOnlyCollection<IDamageAmountModifier> DamageAmountModifiers => _damageAmountModifiers;
    public IReadOnlyCollection<ICardPlayValidator> CardPlayValidators => _cardPlayValidators;
    public IReadOnlyCollection<ICardCostModifier> CardCostModifiers => _cardCostModifiers;

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

    public void RegisterCardEffectDefinitionHandler(ICardEffectDefinitionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_cardEffectDefinitionHandlers.ContainsKey(handler.DefinitionType))
            throw new InvalidOperationException(
                $"Card effect definition handler for '{handler.DefinitionType.Name}' is already registered.");

        _cardEffectDefinitionHandlers.Add(handler.DefinitionType, handler);
    }

    public ICardEffectDefinitionHandler GetCardEffectDefinitionHandler(Type definitionType)
    {
        ArgumentNullException.ThrowIfNull(definitionType);

        if (!_cardEffectDefinitionHandlers.TryGetValue(definitionType, out var handler))
            throw new InvalidOperationException(
                $"Card effect definition handler for '{definitionType.Name}' is not registered.");

        return handler;
    }

    public bool TryGetCardEffectDefinitionHandler(
        Type definitionType,
        out ICardEffectDefinitionHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(definitionType);

        return _cardEffectDefinitionHandlers.TryGetValue(definitionType, out handler);
    }

    public void RegisterDamageAmountModifier(IDamageAmountModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        if (_damageAmountModifiers.Any(existing => existing.GetType() == modifier.GetType()))
            throw new InvalidOperationException(
                $"Damage amount modifier '{modifier.GetType().Name}' is already registered.");

        _damageAmountModifiers.Add(modifier);
        _damageAmountModifiers.Sort(static (left, right) =>
        {
            var priorityComparison = left.Priority.CompareTo(right.Priority);

            if (priorityComparison != 0)
                return priorityComparison;

            return string.Compare(
                left.GetType().FullName,
                right.GetType().FullName,
                StringComparison.Ordinal);
        });
    }

    public IReadOnlyList<IDamageAmountModifier> GetDamageAmountModifiers()
    {
        return _damageAmountModifiers.ToArray();
    }

    public void RegisterCardPlayValidator(ICardPlayValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        if (_cardPlayValidators.Any(existing => existing.GetType() == validator.GetType()))
            throw new InvalidOperationException(
                $"Card play validator '{validator.GetType().Name}' is already registered.");

        _cardPlayValidators.Add(validator);
        _cardPlayValidators.Sort(static (left, right) =>
        {
            var priorityComparison = left.Priority.CompareTo(right.Priority);

            if (priorityComparison != 0)
                return priorityComparison;

            return string.Compare(
                left.GetType().FullName,
                right.GetType().FullName,
                StringComparison.Ordinal);
        });
    }

    public IReadOnlyList<ICardPlayValidator> GetCardPlayValidators()
    {
        return _cardPlayValidators.ToArray();
    }

    public void RegisterCardCostModifier(ICardCostModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        if (_cardCostModifiers.Any(existing => existing.GetType() == modifier.GetType()))
            throw new InvalidOperationException(
                $"Card cost modifier '{modifier.GetType().Name}' is already registered.");

        _cardCostModifiers.Add(modifier);
        _cardCostModifiers.Sort(static (left, right) =>
        {
            var priorityComparison = left.Priority.CompareTo(right.Priority);

            if (priorityComparison != 0)
                return priorityComparison;

            return string.Compare(
                left.GetType().FullName,
                right.GetType().FullName,
                StringComparison.Ordinal);
        });
    }

    public IReadOnlyList<ICardCostModifier> GetCardCostModifiers()
    {
        return _cardCostModifiers.ToArray();
    }    public void RegisterStatusApplicationInterceptor(IStatusApplicationInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);

        if (_statusApplicationInterceptors.Any(existing => existing.GetType() == interceptor.GetType()))
            throw new InvalidOperationException(
                $"Status application interceptor '{interceptor.GetType().Name}' is already registered.");

        _statusApplicationInterceptors.Add(interceptor);
        _statusApplicationInterceptors.Sort(static (left, right) =>
        {
            var priorityComparison = left.Priority.CompareTo(right.Priority);

            if (priorityComparison != 0)
                return priorityComparison;

            return string.Compare(
                left.GetType().FullName,
                right.GetType().FullName,
                StringComparison.Ordinal);
        });
    }

    public IReadOnlyList<IStatusApplicationInterceptor> GetStatusApplicationInterceptors()
    {
        return _statusApplicationInterceptors.ToArray();
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

