using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class TurnEndedEffectRecipeArchitectureTests
{
    [Fact]
    public void TurnEndedRecipeSupportUsesSharedRunnerAndPreservesTargetSemantics()
    {
        var repoRoot = FindRepositoryRoot();
        var sourcePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "Effects",
            "TurnEndedTriggeredEffects.cs");

        var source = File.ReadAllText(sourcePath);
        var handlerStart = source.IndexOf(
            "public sealed class TurnEndedTriggeredEffectHandler",
            StringComparison.Ordinal);

        Assert.True(handlerStart >= 0);

        var handlerSource = source[handlerStart..];
        var stepsIndex = handlerSource.IndexOf(
            "definition.Steps",
            StringComparison.Ordinal);

        Assert.Contains(
            "List<ICombatEffectRecipe<TurnEndedTriggeredEffectContext>> Steps",
            source);
        Assert.Contains(
            "TriggeredEffectDefinitionRunner.EnqueueEffectRequests",
            handlerSource);
        Assert.Contains(
            "var actionBuildContext =",
            handlerSource);
        Assert.True(stepsIndex >= 0);
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
    public void TurnEndedLifecycleOrderingRemainsDiscardThenTriggeredThenDurationDecrease()
    {
        var repoRoot = FindRepositoryRoot();
        var packagePath = Path.Combine(
            repoRoot,
            "src",
            "RogueDeck.Core",
            "Combat",
            "StandardCombatPackage.cs");

        var source = File.ReadAllText(packagePath);
        var discardIndex = source.IndexOf(
            "new DiscardHandOnTurnEndedHandler()",
            StringComparison.Ordinal);
        var triggeredIndex = source.IndexOf(
            "new TurnEndedTriggeredEffectHandler()",
            StringComparison.Ordinal);
        var durationIndex = source.IndexOf(
            "new DecreaseTimedStatusDurationsOnTurnEndedHandler()",
            StringComparison.Ordinal);

        Assert.True(discardIndex >= 0);
        Assert.True(triggeredIndex > discardIndex);
        Assert.True(durationIndex > triggeredIndex);
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
