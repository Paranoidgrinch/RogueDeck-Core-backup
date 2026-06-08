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
        registry.RegisterStatus(CreateStrength());
        registry.RegisterStatus(CreateThorns());
        registry.RegisterStatus(CreateStun());

        registry.RegisterDamageAmountModifier(new StrengthDamageAmountModifier());
        registry.RegisterDamageAmountModifier(new WeakDamageAmountModifier());

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
        registry.RegisterCardEffectDefinitionHandler(new GainResourceCardEffectDefinitionHandler());
        registry.RegisterCardEffectDefinitionHandler(new HealCardEffectDefinitionHandler());

        registry.RegisterEffectRequestHandler(new ApplyStatusEffectHandler());
        registry.RegisterEffectRequestHandler(new RemoveStatusEffectHandler());
        registry.RegisterEffectRequestHandler(new DecreaseStatusDurationEffectHandler());
        registry.RegisterEffectRequestHandler(new DealDamageEffectHandler());
        registry.RegisterEffectRequestHandler(new HealEffectHandler());
        registry.RegisterEffectRequestHandler(new GainBlockEffectHandler());
                registry.RegisterEffectRequestHandler(new DrawCardsEffectHandler());
        registry.RegisterEffectRequestHandler(new DiscardHandEffectHandler());
registry.RegisterEffectRequestHandler(new MoveCardToZoneEffectHandler());
        registry.RegisterEffectRequestHandler(new CreateCardInstanceEffectHandler());
        registry.RegisterEffectRequestHandler(new MoveAllCardsFromZoneEffectHandler());
        registry.RegisterEffectRequestHandler(new RefillResourceEffectHandler());
        registry.RegisterEffectRequestHandler(new GainResourceEffectHandler());
        registry.RegisterEffectRequestHandler(new ClearDefensivePoolEffectHandler());
        registry.RegisterEffectRequestHandler(new SetCombatantLifecycleStateEffectHandler());
        registry.RegisterEffectRequestHandler(new SetCombatResultEffectHandler());        registry.RegisterCombatEventHandler(new RefillResourceOnTurnStartedHandler(StandardCombatIds.EnergyResource, defaultMax: 3));

                registry.RegisterCombatEventHandler(new DrawCardsOnTurnStartedHandler());
registry.RegisterCombatEventHandler(new ClearBlockOnTurnStartedHandler());
                registry.RegisterCombatEventHandler(new DiscardHandOnTurnEndedHandler());
registry.RegisterCombatEventHandler(new DecreaseTimedStatusDurationsOnTurnEndedHandler());
        registry.RegisterCombatEventHandler(new DamageOverTimeOnTurnStartedHandler());
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














