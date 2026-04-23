using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Enchantments;

public sealed class ExpressEnchantment : AngelinaEnchantment
{
    public override bool HasExtraCardText => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        _ = cardPlay;

        CardModel sourceCard = base.Card;
        List<CardModel> handCards = PileType.Hand
            .GetPile(sourceCard.Owner)
            .Cards
            .ToList();

        if (handCards.Count == 0)
        {
            return;
        }

        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: sourceCard.Owner,
            prefs: new CardSelectorPrefs(ResolveEnchantLoc("sendPrompt"), 1),
            filter: null,
            source: sourceCard)).FirstOrDefault();

        if (selectedCard == null)
        {
            return;
        }

        DeliveryPower? deliveryPower = sourceCard.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(
            sourceCard.Owner.Creature,
            1m,
            sourceCard.Owner.Creature,
            sourceCard);

        await CardCmd.Exhaust(choiceContext, selectedCard);

        if (deliveryPower != null)
        {
            await deliveryPower.EnqueueCard(selectedCard);
        }
    }
}
