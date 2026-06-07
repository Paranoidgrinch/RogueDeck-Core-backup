using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class DamageDealtCombatEventTests
{
    [Fact]
    public void DealDamageEnqueuesDamageDealtEvent()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CreateCombatWithHero();

        var heroId = new CombatantId("hero_001");
        var hero = combat.GetCombatant(heroId);

        hero.DefensivePools.Add(
            StandardCombatIds.BlockDefensivePool,
            new ValuePoolState(current: 3));

        var resolver = new CombatEffectResolver();

        resolver.Resolve(
            combat,
            registry,
            new DealDamageEffectRequest(
                TargetCombatantId: heroId,
                Amount: 5));

        var combatEvent = Assert.Single(combat.PendingEvents);
        var damageEvent = Assert.IsType<DamageDealtCombatEvent>(combatEvent);

        Assert.Equal(heroId, damageEvent.TargetCombatantId);
        Assert.Equal(2, damageEvent.HealthDamage);
        Assert.Equal(3, damageEvent.BlockedDamage);
        Assert.Equal(5, damageEvent.RequestedAmount);
    }

    [Fact]
    public void CombatQueueProcessorProcessesDamageDealtEventHandlers()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        registry.RegisterCombatEventHandler(new AddLogOnDamageDealtHandler());

        var combat = CreateCombatWithHero();

        combat.EnqueueEffect(
            new DealDamageEffectRequest(
                TargetCombatantId: new CombatantId("hero_001"),
                Amount: 4));

        var processor = new CombatQueueProcessor();

        processor.ResolvePendingQueues(combat, registry);

        Assert.Equal(0, combat.PendingEffectCount);
        Assert.Equal(0, combat.PendingEventCount);
        Assert.Contains(combat.CombatLog, entry => entry.Type == "DamageDealt");
        Assert.Contains(combat.CombatLog, entry => entry.Type == "DamageEventHandled");
    }

    private static CombatState CreateCombatWithHero()
    {
        var combat = new CombatState(
            new CombatId("combat_001"),
            randomSeed: 12345);

        var hero = new CombatantState(
            new CombatantId("hero_001"),
            new CombatantDefinitionId("standard.hero"),
            "combatant.hero",
            new TeamId("player"),
            new HealthState(current: 20, max: 20));

        combat.AddCombatant(hero);

        return combat;
    }

    private sealed class AddLogOnDamageDealtHandler : CombatEventHandler<DamageDealtCombatEvent>
    {
        protected override void Handle(
            CombatState combat,
            CombatDefinitionRegistry registry,
            DamageDealtCombatEvent combatEvent)
        {
            combat.AddLogEntry(
                "DamageEventHandled",
                $"Handled damage event for '{combatEvent.TargetCombatantId}'.");
        }
    }
}
