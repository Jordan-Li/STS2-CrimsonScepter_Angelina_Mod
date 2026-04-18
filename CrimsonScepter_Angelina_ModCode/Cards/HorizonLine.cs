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

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：水平线
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：使所有单位获得1层飞行。消耗。
/// 升级后效果：移除消耗。
/// </summary>
public sealed class HorizonLine : AngelinaCard
{
    // 未升级时带有消耗；升级后移除消耗。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? []
        : [
            CardKeyword.Exhaust
        ];

    // 额外悬浮说明：
    // 1. 基础版显示消耗
    // 2. 两个版本都显示飞行
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [
            HoverTipFactory.FromPower<FlyPower>()
        ]
        : [
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
            HoverTipFactory.FromPower<FlyPower>()
        ];

    // 动态变量：给予所有单位的飞行层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FlyPower>(1m)
    ];

    // 初始化卡牌的基础信息：1费、技能、罕见、无目标。
    public HorizonLine()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
    }

    // 打出时，使所有单位获得飞行。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during HorizonLine.OnPlay.");

        // 对战场上的所有单位统一施加飞行。
        await PowerCmd.Apply<FlyPower>(
            combatState.Creatures,
            base.DynamicVars["FlyPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后移除消耗关键词。
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
