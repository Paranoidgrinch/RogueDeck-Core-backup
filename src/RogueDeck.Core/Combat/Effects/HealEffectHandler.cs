namespace RogueDeck.Core.Combat;

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
            "Healed",
            $"Healed '{heal.TargetCombatantId}' for {healedAmount} HP.");
    }
}