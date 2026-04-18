using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：能量循环
/// 效果：每回合中，打出耗能至少为2的攻击、技能或能力牌时，每种类型至多各触发若干次：回复1点能量。
/// </summary>
public sealed class EnergyCyclePower : AngelinaPower
{
    private sealed class Data
    {
        public Dictionary<CardType, int> TriggerCountsByType { get; } = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override int DisplayAmount => base.Amount;

    // 额外悬浮说明：能量图标。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this)
    ];

    // 动态变量：每次触发时回复的能量。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 每当自己打出牌后，检查是否满足高耗能回能条件。
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 只统计自己打出的牌。
        if (cardPlay.Card.Owner != base.Owner.Player)
        {
            return;
        }

        // 只允许攻击、技能、能力三种类型触发。
        CardType type = cardPlay.Card.Type;
        if (type != CardType.Attack && type != CardType.Skill && type != CardType.Power)
        {
            return;
        }

        // 只有耗能至少为2的牌才会触发。
        if (cardPlay.Resources.EnergyValue < 2)
        {
            return;
        }

        // 每种类型每回合最多触发 base.Amount 次。
        Data data = GetInternalData<Data>();
        data.TriggerCountsByType.TryGetValue(type, out int triggerCount);
        if (triggerCount >= base.Amount)
        {
            return;
        }

        data.TriggerCountsByType[type] = triggerCount + 1;
        Flash();
        await PlayerCmd.GainEnergy(1m, base.Owner.Player);
    }

    // 在自身这一侧回合结束时，清空本回合各牌型的触发计数。
    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            GetInternalData<Data>().TriggerCountsByType.Clear();
        }

        return Task.CompletedTask;
    }
}
