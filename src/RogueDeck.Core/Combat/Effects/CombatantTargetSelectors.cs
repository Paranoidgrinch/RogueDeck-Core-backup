namespace RogueDeck.Core.Combat;

public sealed record CombatantTargetSelectionContext(
    CombatState Combat,
    CombatantState Source,
    CombatantId? EventTargetId = null);

public interface ICombatantTargetSelector
{
    IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context);
}

public sealed class SourceCombatantTargetSelector : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Source.IsAlive
            ? new[] { context.Source.Id }
            : Array.Empty<CombatantId>();
    }
}

public sealed class EventTargetCombatantTargetSelector : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.EventTargetId is null)
            return Array.Empty<CombatantId>();

        var targetId = context.EventTargetId.Value;

        if (!context.Combat.TryGetCombatant(targetId, out var target))
            return Array.Empty<CombatantId>();

        return target!.IsAlive
            ? new[] { targetId }
            : Array.Empty<CombatantId>();
    }
}

public sealed class AllAlliesOfSourceCombatantTargetSelector : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Combat.Combatants
            .Where(combatant =>
                combatant.TeamId == context.Source.TeamId &&
                combatant.IsAlive)
            .Select(combatant => combatant.Id)
            .ToArray();
    }
}

public sealed class AllEnemiesOfSourceCombatantTargetSelector : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Combat.Combatants
            .Where(combatant =>
                combatant.TeamId != context.Source.TeamId &&
                combatant.IsAlive)
            .Select(combatant => combatant.Id)
            .ToArray();
    }
}

public sealed record AllCombatantsTargetSelector(bool AliveOnly = true) : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var combatants = context.Combat.Combatants.AsEnumerable();

        if (AliveOnly)
            combatants = combatants.Where(combatant => combatant.IsAlive);

        return combatants
            .Select(combatant => combatant.Id)
            .ToArray();
    }
}

public sealed class LowestHealthEnemyOfSourceCombatantTargetSelector : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Combat.Combatants
            .Where(combatant =>
                combatant.TeamId != context.Source.TeamId &&
                combatant.IsAlive)
            .OrderBy(combatant => combatant.Health.Current)
            .ThenBy(combatant => combatant.Id.ToString(), StringComparer.Ordinal)
            .Select(combatant => combatant.Id)
            .Take(1)
            .ToArray();
    }
}

public sealed class HighestHealthEnemyOfSourceCombatantTargetSelector : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Combat.Combatants
            .Where(combatant =>
                combatant.TeamId != context.Source.TeamId &&
                combatant.IsAlive)
            .OrderByDescending(combatant => combatant.Health.Current)
            .ThenBy(combatant => combatant.Id.ToString(), StringComparer.Ordinal)
            .Select(combatant => combatant.Id)
            .Take(1)
            .ToArray();
    }
}

public sealed class AllDamagedAlliesOfSourceCombatantTargetSelector : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Combat.Combatants
            .Where(combatant =>
                combatant.TeamId == context.Source.TeamId &&
                combatant.IsAlive &&
                combatant.Health.Current < combatant.Health.Max)
            .Select(combatant => combatant.Id)
            .ToArray();
    }
}

public sealed record AllAlliesOfSourceWithStatusCombatantTargetSelector(StatusDefinitionId StatusDefinitionId)
    : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Combat.Combatants
            .Where(combatant =>
                combatant.TeamId == context.Source.TeamId &&
                combatant.IsAlive &&
                combatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId))
            .Select(combatant => combatant.Id)
            .ToArray();
    }
}

public sealed record AllEnemiesOfSourceWithStatusCombatantTargetSelector(StatusDefinitionId StatusDefinitionId)
    : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Combat.Combatants
            .Where(combatant =>
                combatant.TeamId != context.Source.TeamId &&
                combatant.IsAlive &&
                combatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId))
            .Select(combatant => combatant.Id)
            .ToArray();
    }
}
public sealed record DamagedCombatantsTargetSelector(ICombatantTargetSelector Inner)
    : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(Inner);

        return Inner.ResolveTargets(context)
            .Select(targetId => context.Combat.TryGetCombatant(targetId, out var combatant)
                ? combatant
                : null)
            .Where(combatant =>
                combatant is not null &&
                combatant.IsAlive &&
                combatant.Health.Current < combatant.Health.Max)
            .Select(combatant => combatant!.Id)
            .ToArray();
    }
}

public sealed record CombatantsWithStatusTargetSelector(
    ICombatantTargetSelector Inner,
    StatusDefinitionId StatusDefinitionId)
    : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(Inner);

        return Inner.ResolveTargets(context)
            .Select(targetId => context.Combat.TryGetCombatant(targetId, out var combatant)
                ? combatant
                : null)
            .Where(combatant =>
                combatant is not null &&
                combatant.IsAlive &&
                combatant.Statuses.Any(status => status.DefinitionId == StatusDefinitionId))
            .Select(combatant => combatant!.Id)
            .ToArray();
    }
}

