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
        registry.RegisterEffectRequestHandler(new DealDamageEffectHandler());

        registry.RegisterEffectRequestHandler(new HealEffectHandler());
    }

    private StatusDefinition CreatePoison()
    {
        var definition = new StatusDefinition(
            new StatusDefinitionId("standard.poison"),
            Id,
            displayNameKey: "status.standard.poison.name",
            descriptionKey: "status.standard.poison.description",
            polarity: StatusPolarity.Debuff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(new TagId("debuff"));
        definition.Tags.Add(new TagId("damage_over_time"));

        return definition;
    }

    private StatusDefinition CreateWeak()
    {
        var definition = new StatusDefinition(
            new StatusDefinitionId("standard.weak"),
            Id,
            displayNameKey: "status.standard.weak.name",
            descriptionKey: "status.standard.weak.description",
            polarity: StatusPolarity.Debuff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(new TagId("debuff"));
        definition.Tags.Add(new TagId("damage_modifier"));

        return definition;
    }

    private StatusDefinition CreateStrength()
    {
        var definition = new StatusDefinition(
            new StatusDefinitionId("standard.strength"),
            Id,
            displayNameKey: "status.standard.strength.name",
            descriptionKey: "status.standard.strength.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(new TagId("buff"));
        definition.Tags.Add(new TagId("damage_modifier"));

        return definition;
    }

    private StatusDefinition CreateThorns()
    {
        var definition = new StatusDefinition(
            new StatusDefinitionId("standard.thorns"),
            Id,
            displayNameKey: "status.standard.thorns.name",
            descriptionKey: "status.standard.thorns.description",
            polarity: StatusPolarity.Buff,
            usesStacks: true,
            showStacksInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(new TagId("buff"));
        definition.Tags.Add(new TagId("triggered_damage"));

        return definition;
    }

    private StatusDefinition CreateStun()
    {
        var definition = new StatusDefinition(
            new StatusDefinitionId("standard.stun"),
            Id,
            displayNameKey: "status.standard.stun.name",
            descriptionKey: "status.standard.stun.description",
            polarity: StatusPolarity.Debuff,
            usesDuration: true,
            showDurationInUi: true,
            stackingBehavior: StatusStackingBehavior.MergeWithExistingInstance);

        definition.Tags.Add(new TagId("debuff"));
        definition.Tags.Add(new TagId("control"));

        return definition;
    }
}