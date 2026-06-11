namespace RogueDeck.Core.Combat;

public sealed record DealDamageEffectRequest(
    CombatantId TargetCombatantId,
    int Amount,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null,
    DamageKind Kind = DamageKind.Direct
) : IEffectRequest;

public sealed class DealDamageEffectHandler : EffectRequestHandler<DealDamageEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DealDamageEffectRequest dealDamage)
    {
        if (dealDamage.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(dealDamage.Amount), "Damage amount cannot be negative.");

        var target = combat.GetCombatant(dealDamage.TargetCombatantId);
        var modifiedAmount = ApplyDamageAmountModifiers(
            combat,
            registry,
            dealDamage,
            target);

        var remainingDamage = modifiedAmount;
        var blockedDamage = 0;

        if (target.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var block) &&
            block.Current > 0)
        {
            blockedDamage = Math.Min(block.Current, remainingDamage);
            block.SetCurrent(block.Current - blockedDamage);
            remainingDamage -= blockedDamage;
        }

        var healthBeforeDamage = target.Health.Current;
        var newHealth = Math.Max(0, healthBeforeDamage - remainingDamage);
        var healthDamage = healthBeforeDamage - newHealth;

        target.Health.SetCurrent(newHealth);

        combat.AddLogEntry(
            StandardCombatLogTypes.DamageDealt,
            $"Dealt {healthDamage} damage to '{dealDamage.TargetCombatantId}' and blocked {blockedDamage} damage.");

        combat.EnqueueEvent(
            new DamageDealtCombatEvent(
                TargetCombatantId: dealDamage.TargetCombatantId,
                HealthDamage: healthDamage,
                BlockedDamage: blockedDamage,
                RequestedAmount: dealDamage.Amount,
                Kind: dealDamage.Kind,
                SourceCombatantId: dealDamage.SourceCombatantId,
                SourceCardId: dealDamage.SourceCardId));

        combat.EnqueueEvent(
            new DamageReceivedCombatEvent(
                ReceiverCombatantId: dealDamage.TargetCombatantId,
                HealthDamage: healthDamage,
                BlockedDamage: blockedDamage,
                RequestedAmount: dealDamage.Amount,
                Kind: dealDamage.Kind,
                SourceCombatantId: dealDamage.SourceCombatantId,
                SourceCardId: dealDamage.SourceCardId));
    }

    private static int ApplyDamageAmountModifiers(
        CombatState combat,
        CombatDefinitionRegistry registry,
        DealDamageEffectRequest dealDamage,
        CombatantState target)
    {
        CombatantState? source = null;

        if (dealDamage.SourceCombatantId is not null &&
            combat.TryGetCombatant(dealDamage.SourceCombatantId.Value, out var foundSource))
        {
            source = foundSource;
        }

        var context = new DamageAmountModificationContext(
            Combat: combat,
            Registry: registry,
            TargetCombatant: target,
            SourceCombatant: source,
            SourceCardId: dealDamage.SourceCardId,
            Kind: dealDamage.Kind,
            RequestedAmount: dealDamage.Amount);

        var currentAmount = dealDamage.Amount;

        foreach (var modifier in registry.GetDamageAmountModifiers())
            currentAmount = Math.Max(0, modifier.ModifyDamageAmount(context, currentAmount));

        return currentAmount;
    }
}

public sealed record HealEffectRequest(
    CombatantId TargetCombatantId,
    int Amount,
    CombatantId? SourceCombatantId = null,
    CardDefinitionId? SourceCardId = null
) : IEffectRequest;

public sealed class HealEffectHandler : EffectRequestHandler<HealEffectRequest>
{
    protected override void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        HealEffectRequest heal)
    {
        if (heal.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(heal.Amount), "Heal amount cannot be negative.");

        var target = combat.GetCombatant(heal.TargetCombatantId);
        var healthBeforeHealing = target.Health.Current;
        var requestedHealth = (long)healthBeforeHealing + heal.Amount;
        var newHealth = (int)Math.Min(target.Health.Max, requestedHealth);
        var healedAmount = newHealth - healthBeforeHealing;

        target.Health.SetCurrent(newHealth);

        combat.AddLogEntry(
            StandardCombatLogTypes.Healed,
            $"Healed '{heal.TargetCombatantId}' for {healedAmount} HP.");

        combat.EnqueueEvent(
            new HealedCombatEvent(
                TargetCombatantId: heal.TargetCombatantId,
                HealedAmount: healedAmount,
                RequestedAmount: heal.Amount,
                SourceCombatantId: heal.SourceCombatantId,
                SourceCardId: heal.SourceCardId));
    }
}
