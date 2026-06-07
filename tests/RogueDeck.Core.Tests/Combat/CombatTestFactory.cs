using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

internal static class CombatTestFactory
{
    public static CombatDefinitionRegistry CreateStandardRegistry()
    {
        var registry = new CombatDefinitionRegistry();

        var package = new StandardCombatPackage();
        package.RegisterDefinitions(registry);

        return registry;
    }
}