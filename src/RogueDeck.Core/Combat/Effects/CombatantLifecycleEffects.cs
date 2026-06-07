namespace RogueDeck.Core.Combat;

public sealed record SetCombatantLifecycleStateEffectRequest(
    CombatantId CombatantId,
    CombatantLifecycleState LifecycleState
) : IEffectRequest;

public sealed class SetCombatantLifecycleStateEffectHandler
    : EffectRequestHandler<SetCombatantLifecycleStateEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        SetCombatantLifecycleStateEffectRequest request)
    {
        var combatant = combat.GetCombatant(request.CombatantId);

        if (combatant.LifecycleState == request.LifecycleState)
            return;

        var oldState = combatant.LifecycleState;

        combatant.SetLifecycleState(request.LifecycleState);

        combat.AddLogEntry(
            "CombatantLifecycleChanged",
            $"Changed lifecycle state of '{request.CombatantId}' from '{oldState}' to '{request.LifecycleState}'.");

        combat.EnqueueEvent(
            new CombatantLifecycleChangedCombatEvent(
                CombatantId: request.CombatantId,
                OldState: oldState,
                NewState: request.LifecycleState));
    }
}

public sealed class MarkCombatantDownedOnZeroHealthHandler : CombatEventHandler<DamageDealtCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DamageDealtCombatEvent combatEvent)
    {
        if (combatEvent.HealthDamage <= 0)
            return;

        if (!combat.TryGetCombatant(combatEvent.TargetCombatantId, out var combatant))
            return;

        if (!combatant!.IsAlive)
            return;

        if (combatant.Health.Current > 0)
            return;

        combat.EnqueueEffect(
            new SetCombatantLifecycleStateEffectRequest(
                CombatantId: combatEvent.TargetCombatantId,
                LifecycleState: CombatantLifecycleState.Downed));
    }
}

