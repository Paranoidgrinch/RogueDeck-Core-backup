namespace RogueDeck.Core.Combat;

public sealed record SetCombatResultEffectRequest(
    CombatResult Result
) : IEffectRequest;

public sealed class SetCombatResultEffectHandler : EffectRequestHandler<SetCombatResultEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        SetCombatResultEffectRequest request)
    {
        if (combat.Result == request.Result)
            return;

        var oldResult = combat.Result;

        combat.SetResult(request.Result);

        combat.AddLogEntry(
            "CombatResultChanged",
            $"Changed combat result from '{oldResult}' to '{request.Result}'.");
    }
}

public sealed class UpdateStandardCombatResultOnLifecycleChangedHandler
    : CombatEventHandler<CombatantLifecycleChangedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantLifecycleChangedCombatEvent combatEvent)
    {
        if (combat.Result != CombatResult.Ongoing)
            return;

        var hasLivingPlayer = combat.HasLivingCombatantsOnTeam(StandardCombatIds.PlayerTeam);

        var hasLivingEnemy = combat.HasLivingCombatantsOnTeam(StandardCombatIds.EnemyTeam);

        if (!hasLivingPlayer && !hasLivingEnemy)
        {
            combat.EnqueueEffect(new SetCombatResultEffectRequest(CombatResult.Draw));
            return;
        }

        if (!hasLivingPlayer)
        {
            combat.EnqueueEffect(new SetCombatResultEffectRequest(CombatResult.Defeat));
            return;
        }

        if (!hasLivingEnemy)
        {
            combat.EnqueueEffect(new SetCombatResultEffectRequest(CombatResult.Victory));
        }
    }
}


