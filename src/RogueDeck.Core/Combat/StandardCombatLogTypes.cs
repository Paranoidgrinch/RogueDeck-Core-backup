namespace RogueDeck.Core.Combat;

public static class StandardCombatLogTypes
{
    public const string TurnStarted = "TurnStarted";
    public const string TurnEnded = "TurnEnded";
    public const string RoundStarted = "RoundStarted";
    public const string RoundEnded = "RoundEnded";

        public const string ResourceRefilled = "ResourceRefilled";
public const string CardPlayed = "CardPlayed";
    public const string CardsDrawn = "CardsDrawn";
    public const string DiscardPileShuffledIntoDrawPile = "DiscardPileShuffledIntoDrawPile";
    public const string HandDiscarded = "HandDiscarded";
    public const string CardMovedToZone = "CardMovedToZone";
    public const string CardsMovedBetweenZones = "CardsMovedBetweenZones";
    public const string CardInstanceCreated = "CardInstanceCreated";

    public const string DamageDealt = "DamageDealt";
    public const string Healed = "Healed";

    public const string BlockGained = "BlockGained";
    public const string DefensivePoolCleared = "DefensivePoolCleared";

    public const string StatusApplied = "StatusApplied";
    public const string StatusMerged = "StatusMerged";
    public const string StatusDurationReduced = "StatusDurationReduced";
    public const string StatusExpired = "StatusExpired";

    public const string CombatantLifecycleChanged = "CombatantLifecycleChanged";
    public const string CombatResultChanged = "CombatResultChanged";
}



