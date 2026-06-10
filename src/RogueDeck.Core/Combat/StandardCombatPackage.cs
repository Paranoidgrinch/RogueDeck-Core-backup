namespace RogueDeck.Core.Combat;

public sealed class StandardCombatPackage : ICombatPackage
{
    public PackageId Id { get; } = new("standard");

    public string DisplayName => "Standard Combat";

    public IReadOnlyCollection<PackageId> Dependencies => Array.Empty<PackageId>();

    public void RegisterDefinitions(CombatDefinitionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.RegisterStatus(CreatePoison());
        registry.RegisterStatus(CreateWeak());
        registry.RegisterStatus(CreateVulnerable());
        registry.RegisterStatus(CreateFrail());
        registry.RegisterStatus(CreateArtifact());
        registry.RegisterStatus(CreateStrength());
        registry.RegisterStatus(CreateRage());
        registry.RegisterTriggeredEffectDefinition(CreateRageBlockOnAttackPlayedTriggeredEffect());
        registry.RegisterStatus(CreateDexterity());
        registry.RegisterStatus(CreateThorns());
        registry.RegisterStatus(CreateStun());
        registry.RegisterStatus(CreateOneAttackPerTurn());
        registry.RegisterStatus(CreateFreeNextCard());
        registry.RegisterStatus(CreateFirstAttackEachTurnFree());
        registry.RegisterStatus(CreateSkillComboDraw());
        registry.RegisterTriggeredEffectDefinition(CreateSkillComboDrawTriggeredEffect());
        registry.RegisterStatus(CreateSkillCostReduction());

        registry.RegisterDamageAmountModifier(new StrengthDamageAmountModifier());
        registry.RegisterDamageAmountModifier(new WeakDamageAmountModifier());
        registry.RegisterDamageAmountModifier(new VulnerableDamageAmountModifier());

        registry.RegisterBlockAmountModifier(new DexterityBlockAmountModifier());
        registry.RegisterBlockAmountModifier(new FrailBlockAmountModifier());

        registry.RegisterCardPlayValidator(new StunCardPlayValidator());
        registry.RegisterCardPlayValidator(new OneAttackPerTurnCardPlayValidator());

        registry.RegisterCardCostModifier(new FreeNextCardCostModifier());
        registry.RegisterCardCostModifier(new FirstAttackEachTurnFreeCostModifier());
        registry.RegisterCardCostModifier(new SkillCostReductionCostModifier());

        registry.RegisterStatusApplicationInterceptor(new ArtifactStatusApplicationInterceptor());

        registry.RegisterCard(CreateStrike());
        registry.RegisterCard(CreateDefend());
        registry.RegisterCardEffectDefinitionHandler(new DealDamageCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new GainBlockCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new DrawCardsCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new DiscardHandCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new CreateCardInstanceCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new MoveAllCardsFromZoneCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new ApplyStatusCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new RemoveStatusCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new RemoveStatusesByPolarityCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new GainResourceCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new HealCardEffectDefinitionHandler());

        registry.RegisterEffectRequestHandler(new ApplyStatusEffectHandler());
        registry.RegisterEffectRequestHandler(new RemoveStatusEffectHandler());
        registry.RegisterEffectRequestHandler(new RemoveStatusesByPolarityEffectHandler());
        registry.RegisterEffectRequestHandler(new DecreaseStatusDurationEffectHandler());
        registry.RegisterEffectRequestHandler(new DecreaseStatusChargesEffectHandler());
        registry.RegisterEffectRequestHandler(new DealDamageEffectHandler());
        registry.RegisterEffectRequestHandler(new HealEffectHandler());
        registry.RegisterEffectRequestHandler(new GainBlockEffectHandler());
                registry.RegisterEffectRequestHandler(new DrawCardsEffectHandler());
        registry.RegisterEffectRequestHandler(new DiscardHandEffectHandler());
        registry.RegisterEffectRequestHandler(new MoveHandCardsOnTurnEndEffectHandler());
registry.RegisterEffectRequestHandler(new MoveCardToZoneEffectHandler());
        registry.RegisterEffectRequestHandler(new CreateCardInstanceEffectHandler());
        registry.RegisterEffectRequestHandler(new MoveAllCardsFromZoneEffectHandler());
        registry.RegisterEffectRequestHandler(new RefillResourceEffectHandler());
        registry.RegisterEffectRequestHandler(new GainResourceEffectHandler());
        registry.RegisterEffectRequestHandler(new ClearDefensivePoolEffectHandler());
        registry.RegisterEffectRequestHandler(new SetCombatantLifecycleStateEffectHandler());
        registry.RegisterEffectRequestHandler(new SetCombatResultEffectHandler());                registry.RegisterCombatEventHandler(new ResetCardPlayTurnStatsOnTurnStartedHandler());
registry.RegisterCombatEventHandler(new RefillResourceOnTurnStartedHandler(StandardCombatIds.EnergyResource, defaultMax: 3));

                registry.RegisterCombatEventHandler(new DrawCardsOnTurnStartedHandler());
registry.RegisterCombatEventHandler(new ClearBlockOnTurnStartedHandler());
                registry.RegisterCombatEventHandler(new DiscardHandOnTurnEndedHandler());
registry.RegisterCombatEventHandler(new DecreaseTimedStatusDurationsOnTurnEndedHandler());
        registry.RegisterCombatEventHandler(new DamageOverTimeOnTurnStartedHandler());
                registry.RegisterCombatEventHandler(new TrackCardsPlayedThisTurnHandler());
        registry.RegisterCombatEventHandler(new CardPlayedTriggeredEffectHandler());
registry.RegisterCombatEventHandler(new ConsumeFreeNextCardOnCardPlayedHandler());
        registry.RegisterCombatEventHandler(new TriggeredDamageOnDamageDealtHandler());
        registry.RegisterCombatEventHandler(new MarkCombatantDownedOnZeroHealthHandler());
        registry.RegisterCombatEventHandler(new UpdateStandardCombatResultOnLifecycleChangedHandler());
    }

