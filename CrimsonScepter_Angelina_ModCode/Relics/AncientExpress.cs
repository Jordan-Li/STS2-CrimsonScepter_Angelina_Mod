using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;

/// <summary>
/// 遗物名：先古速递
/// 稀有度：商店
/// 效果：每场战斗开始时，寄送一张随机的先古牌。
/// </summary>
public sealed class AncientExpress : AngelinaRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        CombatState? combatState = base.Owner.Creature.CombatState;
        if (player != base.Owner || combatState == null || combatState.RoundNumber != 1)
        {
            return;
        }

        List<CardModel> ancientCards = ModelDb.AllCards
            .Where(card => card.Rarity == CardRarity.Ancient)
            .DistinctBy(card => card.Id)
            .ToList();

        CardModel? template = base.Owner.RunState.Rng.CombatCardGeneration.NextItem(ancientCards);
        if (template == null)
        {
            return;
        }

        CardModel generatedCard = combatState.CreateCard(template, base.Owner);
        CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(
            generatedCard,
            PileType.Exhaust,
            addedByPlayer: true
        );

        if (!addResult.success)
        {
            return;
        }

        CardModel deliveredCard = addResult.cardAdded;
        deliveredCard.Pile?.InvokeCardAddFinished();

        DeliveryPower? deliveryPower = await PowerCmd.Apply<DeliveryPower>(
            base.Owner.Creature,
            1m,
            base.Owner.Creature,
            null
        );

        if (deliveryPower != null)
        {
            await deliveryPower.SetSelectedCard(deliveredCard);
        }

        Flash();
    }
}