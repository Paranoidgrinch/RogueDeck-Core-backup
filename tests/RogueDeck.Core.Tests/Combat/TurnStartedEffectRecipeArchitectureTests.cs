using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TurnStartedEffectRecipeArchitectureTests
{
    [Fact]
    public void TurnStartedRecipeSupportUsesSharedRunnerAndPreservesTargetSemantics()
    {
        var repoRoot = FindRepositoryRoot();
        var sourcePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "TurnStartedTriggeredEffects.cs");

        var source = File.ReadAllText(sourcePath);
        var handlerStart = source.IndexOf(
            "public sealed class TurnStartedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(handlerStart >= 0);

        var handlerSource = source[handlerStart..];
        var actionsIndex = handlerSource.IndexOf(
            "definition.Actions",
            StringComparison.Ordinal);
        var effectsIndex = handlerSource.IndexOf(
            "definition.Effects",
            StringComparison.Ordinal);

        Assert.Contains(
            "List<ICombatEffectRecipe<TurnStartedTriggeredEffectContext>> Effects",
            source);
        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);
        Assert.Contains(
            "var actionBuildContext =",
            handlerSource);
        Assert.True(actionsIndex >= 0);
        Assert.True(effectsIndex > actionsIndex);
        Assert.DoesNotContain(
            "foreach (var definition in definitions)",
            handlerSource);

        var selectionStart = source.IndexOf(
            "public static CombatantTargetSelectionContext CreateSelectionContext(",
            StringComparison.Ordinal);
        var actionSourceStart = source.IndexOf(
            "public static TriggeredEffectActionSource CreateActionSource(",
            selectionStart,
            StringComparison.Ordinal);

        Assert.True(selectionStart >= 0);
        Assert.True(actionSourceStart > selectionStart);

        var selectionSource = source[
            selectionStart..actionSourceStart];

        Assert.Contains(
            "Source: context.TurnCombatant",
            selectionSource);
        Assert.DoesNotContain(
            "EventTargetId:",
            selectionSource);
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
    public void TurnStartedTriggeredEffectsRemainBeforeStandardTurnStartAutomation()
    {
        var repoRoot = FindRepositoryRoot();
        var packagePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "StandardCombatPackage.cs");

        var source = File.ReadAllText(packagePath);
        var triggeredIndex = source.IndexOf(
            "new TurnStartedTriggeredEffectHandler()",
            StringComparison.Ordinal);
        var refillIndex = source.IndexOf(
            "new RefillResourceOnTurnStartedHandler(",
            StringComparison.Ordinal);
        var drawIndex = source.IndexOf(
            "new DrawCardsOnTurnStartedHandler()",
            StringComparison.Ordinal);
        var clearBlockIndex = source.IndexOf(
            "new ClearBlockOnTurnStartedHandler()",
            StringComparison.Ordinal);
        var damageOverTimeIndex = source.IndexOf(
            "new DamageOverTimeOnTurnStartedHandler()",
            StringComparison.Ordinal);

        Assert.True(triggeredIndex >= 0);
        Assert.True(refillIndex > triggeredIndex);
        Assert.True(drawIndex > triggeredIndex);
        Assert.True(clearBlockIndex > triggeredIndex);
        Assert.True(damageOverTimeIndex > triggeredIndex);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(
            AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                Path.Combine(
                    directory.FullName,
                    "RogueDeck.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find repository root.");
    }
}
