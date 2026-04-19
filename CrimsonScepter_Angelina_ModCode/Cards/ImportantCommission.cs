using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：1
/// 稀有度：稀有
/// 卡牌类型：技能
/// 效果：打出或送达6次后，获得1个随机遗物，然后将次数重置为6。每场战斗限一次。
/// 升级后效果：保留。
/// </summary>
public sealed class ImportantCommission : DeliveredCardModel
{
    private const int CompletionThreshold = 6;

    private int progressCount;
    private bool shouldExhaustOnResolve;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => WithDeliveredTip(
        HoverTipFactory.FromPower<DeliveryPower>());

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Count", CompletionThreshold)
    ];

    [SavedProperty]
    public int ProgressCount
    {
        get => progressCount;
        set
        {
            AssertMutable();
            progressCount = Math.Clamp(value, 0, CompletionThreshold);
            RefreshProgressDisplay();
        }
    }

    public ImportantCommission()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;

        await AdvanceProgressAndGrantRewardIfNeeded(choiceContext);
    }

    protected override async Task OnDelivered(DeliveryPower deliveryPower)
    {
        _ = deliveryPower;

        await AdvanceProgressAndGrantRewardIfNeeded(null);
    }

    private async Task AdvanceProgressAndGrantRewardIfNeeded(PlayerChoiceContext? choiceContext)
    {
        ImportantCommission persistentCard = GetPersistentCard();
        bool hasRewardLock = base.Owner.Creature.HasPower<ImportantCommissionRewardLockPower>();

        persistentCard.ProgressCount++;

        if (persistentCard.ProgressCount >= CompletionThreshold && !hasRewardLock)
        {
            persistentCard.ProgressCount = 0;
            var relic = RelicFactory.PullNextRelicFromFront(base.Owner).ToMutable();
            await RelicCmd.Obtain(relic, base.Owner);
            if (persistentCard.Pile?.Type == PileType.Deck)
            {
                await CardPileCmd.RemoveFromDeck(persistentCard);
            }
            ImportantCommission card = base.Owner.RunState.CreateCard<ImportantCommission>(base.Owner);
            if (persistentCard.IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
            await PowerCmd.Apply<ImportantCommissionRewardLockPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);
            shouldExhaustOnResolve = true;

            if (choiceContext != null && Pile?.Type != PileType.Exhaust)
            {
                await CardCmd.Exhaust(choiceContext, this);
            }
            else if (Pile?.Type == PileType.Hand)
            {
                await CardPileCmd.Add(this, PileType.Exhaust);
            }
        }
        else if (persistentCard.ProgressCount >= CompletionThreshold)
        {
            persistentCard.ProgressCount = CompletionThreshold;
        }

        SyncFromPersistent(persistentCard);
    }

    private ImportantCommission GetPersistentCard()
    {
        return base.DeckVersion as ImportantCommission ?? this;
    }

    private void SyncFromPersistent(ImportantCommission persistentCard)
    {
        if (ReferenceEquals(persistentCard, this))
        {
            RefreshProgressDisplay();
            return;
        }

        ProgressCount = persistentCard.ProgressCount;
    }

    private void RefreshProgressDisplay()
    {
        base.DynamicVars["Count"].BaseValue = Math.Max(CompletionThreshold - progressCount, 0);
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card == this && shouldExhaustOnResolve)
        {
            shouldExhaustOnResolve = false;
            return (PileType.Exhaust, CardPilePosition.Bottom);
        }

        return base.ModifyCardPlayResultPileTypeAndPosition(card, isAutoPlay, resources, pileType, position);
    }
}
