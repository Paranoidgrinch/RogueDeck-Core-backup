namespace RogueDeck.Core.Combat;

public sealed class HealEffectHandler : IEffectRequestHandler
{
    public Type RequestType => typeof(HealEffectRequest);

    public void Resolve(
        CombatState combat,
        CombatDefinitionRegistry registry,
        IEffectRequest request)
    {
        var heal = request as HealEffectRequest
            ?? throw new ArgumentException(
                $"Expected request type '{nameof(HealEffectRequest)}'.",
                nameof(request));

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