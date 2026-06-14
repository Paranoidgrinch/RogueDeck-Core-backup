using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardCostPaidTriggeredEffectTests
{
    private static readonly CombatantId HeroId = new("hero_001");
    private static readonly CombatantId GoblinId = new("goblin_001");
    private static readonly ResourceId FuryResourceId = new("test.fury");

    [Fact]
    public void StandardCombatPackageRegistersCardCostPaidTriggeredEffectHandler()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        Assert.Contains(
            registry.GetCombatEventHandlers(typeof(CardCostPaidCombatEvent)),
            handler => handler is CardCostPaidTriggeredEffectHandler);
    }

    [Fact]
    public void CardCostPaidTriggeredEffectFiresWhenCardCostIsPaid()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(combat.GetCombatant(HeroId), current: 3, max: 3);

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_block"));

        definition.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardCostPaidTriggeredEffectAmount(3)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            3,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardCostPaidTriggeredEffectDoesNotFireWhenCardIsFree()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        var hero = combat.GetCombatant(HeroId);
        EnsureEnergy(hero, current: 0, max: 3);
        ApplyFreeNextCard(combat, registry, HeroId, charges: 1);

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_free_card"));

        definition.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardCostPaidTriggeredEffectAmount(5)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardCostPaidTriggeredEffectFiltersByCardDefinition()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(combat.GetCombatant(HeroId), current: 3, max: 3);

        var matching = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_matching_card"));

        matching.Filters.Add(
            new CardCostPaidCardDefinitionTriggerFilter(StandardCombatIds.StrikeCard));

        matching.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardCostPaidTriggeredEffectAmount(2)));

        var nonMatching = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_non_matching_card"));

        nonMatching.Filters.Add(
            new CardCostPaidCardDefinitionTriggerFilter(StandardCombatIds.DefendCard));

        nonMatching.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardCostPaidTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        PlayStrike(combat, registry);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardCostPaidTriggeredEffectFiltersByResourceId()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(combat.GetCombatant(HeroId), current: 3, max: 3);

        var matching = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_matching_resource"));

        matching.Filters.Add(
            new CardCostPaidResourceIdTriggerFilter(StandardCombatIds.EnergyResource));

        matching.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardCostPaidTriggeredEffectAmount(2)));

        var nonMatching = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_non_matching_resource"));

        nonMatching.Filters.Add(
            new CardCostPaidResourceIdTriggerFilter(FuryResourceId));

        nonMatching.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardCostPaidTriggeredEffectAmount(7)));

        registry.RegisterTriggeredEffectDefinition(matching);
        registry.RegisterTriggeredEffectDefinition(nonMatching);

        PlayStrike(combat, registry);

        Assert.Equal(
            2,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardCostPaidTriggeredEffectMinimumTotalAmountFilterWorks()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(combat.GetCombatant(HeroId), current: 3, max: 3);

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_minimum_total"));

        definition.Filters.Add(
            new CardCostPaidMinimumTotalAmountTriggerFilter(MinimumTotalAmount: 2));

        definition.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new FixedCardCostPaidTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            0,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardCostPaidTriggeredEffectCanUseTotalPaidAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(combat.GetCombatant(HeroId), current: 3, max: 3);

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_total_amount"));

        definition.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.Source,
                new CardCostPaidTotalAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            1,
            GetBlockOrZero(combat, HeroId));
    }

    [Fact]
    public void CardCostPaidTriggeredEffectCanTargetPayerAsEventTarget()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(combat.GetCombatant(HeroId), current: 3, max: 3);

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("test.card_cost_paid_event_target"));

        definition.Actions.Add(
            new GainBlockOnCardCostPaidTriggeredEffectAction(
                CombatantTargetSelectors.EventTarget,
                new FixedCardCostPaidTriggeredEffectAmount(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void CardCostPaidGenericGainBlockRecipeFires()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(
            combat.GetCombatant(HeroId),
            current: 3,
            max: 3);

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.card_cost_paid_generic_gain_block"));

        definition.Effects.Add(
            new GainBlockEffectRecipe<CardCostPaidTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new FixedCombatValue<int>(4)));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            4,
            GetBlockOrZero(combat, HeroId));

        Assert.Equal(
            0,
            GetBlockOrZero(combat, GoblinId));
    }

    [Fact]
    public void CardCostPaidGenericHealRecipeUsesTotalPaidAmount()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();
        var hero = combat.GetCombatant(HeroId);

        EnsureEnergy(
            hero,
            current: 3,
            max: 3);

        var healthBefore = hero.Health.Max - 3;
        hero.Health.SetCurrent(healthBefore);

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.card_cost_paid_generic_heal"));

        definition.Effects.Add(
            new HealEffectRecipe<CardCostPaidTriggeredEffectContext>(
                CombatantTargetSelectors.EventTarget,
                new CardCostPaidTotalAmount()));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            healthBefore + 1,
            hero.Health.Current);
    }

    [Fact]
    public void CardCostPaidRunsLegacyActionsBeforeGenericEffectsInListOrder()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();
        var combat = CombatTestFactory.CreateCombatWithHeroAndGoblin();

        EnsureEnergy(
            combat.GetCombatant(HeroId),
            current: 3,
            max: 3);

        var order = new List<string>();

        var definition = new CardCostPaidTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId(
                "test.card_cost_paid_order"));

        definition.Actions.Add(
            new RecordingCardCostPaidAction(
                order,
                "legacy-a"));

        definition.Actions.Add(
            new RecordingCardCostPaidAction(
                order,
                "legacy-b"));

        definition.Effects.Add(
            new RecordingCardCostPaidRecipe(
                order,
                "recipe-a"));

        definition.Effects.Add(
            new RecordingCardCostPaidRecipe(
                order,
                "recipe-b"));

        registry.RegisterTriggeredEffectDefinition(definition);

        PlayStrike(combat, registry);

        Assert.Equal(
            new[]
            {
                "legacy-a",
                "legacy-b",
                "recipe-a",
                "recipe-b"
            },
            order);
    }

    private sealed class RecordingCardCostPaidAction
        : ICardCostPaidTriggeredEffectAction
    {
        private readonly ICollection<string> _order;
        private readonly string _entry;

        public RecordingCardCostPaidAction(
            ICollection<string> order,
            string entry)
        {
            _order = order;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardCostPaidTriggeredEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _order.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }

    private sealed class RecordingCardCostPaidRecipe
        : ICombatEffectRecipe<CardCostPaidTriggeredEffectContext>
    {
        private readonly ICollection<string> _order;
        private readonly string _entry;

        public RecordingCardCostPaidRecipe(
            ICollection<string> order,
            string entry)
        {
            _order = order;
            _entry = entry;
        }

        public IReadOnlyCollection<IEffectRequest> BuildEffectRequests(
            CardCostPaidTriggeredEffectContext context,
            TriggeredEffectActionBuildContext buildContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(buildContext);

            _order.Add(_entry);

            return Array.Empty<IEffectRequest>();
        }
    }
    private static void PlayStrike(
        CombatState combat,
        CombatDefinitionRegistry registry)
    {
        var strike = AddCardToZone(
            combat,
            HeroId,
            StandardCombatIds.StrikeCard,
            CardZone.Hand);

        new CombatCardPlayProcessor().PlayCardInstance(
            combat,
            registry,
            new CardInstancePlayRequest(
                CardInstanceId: strike.Id,
                SourceCombatantId: HeroId,
                TargetCombatantId: GoblinId));
    }

    private static void ApplyFreeNextCard(
        CombatState combat,
        CombatDefinitionRegistry registry,
        CombatantId targetId,
        int charges)
    {
        combat.EnqueueEffect(new ApplyStatusEffectRequest(
            TargetCombatantId: targetId,
            StatusDefinitionId: StandardCombatIds.FreeNextCardStatus,
            Stacks: 0,
            DurationTurns: 0,
            Charges: charges));

        new CombatQueueProcessor().ResolvePendingQueues(combat, registry);
    }

    private static void EnsureEnergy(
        CombatantState combatant,
        int current,
        int max)
    {
        if (combatant.Resources.TryGetValue(StandardCombatIds.EnergyResource, out var energy))
        {
            energy.SetMax(max);
            energy.SetCurrent(current);
            return;
        }

        combatant.Resources.Add(
            StandardCombatIds.EnergyResource,
            new ValuePoolState(current: current, max: max));
    }

    private static int GetBlockOrZero(
        CombatState combat,
        CombatantId combatantId)
    {
        var combatant = combat.GetCombatant(combatantId);

        return combatant.DefensivePools.TryGetValue(
            StandardCombatIds.BlockDefensivePool,
            out var blockPool)
                ? blockPool.Current
                : 0;
    }

    private static CardInstance AddCardToZone(
        CombatState combat,
        CombatantId ownerId,
        CardDefinitionId definitionId,
        CardZone zone)
    {
        var card = new CardInstance(
            combat.CreateNextCardInstanceId(),
            definitionId,
            ownerId,
            zone);

        combat.GetCardZones(ownerId).AddCard(card);

        return card;
    }
}
