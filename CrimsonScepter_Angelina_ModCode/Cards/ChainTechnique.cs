using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：施加10点失衡，获得1点临时集中。若此牌使目标进入失重，改为获得1点集中。
/// 升级后效果：施加13点失衡，获得2点临时集中。若此牌使目标进入失重，改为获得2点集中。
/// </summary>
public sealed class ChainTechnique : AngelinaCard
{
    // 维护失衡、临时集中和集中的动态数值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(10m),
        new PowerVar<ChantTemporaryFocusNextTurnPower>(1m),
        new PowerVar<FocusPower>(1m)
    ];

    // 这张牌会用到失衡、失重、集中和临时集中四种悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<WeightlessPower>(),
        HoverTipFactory.FromPower<FocusPower>(),
        HoverTipFactory.FromPower<ChantTemporaryFocusNextTurnPower>()
    ];

    public ChainTechnique()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 这是一张指定敌人的技能牌，没有目标就不应继续结算。
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 先记录目标原本是否处于失重，用来判断这张牌是否成功让其进入失重。
        bool wasWeightless = cardPlay.Target.GetPower<WeightlessPower>() != null;

        // 先对目标施加失衡。
        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            this
        );

        // 若目标因此从非失重变成失重，则直接获得集中并结束，不再给临时集中。
        bool isWeightless = cardPlay.Target.GetPower<WeightlessPower>() != null;
        if (!wasWeightless && isWeightless)
        {
            await PowerCmd.Apply<FocusPower>(
                base.Owner.Creature,
                base.DynamicVars["FocusPower"].BaseValue,
                base.Owner.Creature,
                this
            );
            return;
        }

        // 若没有让目标进入失重，则改为获得临时集中。
        await PowerCmd.Apply<ChantTemporaryFocusNextTurnPower>(
            base.Owner.Creature,
            base.DynamicVars["ChantTemporaryFocusNextTurnPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        // 升级后同时提高失衡、临时集中和集中。
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(3m);
        base.DynamicVars["ChantTemporaryFocusNextTurnPower"].UpgradeValueBy(1m);
        base.DynamicVars["FocusPower"].UpgradeValueBy(1m);
    }
}
