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
    }    [Fact]
    public void CombatTurnProcessorMustNotKnowResourceLifecycleMechanics()
    {
        var repoRoot = FindRepositoryRoot();
        var turnProcessorPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "CombatTurnProcessor.cs");

        var source = File.ReadAllText(turnProcessorPath);

        Assert.DoesNotContain("Resource", source);
        Assert.DoesNotContain("Energy", source);
        Assert.DoesNotContain("ValuePoolState", source);
        Assert.DoesNotContain("RefillResource", source);
        Assert.DoesNotContain("StandardCombatIds.EnergyResource", source);
    }    [Fact]
    public void ApplyStatusEffectHandlerMustNotKnowConcreteInterceptorStatuses()
    {
        var repoRoot = FindRepositoryRoot();
        var statusEffectsPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusEffects.cs");

        var source = File.ReadAllText(statusEffectsPath);

        Assert.DoesNotContain("ArtifactStatus", source);
        Assert.Contains("GetStatusApplicationInterceptors", source);
    }
    [Fact]
    public void CombatCardPlayProcessorUsesRegisteredCardCostModifiers()
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

        Assert.Contains("GetCardCostModifiers", source);
        Assert.DoesNotContain("CostReductionStatus", source);
        Assert.DoesNotContain("FreeCardStatus", source);
    }
    [Fact]
    public void CombatCardPlayProcessorMustNotKnowConcreteControlStatuses()
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

        Assert.DoesNotContain("StunStatus", source);
        Assert.DoesNotContain("ControlTag", source);
        Assert.Contains("GetCardPlayValidators", source);
    }    [Fact]
    public void GainBlockEffectHandlerMustNotKnowConcreteBlockModifierStatuses()
    {
        var repoRoot = FindRepositoryRoot();
        var defensivePoolEffectsPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "DefensivePoolEffects.cs");

        var source = File.ReadAllText(defensivePoolEffectsPath);

        Assert.DoesNotContain("DexterityStatus", source);
        Assert.DoesNotContain("FrailStatus", source);
        Assert.DoesNotContain("BlockModifierTag", source);
        Assert.Contains("GetBlockAmountModifiers", source);
    }

    [Fact]
    public void DealDamageEffectHandlerMustNotKnowConcreteDamageModifierStatuses()
    {
        var repoRoot = FindRepositoryRoot();
        var damageEffectsPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "DamageEffects.cs");

        var source = File.ReadAllText(damageEffectsPath);

        Assert.DoesNotContain("StrengthStatus", source);
        Assert.DoesNotContain("WeakStatus", source);
        Assert.DoesNotContain("DamageModifierTag", source);
        Assert.Contains("GetDamageAmountModifiers", source);
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

    [Fact]
    public void CombatEffectRecipeContractsMustNotKnowConcreteEventsOrEffects()
    {
        var repoRoot = FindRepositoryRoot();
        var contractsPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "CombatEffectRecipeContracts.cs");

        var source = File.ReadAllText(contractsPath);

        Assert.Contains("ICombatValueProvider", source);
        Assert.Contains("ICombatEffectRecipe", source);
        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("EffectKind", source);
        Assert.DoesNotContain("RoundStarted", source);
        Assert.DoesNotContain("GainBlockEffectRequest", source);
        Assert.DoesNotContain("HealEffectRequest", source);
        Assert.DoesNotContain("DealDamageEffectRequest", source);
    }

    [Fact]
    public void GainBlockEffectRecipeMustRemainEventAgnosticAndUseSharedBuilder()
    {
        var repoRoot = FindRepositoryRoot();
        var recipePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "GainBlockEffectRecipe.cs");

        var source = File.ReadAllText(recipePath);

        Assert.Contains(
            "TriggeredEffectActionBuilder.BuildGainBlockRequests",
            source);
        Assert.DoesNotContain(
            "TriggeredEffectActionRequestFactory",
            source);
        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);
        Assert.DoesNotContain("RoundStarted", source);
        Assert.DoesNotContain("EffectKind", source);
        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RoundStartedRecipePilotMustRemainGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();
        var roundStartedPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "RoundStartedTriggeredEffects.cs");

        var source = File.ReadAllText(roundStartedPath);
        var actionsIndex = source.LastIndexOf(
            "definition.Actions",
            StringComparison.Ordinal);
        var effectsIndex = source.LastIndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.Contains(
            "List<ICombatEffectRecipe<RoundStartedTriggeredEffectContext>> Effects",
            source);
        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);
        Assert.Contains(
            "RoundStartedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            source);
        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);
        Assert.DoesNotContain("EffectKind", source);
        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HealEffectRecipeMustRemainEventAgnosticAndUseSharedBuilder()
    {
        var repoRoot = FindRepositoryRoot();
        var recipePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "HealEffectRecipe.cs");

        var source = File.ReadAllText(recipePath);

        Assert.Contains(
            "TriggeredEffectActionBuilder.BuildHealRequests",
            source);
        Assert.DoesNotContain(
            "TriggeredEffectActionRequestFactory",
            source);
        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);
        Assert.DoesNotContain("RoundEnded", source);
        Assert.DoesNotContain("TurnStarted", source);
        Assert.DoesNotContain("TurnEnded", source);
        Assert.DoesNotContain("HealedCombatEvent", source);
        Assert.DoesNotContain("EffectKind", source);
        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RoundEndedRecipePilotMustRemainGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();
        var roundEndedPath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "RoundEndedTriggeredEffects.cs");

        var source = File.ReadAllText(roundEndedPath);
        var actionsIndex = source.LastIndexOf(
            "definition.Actions",
            StringComparison.Ordinal);
        var effectsIndex = source.LastIndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.Contains(
            "List<ICombatEffectRecipe<RoundEndedTriggeredEffectContext>> Effects",
            source);
        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);
        Assert.Contains(
            "var actionBuildContext =",
            source);
        Assert.Contains(
            "RoundEndedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            source);
        Assert.Contains(
            "effect => effect.BuildEffectRequests(",
            source);
        Assert.Contains(
            "combatEvent.LastActiveCombatantId is null",
            source);
        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);
        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);
        Assert.DoesNotContain("EffectKind", source);
        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CardCostPaidRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "CardCostPaidTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var handlerStart = source.IndexOf(
            "public sealed class CardCostPaidTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(handlerStart >= 0);

        var handler = source[handlerStart..];

        var actionsIndex = handler.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handler.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.Contains(
            "List<ICombatEffectRecipe<CardCostPaidTriggeredEffectContext>> Effects",
            source);

        Assert.Contains(
            "ICombatValueProvider<CardCostPaidTriggeredEffectContext, int>",
            source);

        Assert.Contains(
            "CardCostPaidTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handler);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handler);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handler);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);
    }
    [Fact]
    public void DamageDealtRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "DamageDealtTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var definitionSource = Slice(
            source,
            "public sealed class DamageDealtTriggeredEffectDefinition",
            "public sealed record DamageDealtTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                source,
                "public sealed record FixedDamageDealtTriggeredEffectAmount",
                "public sealed class DamageDealtHealthDamageAmount"),
            Slice(
                source,
                "public sealed class DamageDealtHealthDamageAmount",
                "public sealed class DamageDealtBlockedDamageAmount"),
            Slice(
                source,
                "public sealed class DamageDealtBlockedDamageAmount",
                "public sealed class DamageDealtRequestedAmount"),
            Slice(
                source,
                "public sealed class DamageDealtRequestedAmount",
                "public sealed record DamageDealtSourceStatusStacksAmount"),
            Slice(
                source,
                "public sealed record DamageDealtSourceStatusStacksAmount",
                "public sealed record DamageDealtTargetStatusStacksAmount"),
            Slice(
                source,
                "public sealed record DamageDealtTargetStatusStacksAmount",
                "public sealed class DamageDealtHasSourceTriggerFilter")
        };

        var handlerStart = source.IndexOf(
            "public sealed class DamageDealtTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing DamageDealtTriggeredEffectHandler.");

        var handlerSource = source[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<DamageDealtTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<DamageDealtTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(DamageDealtTriggeredEffectContext context)",
                providerSource);
        }

        Assert.Contains(
            "DamageDealtTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);

        Assert.DoesNotContain(
            "DamageReceivedTriggeredEffectDefinition",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);

        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }
    [Fact]
    public void DamageReceivedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "DamageReceivedTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var definitionSource = Slice(
            source,
            "public sealed class DamageReceivedTriggeredEffectDefinition",
            "public sealed record DamageReceivedTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                source,
                "public sealed record FixedDamageReceivedTriggeredEffectAmount",
                "public sealed class DamageReceivedHealthDamageAmount"),
            Slice(
                source,
                "public sealed class DamageReceivedHealthDamageAmount",
                "public sealed class DamageReceivedBlockedDamageAmount"),
            Slice(
                source,
                "public sealed class DamageReceivedBlockedDamageAmount",
                "public sealed class DamageReceivedRequestedAmount"),
            Slice(
                source,
                "public sealed class DamageReceivedRequestedAmount",
                "public sealed record DamageReceivedReceiverStatusStacksAmount"),
            Slice(
                source,
                "public sealed record DamageReceivedReceiverStatusStacksAmount",
                "public sealed record DamageReceivedSourceStatusStacksAmount"),
            Slice(
                source,
                "public sealed record DamageReceivedSourceStatusStacksAmount",
                "public sealed class DamageReceivedHasSourceTriggerFilter")
        };

        var handlerStart = source.IndexOf(
            "public sealed class DamageReceivedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing DamageReceivedTriggeredEffectHandler.");

        var handlerSource = source[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<DamageReceivedTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<DamageReceivedTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(DamageReceivedTriggeredEffectContext context)",
                providerSource);
        }

        Assert.Contains(
            "DamageReceivedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);

        Assert.DoesNotContain(
            "DamageDealtTriggeredEffectDefinition",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);

        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }
    [Fact]
    public void StatusAppliedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusAppliedTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var definitionSource = Slice(
            source,
            "public sealed class StatusAppliedTriggeredEffectDefinition",
            "public sealed record StatusAppliedTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                source,
                "public sealed record FixedStatusAppliedTriggeredEffectAmount",
                "public sealed class StatusAppliedStacksAmount"),
            Slice(
                source,
                "public sealed class StatusAppliedStacksAmount",
                "public sealed class StatusAppliedDurationTurnsAmount"),
            Slice(
                source,
                "public sealed class StatusAppliedDurationTurnsAmount",
                "public sealed class StatusAppliedChargesAmount"),
            Slice(
                source,
                "public sealed class StatusAppliedChargesAmount",
                "public sealed record StatusAppliedTargetStatusStacksAmount"),
            Slice(
                source,
                "public sealed record StatusAppliedTargetStatusStacksAmount",
                "public sealed record StatusAppliedSourceStatusStacksAmount"),
            Slice(
                source,
                "public sealed record StatusAppliedSourceStatusStacksAmount",
                "public sealed class StatusAppliedHasSourceTriggerFilter")
        };

        var handlerStart = source.IndexOf(
            "public sealed class StatusAppliedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing StatusAppliedTriggeredEffectHandler.");

        var handlerSource = source[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<StatusAppliedTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<StatusAppliedTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(StatusAppliedTriggeredEffectContext context)",
                providerSource);
        }

        Assert.Contains(
            "StatusAppliedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);

        Assert.DoesNotContain(
            "StatusMergedTriggeredEffectDefinition",
            source);

        Assert.DoesNotContain(
            "StatusApplicationBlockedTriggeredEffectDefinition",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);

        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }
    [Fact]
    public void StatusMergedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();
        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusLifecycleTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var familyStart = source.IndexOf(
            "public sealed class StatusMergedTriggeredEffectDefinition",
            StringComparison.Ordinal);

        Assert.True(
            familyStart >= 0,
            "Missing StatusMergedTriggeredEffectDefinition.");

        var familySource = source[familyStart..];

        var definitionSource = Slice(
            familySource,
            "public sealed class StatusMergedTriggeredEffectDefinition",
            "public sealed record StatusMergedTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                familySource,
                "public sealed record FixedStatusMergedTriggeredEffectAmount",
                "public sealed class StatusMergedStacksAmount"),
            Slice(
                familySource,
                "public sealed class StatusMergedStacksAmount",
                "public sealed class StatusMergedDurationTurnsAmount"),
            Slice(
                familySource,
                "public sealed class StatusMergedDurationTurnsAmount",
                "public sealed class StatusMergedChargesAmount"),
            Slice(
                familySource,
                "public sealed class StatusMergedChargesAmount",
                "public sealed record StatusMergedTargetStatusStacksAmount"),
            Slice(
                familySource,
                "public sealed record StatusMergedTargetStatusStacksAmount",
                "public sealed record StatusMergedSourceStatusStacksAmount"),
            Slice(
                familySource,
                "public sealed record StatusMergedSourceStatusStacksAmount",
                "public sealed class StatusMergedHasSourceTriggerFilter")
        };

        var handlerStart = familySource.IndexOf(
            "public sealed class StatusMergedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing StatusMergedTriggeredEffectHandler.");

        var handlerSource = familySource[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<StatusMergedTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<StatusMergedTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(StatusMergedTriggeredEffectContext context)",
                providerSource);
        }

        Assert.Contains(
            "StatusMergedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "EffectKind",
            familySource);

        Assert.False(
            familySource.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }
    [Fact]
    public void StatusApplicationBlockedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusApplicationBlockedTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var definitionSource = Slice(
            source,
            "public sealed class StatusApplicationBlockedTriggeredEffectDefinition",
            "public sealed record StatusApplicationBlockedTriggeredEffectContext");

        var providerSource = Slice(
            source,
            "public sealed record FixedStatusApplicationBlockedTriggeredEffectAmount",
            "public sealed record StatusApplicationBlockedBlockedStatusTriggerFilter");

        var handlerStart = source.IndexOf(
            "public sealed class StatusApplicationBlockedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing StatusApplicationBlockedTriggeredEffectHandler.");

        var handlerSource = source[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<StatusApplicationBlockedTriggeredEffectContext>> Effects",
            definitionSource);

        Assert.Contains(
            "ICombatValueProvider<StatusApplicationBlockedTriggeredEffectContext, int>",
            providerSource);

        Assert.Contains(
            "public int Resolve(StatusApplicationBlockedTriggeredEffectContext context)",
            providerSource);

        Assert.Contains(
            "return ResolveAmount(context);",
            providerSource);

        Assert.Contains(
            "StatusApplicationBlockedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);

        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }
    [Fact]
    public void StatusChargesReducedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusLifecycleTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var familySource = Slice(
            source,
            "public sealed class StatusChargesReducedTriggeredEffectDefinition",
            "public sealed class StatusExpiredTriggeredEffectDefinition");

        var definitionSource = Slice(
            familySource,
            "public sealed class StatusChargesReducedTriggeredEffectDefinition",
            "public sealed record StatusChargesReducedTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                familySource,
                "public sealed record FixedStatusChargesReducedTriggeredEffectAmount",
                "public sealed class StatusChargesReducedChargeCountAmount"),
            Slice(
                familySource,
                "public sealed class StatusChargesReducedChargeCountAmount",
                "public sealed record StatusChargesReducedStatusDefinitionTriggerFilter")
        };

        var handlerStart = familySource.IndexOf(
            "public sealed class StatusChargesReducedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing StatusChargesReducedTriggeredEffectHandler.");

        var handlerSource = familySource[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<StatusChargesReducedTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<StatusChargesReducedTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(StatusChargesReducedTriggeredEffectContext context)",
                providerSource);

            Assert.Contains(
                "return ResolveAmount(context);",
                providerSource);
        }

        Assert.Contains(
            "StatusChargesReducedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "EffectKind",
            familySource);

        Assert.False(
            familySource.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }

    [Fact]
    public void StatusesRemovedByPolarityRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusesRemovedByPolarityTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var definitionSource = Slice(
            source,
            "public sealed class StatusesRemovedByPolarityTriggeredEffectDefinition",
            "public sealed record StatusesRemovedByPolarityTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                source,
                "public sealed record FixedStatusesRemovedByPolarityTriggeredEffectAmount",
                "public sealed class StatusesRemovedByPolarityRemovedStatusCount"),
            Slice(
                source,
                "public sealed class StatusesRemovedByPolarityRemovedStatusCount",
                "public sealed record StatusesRemovedByPolarityTriggerFilter")
        };

        var handlerStart = source.IndexOf(
            "public sealed class StatusesRemovedByPolarityTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing StatusesRemovedByPolarityTriggeredEffectHandler.");

        var handlerSource = source[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<StatusesRemovedByPolarityTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<StatusesRemovedByPolarityTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(StatusesRemovedByPolarityTriggeredEffectContext context)",
                providerSource);

            Assert.Contains(
                "return ResolveAmount(context);",
                providerSource);
        }

        Assert.Contains(
            "StatusesRemovedByPolarityTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            source);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            source);

        Assert.DoesNotContain(
            "EffectKind",
            source);

        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }

    [Fact]
    public void StatusExpiredRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusLifecycleTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var familySource = Slice(
            source,
            "public sealed class StatusExpiredTriggeredEffectDefinition",
            "public sealed class StatusMergedTriggeredEffectDefinition");

        var definitionSource = Slice(
            familySource,
            "public sealed class StatusExpiredTriggeredEffectDefinition",
            "public sealed record StatusExpiredTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                familySource,
                "public sealed record FixedStatusExpiredTriggeredEffectAmount",
                "public sealed record StatusExpiredTargetStatusStacksAmount"),
            Slice(
                familySource,
                "public sealed record StatusExpiredTargetStatusStacksAmount",
                "public sealed record StatusExpiredSourceStatusStacksAmount"),
            Slice(
                familySource,
                "public sealed record StatusExpiredSourceStatusStacksAmount",
                "public sealed class StatusExpiredHasSourceTriggerFilter")
        };

        var handlerStart = familySource.IndexOf(
            "public sealed class StatusExpiredTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing StatusExpiredTriggeredEffectHandler.");

        var handlerSource = familySource[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<StatusExpiredTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<StatusExpiredTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(StatusExpiredTriggeredEffectContext context)",
                providerSource);

            Assert.Contains(
                "return ResolveAmount(context);",
                providerSource);
        }

        Assert.Contains(
            "StatusExpiredTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "EffectKind",
            familySource);

        Assert.False(
            familySource.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }

    [Fact]
    public void StatusRemovedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "StatusLifecycleTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var familySource = Slice(
            source,
            "public sealed class StatusRemovedTriggeredEffectDefinition",
            "public sealed class StatusChargesReducedTriggeredEffectDefinition");

        var definitionSource = Slice(
            familySource,
            "public sealed class StatusRemovedTriggeredEffectDefinition",
            "public sealed record StatusRemovedTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                familySource,
                "public sealed record FixedStatusRemovedTriggeredEffectAmount",
                "public sealed class StatusRemovedCountAmount"),
            Slice(
                familySource,
                "public sealed class StatusRemovedCountAmount",
                "public sealed record StatusRemovedTargetStatusStacksAmount"),
            Slice(
                familySource,
                "public sealed record StatusRemovedTargetStatusStacksAmount",
                "public sealed record StatusRemovedSourceStatusStacksAmount"),
            Slice(
                familySource,
                "public sealed record StatusRemovedSourceStatusStacksAmount",
                "public sealed class StatusRemovedHasSourceTriggerFilter")
        };

        var handlerStart = familySource.IndexOf(
            "public sealed class StatusRemovedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing StatusRemovedTriggeredEffectHandler.");

        var handlerSource = familySource[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<StatusRemovedTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<StatusRemovedTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(StatusRemovedTriggeredEffectContext context)",
                providerSource);

            Assert.Contains(
                "return ResolveAmount(context);",
                providerSource);
        }

        Assert.Contains(
            "StatusRemovedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "new GainBlockEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "new HealEffectRequest",
            familySource);

        Assert.DoesNotContain(
            "EffectKind",
            familySource);

        Assert.False(
            familySource.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
    }

    [Fact]
    public void CardInstanceCreatedRecipeMigrationRemainsGenericAndOrdered()
    {
        var repoRoot = FindRepositoryRoot();

        var path = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "CardInstanceCreatedTriggeredEffects.cs");

        var source = File.ReadAllText(path);

        var definitionSource = Slice(
            source,
            "public sealed class CardInstanceCreatedTriggeredEffectDefinition",
            "public sealed record CardInstanceCreatedTriggeredEffectContext");

        var providerSources = new[]
        {
            Slice(
                source,
                "public sealed record FixedCardInstanceCreatedTriggeredEffectAmount",
                "public sealed class CardInstanceCreatedCount"),
            Slice(
                source,
                "public sealed class CardInstanceCreatedCount",
                "public sealed record CardInstanceCreatedCardDefinitionTriggerFilter")
        };

        var handlerStart = source.IndexOf(
            "public sealed class CardInstanceCreatedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(
            handlerStart >= 0,
            "Missing CardInstanceCreatedTriggeredEffectHandler.");

        var handlerSource = source[handlerStart..];

        Assert.Contains(
            "List<ICombatEffectRecipe<CardInstanceCreatedTriggeredEffectContext>> Effects",
            definitionSource);

        foreach (var providerSource in providerSources)
        {
            Assert.Contains(
                "ICombatValueProvider<CardInstanceCreatedTriggeredEffectContext, int>",
                providerSource);

            Assert.Contains(
                "public int Resolve(CardInstanceCreatedTriggeredEffectContext context)",
                providerSource);

            Assert.Contains(
                "return ResolveAmount(context);",
                providerSource);
        }

        Assert.Contains(
            "CardInstanceCreatedTriggeredEffectTargetResolver.CreateActionBuildContext(context)",
            handlerSource);

        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);

        Assert.Contains(
            "effect.BuildEffectRequests(",
            handlerSource);

        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);

        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);

        Assert.DoesNotContain(
            "combat.EnqueueEffect",
            handlerSource);

        Assert.DoesNotContain(
            "EffectRequest(",
            handlerSource);

        var forbiddenConcreteRequests = new[]
        {
            "new GainBlockEffectRequest",
            "new HealEffectRequest",
            "new DealDamageEffectRequest",
            "new ApplyStatusEffectRequest",
            "new DrawCardsEffectRequest",
            "new CreateCardInstanceEffectRequest"
        };

        foreach (var forbiddenConcreteRequest in forbiddenConcreteRequests)
        {
            Assert.DoesNotContain(
                forbiddenConcreteRequest,
                handlerSource);
        }

        Assert.DoesNotContain(
            "EffectKind",
            source);

        Assert.False(
            source.Contains(
                "switch",
                StringComparison.OrdinalIgnoreCase));

        static string Slice(
            string text,
            string startMarker,
            string endMarker)
        {
            var start = text.IndexOf(
                startMarker,
                StringComparison.Ordinal);

            Assert.True(
                start >= 0,
                $"Missing start marker: {startMarker}");

            var end = text.IndexOf(
                endMarker,
                start,
                StringComparison.Ordinal);

            Assert.True(
                end > start,
                $"Missing end marker: {endMarker}");

            return text[start..end];
        }
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








