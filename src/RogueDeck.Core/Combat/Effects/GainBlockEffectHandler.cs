namespace RogueDeck.Core.Combat;

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