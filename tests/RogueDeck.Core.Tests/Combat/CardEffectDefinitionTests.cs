using RogueDeck.Core.Combat;

namespace RogueDeck.Core.Tests;

public class CardEffectDefinitionTests
{
    [Fact]
    public void StrikeCardDefinesSingleEnemyDamageEffect()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var strike = registry.GetCard(StandardCombatIds.StrikeCard);

        var effect = Assert.Single(strike.Effects);
        var damage = Assert.IsType<DealDamageCardEffectDefinition>(effect);

        Assert.Equal(6, damage.Amount);
        Assert.Equal(CardTargetMode.SingleEnemy, damage.TargetMode);
    }

    [Fact]
    public void DefendCardDefinesSelfBlockEffect()
    {
        var registry = CombatTestFactory.CreateStandardRegistry();

        var defend = registry.GetCard(StandardCombatIds.DefendCard);

        var effect = Assert.Single(defend.Effects);
        var block = Assert.IsType<GainBlockCardEffectDefinition>(effect);

        Assert.Equal(5, block.Amount);
        Assert.Equal(CardTargetMode.Self, block.TargetMode);
    }

    [Fact]
    public void CardDefinitionCanContainMultipleEffectDefinitions()
    {
        var card = new CardDefinition(
            new CardDefinitionId("test.hybrid"),
            new PackageId("test"),
            displayNameKey: "card.test.hybrid.name",
            descriptionKey: "card.test.hybrid.description");

        card.Effects.Add(new DealDamageCardEffectDefinition(3, CardTargetMode.SingleEnemy));
        card.Effects.Add(new GainBlockCardEffectDefinition(2, CardTargetMode.Self));

        Assert.Equal(2, card.Effects.Count);
    }
}
