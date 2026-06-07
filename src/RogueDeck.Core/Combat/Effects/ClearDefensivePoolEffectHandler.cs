namespace RogueDeck.Core.Combat;

public sealed class ClearDefensivePoolEffectHandler : EffectRequestHandler<ClearDefensivePoolEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        ClearDefensivePoolEffectRequest clearPool)
    {
        var target = combat.GetCombatant(clearPool.TargetCombatantId);

        if (!target.DefensivePools.TryGetValue(clearPool.PoolId, out var pool))
        {
            combat.AddLogEntry(
                "DefensivePoolCleared",
                $"Cleared 0 from defensive pool '{clearPool.PoolId}' on '{clearPool.TargetCombatantId}'.");

            return;
        }

        var clearedAmount = pool.Current;

        pool.SetCurrent(0);

        combat.AddLogEntry(
            "DefensivePoolCleared",
            $"Cleared {clearedAmount} from defensive pool '{clearPool.PoolId}' on '{clearPool.TargetCombatantId}'.");
    }
}