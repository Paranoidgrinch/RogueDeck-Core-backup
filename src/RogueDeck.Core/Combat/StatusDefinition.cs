namespace RogueDeck.Core.Combat;

public sealed class StatusDefinition
{
    public StatusDefinitionId Id { get; }
    public PackageId PackageId { get; }

    public string DisplayNameKey { get; }
    public string DescriptionKey { get; }

    public StatusVisibility DefaultVisibility { get; }
    public StatusPolarity Polarity { get; }

    public bool UsesStacks { get; }
    public bool UsesDuration { get; }
    public bool UsesCharges { get; }

    public bool ShowStacksInUi { get; }
    public bool ShowDurationInUi { get; }
    public bool ShowChargesInUi { get; }

    public HashSet<TagId> Tags { get; } = new();

    public StatusDefinition(
        StatusDefinitionId id,
        PackageId packageId,
        string displayNameKey,
        string descriptionKey,
        StatusVisibility defaultVisibility = StatusVisibility.Visible,
        StatusPolarity polarity = StatusPolarity.Neutral,
        bool usesStacks = false,
        bool usesDuration = false,
        bool usesCharges = false,
        bool showStacksInUi = false,
        bool showDurationInUi = false,
        bool showChargesInUi = false)
    {
        if (string.IsNullOrWhiteSpace(displayNameKey))
            throw new ArgumentException("Display name key cannot be empty.", nameof(displayNameKey));

        if (string.IsNullOrWhiteSpace(descriptionKey))
            throw new ArgumentException("Description key cannot be empty.", nameof(descriptionKey));

        Id = id;
        PackageId = packageId;
        DisplayNameKey = displayNameKey;
        DescriptionKey = descriptionKey;
        DefaultVisibility = defaultVisibility;
        Polarity = polarity;
        UsesStacks = usesStacks;
        UsesDuration = usesDuration;
        UsesCharges = usesCharges;
        ShowStacksInUi = showStacksInUi;
        ShowDurationInUi = showDurationInUi;
        ShowChargesInUi = showChargesInUi;
    }
}