namespace RogueDeck.Core.Combat;

public sealed class DealDamageEffectHandler : IEffectRequestHandler
{
    public Type RequestType => typeof(DealDamageEffectRequest);

    public void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        IEffectRequest request)
    {
        var dealDamage = request as DealDamageEffectRequest
            ?? throw new ArgumentException(
                $"Expected request type '{nameof(DealDamageEffectRequest)}'.",
                nameof(request));

        if (dealDamage.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(dealDamage.Amount), "Damage amount cannot be negative.");

        var target = combat.GetCombatant(dealDamage.TargetCombatantId);

        var remainingDamage = dealDamage.Amount;
        var blockedDamage = 0;

        if (target.DefensivePools.TryGetValue(StandardCombatIds.BlockDefensivePool, out var block) && block.Current > 0)
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
            "DamageDealt",
            $"Dealt {healthDamage} damage to '{dealDamage.TargetCombatantId}' and blocked {blockedDamage} damage.");
    }
}