namespace RogueDeck.Core.Tests;

public class CombatArchitectureGuardTests
{
    [Fact]
    public void CombatEffectResolverMustNotKnowConcreteEffectRequests()
    {
        var repoRoot = FindRepositoryRoot();
        var resolverPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "CombatEffectResolver.cs");

        var source = File.ReadAllText(resolverPath);

        Assert.DoesNotContain("switch", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("case ", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApplyStatusEffectRequest", source);
        Assert.DoesNotContain("DealDamageEffectRequest", source);
        Assert.DoesNotContain("HealEffectRequest", source);
        Assert.DoesNotContain("GainBlockEffectRequest", source);
        Assert.DoesNotContain("ClearDefensivePoolEffectRequest", source);
        Assert.DoesNotContain("SetCombatantLifecycleStateEffectRequest", source);
        Assert.DoesNotContain("SetCombatResultEffectRequest", source);
    }

    [Fact]
    public void CombatTurnProcessorMustNotKnowConcreteStandardStatusMechanics()
    {
        var repoRoot = FindRepositoryRoot();
        var turnProcessorPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "CombatTurnProcessor.cs");

        var source = File.ReadAllText(turnProcessorPath);

        Assert.DoesNotContain("Poison", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Weak", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Thorns", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stun", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DamageOverTime", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TriggeredDamage", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BlockDefensivePool", source, StringComparison.OrdinalIgnoreCase);
    }    [Fact]
    public void CombatCardPlayProcessorMustNotKnowConcreteCardEffectDefinitions()
    {
        var repoRoot = FindRepositoryRoot();
        var cardPlayPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Cards",
            "CardPlay.cs");

        var source = File.ReadAllText(cardPlayPath);

        Assert.DoesNotContain("DealDamageCardEffectDefinition", source);
        Assert.DoesNotContain("GainBlockCardEffectDefinition", source);
        Assert.DoesNotContain("Unsupported card effect definition type", source);
        Assert.DoesNotContain("switch", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetCardEffectDefinitionHandler", source);
    }    [Fact]
    public void CombatTurnProcessorMustNotKnowCardZoneLifecycleMechanics()
    {
        var repoRoot = FindRepositoryRoot();
        var turnProcessorPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "CombatTurnProcessor.cs");

        var source = File.ReadAllText(turnProcessorPath);

        Assert.DoesNotContain("DrawCardsEffectRequest", source);
        Assert.DoesNotContain("DiscardHandEffectRequest", source);
        Assert.DoesNotContain("DrawCards", source);
        Assert.DoesNotContain("DiscardHand", source);
        Assert.DoesNotContain("CardZone", source);
        Assert.DoesNotContain("BanishedPile", source);
        Assert.DoesNotContain("ExhaustPile", source);
    }



    [Fact]
    public void StandardCombatPackageIsAllowedToRegisterStandardHandlers()
    {
        var repoRoot = FindRepositoryRoot();
        var packagePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "StandardCombatPackage.cs");

        var source = File.ReadAllText(packagePath);

        Assert.Contains("RegisterStatus", source);
        Assert.Contains("RegisterEffectRequestHandler", source);
        Assert.Contains("RegisterCombatEventHandler", source);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var srcCombatPath = Path.Combine(
                directory.FullName,
                "src",
                "RogueDeck.Core",
                "Combat");

            var testCombatPath = Path.Combine(
                directory.FullName,
                "tests",
                "RogueDeck.Core.Tests",
                "Combat");

            if (Directory.Exists(srcCombatPath) && Directory.Exists(testCombatPath))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}