    private StatusDefinition CreatePoison()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.PoisonStatus,
            Id,
            displayNameKey: "status.standard.poison.name",
            descriptionKey: "status.standard.poison.description",
            polarity: StatusPolarity.Debuff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.DebuffTag);
        definition.Tags.Add(StandardCombatIds.DamageOverTimeTag);

        return definition;
    }

    private StatusDefinition CreateWeak()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.WeakStatus,
            Id,
            displayNameKey: "status.standard.weak.name",
            descriptionKey: "status.standard.weak.description",
            polarity: StatusPolarity.Debuff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.DebuffTag);
        definition.Tags.Add(StandardCombatIds.DamageModifierTag);

        return definition;
    }
    private StatusDefinition CreateArtifact()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.ArtifactStatus,
            Id,
            displayNameKey: "status.standard.artifact.name",
            descriptionKey: "status.standard.artifact.description",
            polarity: StatusPolarity.Buff,
            usesCharges: true,
            showChargesInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.StatusApplicationInterceptorTag);

        return definition;
    }
    private StatusDefinition CreateFrail()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.FrailStatus,
            Id,
            displayNameKey: "status.standard.frail.name",
            descriptionKey: "status.standard.frail.description",
            polarity: StatusPolarity.Debuff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.DebuffTag);
        definition.Tags.Add(StandardCombatIds.BlockModifierTag);

        return definition;
    }
    private StatusDefinition CreateVulnerable()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.VulnerableStatus,
            Id,
            displayNameKey: "status.standard.vulnerable.name",
            descriptionKey: "status.standard.vulnerable.description",
            polarity: StatusPolarity.Debuff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.DebuffTag);
        definition.Tags.Add(StandardCombatIds.DamageModifierTag);

        return definition;
    }
    private StatusDefinition CreateDexterity()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.DexterityStatus,
            Id,
            displayNameKey: "status.standard.dexterity.name",
            descriptionKey: "status.standard.dexterity.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.BlockModifierTag);

        return definition;
    }
    private StatusDefinition CreateRage()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.RageStatus,
            Id,
            displayNameKey: "status.standard.rage.name",
            descriptionKey: "status.standard.rage.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.CardPlayedTriggerTag);

        return definition;
    }

    private CardPlayedTriggeredEffectDefinition CreateSkillComboDrawTriggeredEffect()
    {
        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("standard.skill_combo_draw"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.SkillCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(StandardCombatIds.SkillComboDrawStatus));

        definition.Filters.Add(
            new CardPlayedEveryNthCardWithTagThisTurnFilter(
                StandardCombatIds.SkillCardTag,
                interval: 3));

        definition.Actions.Add(
            new DrawCardsOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(StandardCombatIds.SkillComboDrawStatus)));

        return definition;
    }
    private CardPlayedTriggeredEffectDefinition CreateRageBlockOnAttackPlayedTriggeredEffect()
    {
        var definition = new CardPlayedTriggeredEffectDefinition(
            new TriggeredEffectDefinitionId("standard.rage_block_on_attack_played"));

        definition.Filters.Add(
            new CardPlayedCardHasTagTriggerFilter(StandardCombatIds.AttackCardTag));

        definition.Filters.Add(
            new CardPlayedSourceHasStatusTriggerFilter(StandardCombatIds.RageStatus));

        definition.Actions.Add(
            new GainBlockOnCardPlayedTriggeredEffectAction(
                CardPlayedTriggeredEffectTarget.Source,
                new CardPlayedSourceStatusStacksAmount(StandardCombatIds.RageStatus)));

        return definition;
    }
    private StatusDefinition CreateStrength()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.StrengthStatus,
            Id,
            displayNameKey: "status.standard.strength.name",
            descriptionKey: "status.standard.strength.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.DamageModifierTag);

        return definition;
    }

    private StatusDefinition CreateThorns()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.ThornsStatus,
            Id,
            displayNameKey: "status.standard.thorns.name",
            descriptionKey: "status.standard.thorns.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.TriggeredDamageTag);

        return definition;
    }
    private StatusDefinition CreateOneAttackPerTurn()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.OneAttackPerTurnStatus,
            Id,
            displayNameKey: "status.standard.one_attack_per_turn.name",
            descriptionKey: "status.standard.one_attack_per_turn.description",
            polarity: StatusPolarity.Debuff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.DebuffTag);
        definition.Tags.Add(StandardCombatIds.ControlTag);
        definition.Tags.Add(StandardCombatIds.PlayLimitTag);

        return definition;
    }

    private StatusDefinition CreateStun()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.StunStatus,
            Id,
            displayNameKey: "status.standard.stun.name",
            descriptionKey: "status.standard.stun.description",
            polarity: StatusPolarity.Debuff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.DebuffTag);
        definition.Tags.Add(StandardCombatIds.ControlTag);

        return definition;
    }
    private StatusDefinition CreateSkillComboDraw()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.SkillComboDrawStatus,
            Id,
            displayNameKey: "status.standard.skill_combo_draw.name",
            descriptionKey: "status.standard.skill_combo_draw.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.ComboTag);

        return definition;
    }
    private StatusDefinition CreateFirstAttackEachTurnFree()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.FirstAttackEachTurnFreeStatus,
            Id,
            displayNameKey: "status.standard.first_attack_each_turn_free.name",
            descriptionKey: "status.standard.first_attack_each_turn_free.description",
            polarity: StatusPolarity.Buff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.CostModifierTag);

        return definition;
    }
    private StatusDefinition CreateSkillCostReduction()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.SkillCostReductionStatus,
            Id,
            displayNameKey: "status.standard.skill_cost_reduction.name",
            descriptionKey: "status.standard.skill_cost_reduction.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            usesDuration: true,
            showStacksInUi: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.CostModifierTag);

        return definition;
    }
    private StatusDefinition CreateFreeNextCard()
    {
        var definition = new StatusDefinition(
            StandardCombatIds.FreeNextCardStatus,
            Id,
            displayNameKey: "status.standard.free_next_card.name",
            descriptionKey: "status.standard.free_next_card.description",
            polarity: StatusPolarity.Buff,
            usesCharges: true,
            showChargesInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(StandardCombatIds.BuffTag);
        definition.Tags.Add(StandardCombatIds.CostModifierTag);

        return definition;
    }

    private CardDefinition CreateStrike()
    {
        var definition = new CardDefinition(
            StandardCombatIds.StrikeCard,
            Id,
            displayNameKey: "card.standard.strike.name",
            descriptionKey: "card.standard.strike.description");

        definition.Costs.Add(new ResourceCost(StandardCombatIds.EnergyResource, 1));
        definition.Tags.Add(StandardCombatIds.AttackCardTag);
        definition.Effects.Add(new DealDamageCardEffectDefinition(6, CardTargetMode.SingleEnemy));

        return definition;
    }

    private CardDefinition CreateDefend()
    {
        var definition = new CardDefinition(
            StandardCombatIds.DefendCard,
            Id,
            displayNameKey: "card.standard.defend.name",
            descriptionKey: "card.standard.defend.description");

        definition.Costs.Add(new ResourceCost(StandardCombatIds.EnergyResource, 1));
        definition.Tags.Add(StandardCombatIds.SkillCardTag);
        definition.Effects.Add(new GainBlockCardEffectDefinition(5, CardTargetMode.Self));

        return definition;
    }
}