public sealed record LowestHealthCombatantTargetSelector(ICombatantTargetSelector Inner)
    : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(Inner);

        return Inner.ResolveTargets(context)
            .Select(targetId => context.Combat.TryGetCombatant(targetId, out var combatant)
                ? combatant
                : null)
            .Where(combatant => combatant is not null && combatant.IsAlive)
            .OrderBy(combatant => combatant!.Health.Current)
            .ThenBy(combatant => combatant!.Id.ToString(), StringComparer.Ordinal)
            .Select(combatant => combatant!.Id)
            .Take(1)
            .ToArray();
    }
}

public sealed record HighestHealthCombatantTargetSelector(ICombatantTargetSelector Inner)
    : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(Inner);

        return Inner.ResolveTargets(context)
            .Select(targetId => context.Combat.TryGetCombatant(targetId, out var combatant)
                ? combatant
                : null)
            .Where(combatant => combatant is not null && combatant.IsAlive)
            .OrderByDescending(combatant => combatant!.Health.Current)
            .ThenBy(combatant => combatant!.Id.ToString(), StringComparer.Ordinal)
            .Select(combatant => combatant!.Id)
            .Take(1)
            .ToArray();
    }
}

public sealed class UnionCombatantTargetSelector : ICombatantTargetSelector
{
    private readonly IReadOnlyCollection<ICombatantTargetSelector> _selectors;

    public UnionCombatantTargetSelector(params ICombatantTargetSelector[] selectors)
    {
        ArgumentNullException.ThrowIfNull(selectors);

        if (selectors.Length == 0)
            throw new ArgumentException("At least one selector is required.", nameof(selectors));

        if (selectors.Any(selector => selector is null))
            throw new ArgumentException("Selectors cannot contain null.", nameof(selectors));

        _selectors = selectors.ToArray();
    }

    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var seen = new HashSet<CombatantId>();
        var result = new List<CombatantId>();

        foreach (var selector in _selectors)
        {
            foreach (var targetId in selector.ResolveTargets(context))
            {
                if (seen.Add(targetId))
                    result.Add(targetId);
            }
        }

        return result;
    }
}

public sealed record ExceptCombatantTargetSelector(
    ICombatantTargetSelector Include,
    ICombatantTargetSelector Exclude)
    : ICombatantTargetSelector
{
    public IReadOnlyCollection<CombatantId> ResolveTargets(CombatantTargetSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(Include);
        ArgumentNullException.ThrowIfNull(Exclude);

        var excluded = Exclude.ResolveTargets(context).ToHashSet();

        return Include.ResolveTargets(context)
            .Where(targetId => !excluded.Contains(targetId))
            .ToArray();
    }
}
public static class CombatantTargetSelectors
{
    public static ICombatantTargetSelector Source { get; } =
        new SourceCombatantTargetSelector();

    public static ICombatantTargetSelector EventTarget { get; } =
        new EventTargetCombatantTargetSelector();

    public static ICombatantTargetSelector AllAlliesOfSource { get; } =
        new AllAlliesOfSourceCombatantTargetSelector();

    public static ICombatantTargetSelector AllEnemiesOfSource { get; } =
        new AllEnemiesOfSourceCombatantTargetSelector();

    public static ICombatantTargetSelector AllAliveCombatants { get; } =
        new AllCombatantsTargetSelector(AliveOnly: true);

    public static ICombatantTargetSelector AllCombatants { get; } =
        new AllCombatantsTargetSelector(AliveOnly: false);
    public static ICombatantTargetSelector LowestHealthEnemyOfSource { get; } =
        new LowestHealthEnemyOfSourceCombatantTargetSelector();

    public static ICombatantTargetSelector HighestHealthEnemyOfSource { get; } =
        new HighestHealthEnemyOfSourceCombatantTargetSelector();

    public static ICombatantTargetSelector AllDamagedAlliesOfSource { get; } =
        new AllDamagedAlliesOfSourceCombatantTargetSelector();

    public static ICombatantTargetSelector AllAlliesOfSourceWithStatus(
        StatusDefinitionId statusDefinitionId)
    {
        return new AllAlliesOfSourceWithStatusCombatantTargetSelector(statusDefinitionId);
    }

    public static ICombatantTargetSelector AllEnemiesOfSourceWithStatus(
        StatusDefinitionId statusDefinitionId)
    {
        return new AllEnemiesOfSourceWithStatusCombatantTargetSelector(statusDefinitionId);
    }
    public static ICombatantTargetSelector Damaged(ICombatantTargetSelector inner)
    {
        return new DamagedCombatantsTargetSelector(inner);
    }

    public static ICombatantTargetSelector WithStatus(
        ICombatantTargetSelector inner,
        StatusDefinitionId statusDefinitionId)
    {
        return new CombatantsWithStatusTargetSelector(inner, statusDefinitionId);
    }

    public static ICombatantTargetSelector LowestHealth(ICombatantTargetSelector inner)
    {
        return new LowestHealthCombatantTargetSelector(inner);
    }

    public static ICombatantTargetSelector HighestHealth(ICombatantTargetSelector inner)
    {
        return new HighestHealthCombatantTargetSelector(inner);
    }

    public static ICombatantTargetSelector Union(params ICombatantTargetSelector[] selectors)
    {
        return new UnionCombatantTargetSelector(selectors);
    }

    public static ICombatantTargetSelector Except(
        ICombatantTargetSelector include,
        ICombatantTargetSelector exclude)
    {
        return new ExceptCombatantTargetSelector(include, exclude);
    }
}




