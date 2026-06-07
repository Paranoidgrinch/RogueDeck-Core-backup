namespace RogueDeck.Core.Combat;

public sealed record GainBlockEffectRequest(
    CombatantId TargetCombatantId,
    int Amount,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : IEffectRequest;

public sealed class GainBlockEffectHandler : EffectRequestHandler<GainBlockEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        GainBlockEffectRequest gainBlock)
    {
        if (gainBlock.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(gainBlock.Amount), "Block amount cannot be negative.");

        var target = combat.GetCombatant(gainBlock.TargetCombatantId);

        if (!target.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var block))
        {
            target.DefensivePools.Add(
                StandardCombatIds.BlockDefensivePool,
                new ValuePoolState(current: gainBlock.Amount));

            combat.AddLogEntry(
                "BlockGained",
                $"Gained {gainBlock.Amount} block on '{gainBlock.TargetCombatantId}'.");

            return;
        }

        var requestedBlock = (long)block.Current + gainBlock.Amount;
        var newBlock = (int)Math.Min(int.MaxValue, requestedBlock);

        block.SetCurrent(newBlock);

        combat.AddLogEntry(
            "BlockGained",
            $"Gained {gainBlock.Amount} block on '{gainBlock.TargetCombatantId}'.");
    }
}

public sealed record ClearDefensivePoolEffectRequest(
    CombatantId TargetCombatantId,
    DefensivePoolId PoolId
) : IEffectRequest;

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

public sealed class ClearBlockOnTurnStartedHandler : CombatEventHandler<TurnStartedCombatEvent>
{
    protected override void Handle(
        CombatState combat,
        CombatDefinitionRegistry registry,
        TurnStartedCombatEvent combatEvent)
    {
        if (!combat.TryGetCombatant(combatEvent.CombatantId, out var combatant))
            return;

        if (!combatant!.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var block))
            return;

        if (block.Current <= 0)
            return;

        combat.EnqueueEffect(
            new ClearDefensivePoolEffectRequest(
                TargetCombatantId: combatEvent.CombatantId,
                PoolId: StandardCombatIds.BlockDefensivePool));
    }
}
