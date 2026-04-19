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
/// 费用：3
/// 稀有度：稀有
/// 卡牌类型：技能
/// 效果：在这张牌被打出8次后，每场战斗仅限1次，打出此牌时获得1个随机遗物并消耗。送达：升级需要次数减1，本场战斗中消耗能量减少2。
/// 升级后效果：保留。
/// </summary>
public sealed class ImportantCommission : DeliveredCardModel
{
    private const int CompletionThreshold = 8;

    private int progressCount;
    private bool isCompleted;
    // 只显示寄送提示；奖励锁应当只在真正获得对应 Power 后由 Power 自己显示。
    protected override IEnumerable<IHoverTip> ExtraHoverTips => WithDeliveredTip(
        HoverTipFactory.FromPower<DeliveryPower>());

    // Count 显示离完成还差几次；Energy 表示送达后的本场战斗减费。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Count", CompletionThreshold),
        new EnergyVar(2)
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

    [SavedProperty]
    public bool IsCompleted
    {
        get => isCompleted;
        set
        {
            AssertMutable();
            isCompleted = value;

            if (isCompleted && progressCount < CompletionThreshold)
            {
                progressCount = CompletionThreshold;
            }

            RefreshProgressDisplay();
        }
    }

    public ImportantCommission()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 升级后获得保留。
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    // 打出时：
    // 1. 未完成则推进任务进度
    // 2. 完成后转为奖励牌，每场战斗首次打出获得随机遗物
    // 3. 成功领奖的这次打出会改为进入 Exhaust
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;

        ImportantCommission persistentCard = GetPersistentCard();
        SyncFromPersistent(persistentCard);

        if (!persistentCard.IsCompleted)
        {
            persistentCard.ProgressCount++;

            if (persistentCard.ProgressCount >= CompletionThreshold)
            {
                persistentCard.IsCompleted = true;
            }

            SyncFromPersistent(persistentCard);
            // 第 8 次任务这张牌只负责“完成任务”，不会立刻领奖。
            return;
        }

        await TryGrantRewardThisCombat();
    }

    // 送达时：
    // 1. 本场战斗耗能 -2
    // 2. 若仍未完成，则额外推进 1 次任务进度
    protected override async Task OnDelivered(DeliveryPower deliveryPower)
    {
        _ = deliveryPower;

        base.EnergyCost.AddThisCombat(-2, reduceOnly: true);
        base.InvokeEnergyCostChanged();

        ImportantCommission persistentCard = GetPersistentCard();
        SyncFromPersistent(persistentCard);

        if (persistentCard.IsCompleted)
        {
            return;
        }

        persistentCard.ProgressCount++;
        if (persistentCard.ProgressCount >= CompletionThreshold)
        {
            persistentCard.IsCompleted = true;
        }

        SyncFromPersistent(persistentCard);
    }

    // 只有成功获得遗物的那次打出才改为消耗。
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card == this && ShouldExhaustForRewardThisPlay())
        {
            return (PileType.Exhaust, CardPilePosition.Bottom);
        }

        return base.ModifyCardPlayResultPileTypeAndPosition(card, isAutoPlay, resources, pileType, position);
    }

    // 完成态下，每场战斗只在第一次领奖时给一个随机遗物并上锁。
    private async Task TryGrantRewardThisCombat()
    {
        if (base.Owner.Creature.HasPower<ImportantCommissionRewardLockPower>())
        {
            return;
        }

        var relic = RelicFactory.PullNextRelicFromFront(base.Owner).ToMutable();
        await RelicCmd.Obtain(relic, base.Owner);
        await PowerCmd.Apply<ImportantCommissionRewardLockPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);
    }

    // 战斗中的复制体通过 DeckVersion 指回牌库中的持久版本。
    private ImportantCommission GetPersistentCard()
    {
        return base.DeckVersion as ImportantCommission ?? this;
    }

    // 把牌库持久版本的累计进度同步到当前战斗中的这张牌。
    private void SyncFromPersistent(ImportantCommission persistentCard)
    {
        if (ReferenceEquals(persistentCard, this))
        {
            return;
        }

        ProgressCount = persistentCard.ProgressCount;
        IsCompleted = persistentCard.IsCompleted;
    }

    // 根据当前状态刷新“还差几次完成”显示。
    private void RefreshProgressDisplay()
    {
        base.DynamicVars["Count"].BaseValue = IsCompleted
            ? 0
            : Math.Max(CompletionThreshold - progressCount, 0);
    }

    // 已完成后的战斗内首次打出会领奖，并且这次打出应当直接进入 Exhaust。
    private bool ShouldExhaustForRewardThisPlay()
    {
        ImportantCommission persistentCard = GetPersistentCard();

        if (!persistentCard.IsCompleted)
        {
            return false;
        }

        return !base.Owner.Creature.HasPower<ImportantCommissionRewardLockPower>();
    }
}
