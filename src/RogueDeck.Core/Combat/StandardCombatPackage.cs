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

        registry.RegisterEffectRequestHandler(new ApplyStatusEffectHandler());
        registry.RegisterEffectRequestHandler(new DecreaseStatusDurationEffectHandler());
        registry.RegisterEffectRequestHandler(new DealDamageEffectHandler());

        registry.RegisterEffectRequestHandler(new HealEffectHandler());
        registry.RegisterEffectRequestHandler(new GainBlockEffectHandler());
        registry.RegisterEffectRequestHandler(new ClearDefensivePoolEffectHandler());
        registry.RegisterEffectRequestHandler(new SetCombatantLifecycleStateEffectHandler());
        registry.RegisterEffectRequestHandler(new SetCombatResultEffectHandler());
        registry.RegisterCombatEventHandler(new ClearBlockOnTurnStartedHandler());
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
}






