using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：1
/// 稀有度：其他
/// 卡牌类型：能力
/// 效果：每当你打出一张攻击牌时，若其目标没有浮空，则给予其1层飞行。每打出一张牌，处于浮空状态的敌人受到2点伤害。
/// 升级后效果：固有。
/// </summary>
public sealed class SpecialGravityModule : AngelinaCard
{
    // 维护给予的飞行层数和能力层数两个动态数值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FlyPower>(1m),
        new PowerVar<SpecialGravityModulePower>(1m)
    ];

    // 升级后获得固有；关键字会由游戏自动追加到卡牌显示中。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [CardKeyword.Innate] : [];

    // 这张牌会用到固有、飞行、浮空和对应能力的悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
            HoverTipFactory.FromPower<SpecialGravityModulePower>(),
            HoverTipFactory.FromPower<FlyPower>(),
            new HoverTip(
                new LocString("powers", "AIRBORNE.title"),
                new LocString("powers", "AIRBORNE.description"))
        ]
        : [
            HoverTipFactory.FromPower<SpecialGravityModulePower>(),
            HoverTipFactory.FromPower<FlyPower>(),
            new HoverTip(
                new LocString("powers", "AIRBORNE.title"),
                new LocString("powers", "AIRBORNE.description"))
        ];

    // 费用 1，能力牌，稀有度为其他，自身目标。
    public SpecialGravityModule()
        : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    // 打出时获得特限重力模块能力。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<SpecialGravityModulePower>(
            base.Owner.Creature,
            base.DynamicVars["SpecialGravityModulePower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后获得固有。
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
