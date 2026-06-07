namespace RogueDeck.Core.Combat;

public interface ICombatEventHandler
{
    Type EventType { get; }

    void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ICombatEvent combatEvent);
}

public abstract class CombatEventHandler<TEvent> : ICombatEventHandler
    where TEvent : ICombatEvent
{
    public Type EventType => typeof(TEvent);

    public void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ICombatEvent combatEvent)
    {
        if (combatEvent is not TEvent typedEvent)
            throw new ArgumentException(
                $"Expected event type '{typeof(TEvent).Name}'.",
                nameof(combatEvent));

        Handle(combat, registry, typedEvent);
    }

    protected abstract void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TEvent combatEvent);
}

public sealed class CombatEventQueueProcessor
{
    public void ResolvePendingEvents(
        CombatState combat,
        CombatDefinitionRegistry registry,
        int maxEventsToResolve = 1024)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(registry);

        if (maxEventsToResolve <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxEventsToResolve),
                "Maximum events to resolve must be greater than zero.");

        var resolvedEvents = 0;

        while (combat.HasPendingEvents)
        {
            if (resolvedEvents >= maxEventsToResolve)
                throw new InvalidOperationException(
                    $"Stopped resolving pending events after reaching the limit of {maxEventsToResolve} events.");

            var combatEvent = combat.DequeueNextEvent();
            var handlers = registry.GetCombatEventHandlers(combatEvent.GetType());

            foreach (var handler in handlers)
                handler.Handle(combat, registry, combatEvent);

            resolvedEvents++;
        }
    }
}
