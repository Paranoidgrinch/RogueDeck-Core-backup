namespace RogueDeck.Core.Combat;

public sealed class GainBlockEffectHandler : IEffectRequestHandler
{
    public Type RequestType => typeof(GainBlockEffectRequest);

    public void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        IEffectRequest request)
    {
        var gainBlock = request as GainBlockEffectRequest
            ?? throw new ArgumentException(
                $"Expected request type '{nameof(GainBlockEffectRequest)}'.",
                nameof(request));

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