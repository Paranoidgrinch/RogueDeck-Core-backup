namespace RogueDeck.Core.Combat;

public static class StandardCombatIds
{
    public static readonly DefensivePoolId BlockDefensivePool = new("standard.block");

    public static readonly TeamId PlayerTeam = new("player");

    public static readonly TeamId EnemyTeam = new("enemy");

    public static readonly StatusDefinitionId PoisonStatus = new("standard.poison");

    public static readonly StatusDefinitionId WeakStatus = new("standard.weak");

    public static readonly StatusDefinitionId StrengthStatus = new("standard.strength");

    public static readonly StatusDefinitionId ThornsStatus = new("standard.thorns");

    public static readonly StatusDefinitionId StunStatus = new("standard.stun");

    public static readonly TagId BuffTag = new("buff");

    public static readonly TagId DebuffTag = new("debuff");

    public static readonly TagId DamageOverTimeTag = new("damage_over_time");

    public static readonly TagId DamageModifierTag = new("damage_modifier");

    public static readonly TagId TriggeredDamageTag = new("triggered_damage");

    public static readonly TagId ControlTag = new("control");
}
