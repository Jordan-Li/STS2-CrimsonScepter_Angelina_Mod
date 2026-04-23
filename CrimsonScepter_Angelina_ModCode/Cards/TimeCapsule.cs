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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

public sealed class TimeCapsule : AngelinaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>(),
        HoverTipFactory.FromPower<TimeCapsulePower>()
    ];

    public TimeCapsule()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> handCards = PileType.Hand.GetPile(base.Owner).Cards.ToList();
        int selectCount = Math.Min((int)base.DynamicVars.Cards.BaseValue, handCards.Count);
        if (selectCount == 0)
        {
            return;
        }

        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "TIME_CAPSULE.selectPrompt"), selectCount),
            filter: null,
            source: this)).ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

        TimeCapsulePower? timeCapsulePower = base.Owner.Creature.GetPower<TimeCapsulePower>();
        timeCapsulePower ??= await PowerCmd.Apply<TimeCapsulePower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

        foreach (CardModel selectedCard in selectedCards)
        {
            if (timeCapsulePower != null)
            {
                await timeCapsulePower.TrackCard(selectedCard);
            }

            await CardCmd.Exhaust(choiceContext, selectedCard);

            if (deliveryPower != null)
            {
                await deliveryPower.EnqueueCard(selectedCard);
            }
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
