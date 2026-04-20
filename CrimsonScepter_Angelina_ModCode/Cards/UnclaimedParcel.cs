using BaseLib.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

[Pool(typeof(StatusCardPool))]
public sealed class UnclaimedParcel : DeliveredCardModel
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => WithDeliveredTip(
        HoverTipFactory.FromPower<DeliveryPower>());

    public UnclaimedParcel()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.None)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;
        return Task.CompletedTask;
    }

    protected override async Task OnDelivered(DeliveryPower deliveryPower)
    {
        await CardPileCmd.Add(this, PileType.Exhaust);
        await deliveryPower.EnqueueCard(this);
    }
}
