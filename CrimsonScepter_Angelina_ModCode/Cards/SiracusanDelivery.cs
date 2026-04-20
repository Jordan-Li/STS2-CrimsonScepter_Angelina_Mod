using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

public sealed class SiracusanDelivery : AngelinaCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<UnclaimedParcel>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    public SiracusanDelivery()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        CombatState combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during SiracusanDelivery.OnPlay.");

        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

        CardModel unclaimedParcel = combatState.CreateCard<UnclaimedParcel>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(unclaimedParcel, PileType.Discard, addedByPlayer: true));
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1);
    }
}
