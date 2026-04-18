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
/// 卡牌名：惯性
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：能力
/// 效果：当有敌方单位失重或回合开始时处于失重时，你抽2张牌。
/// 升级后效果：当有敌方单位失重或回合开始时处于失重时，你抽3张牌。
/// </summary>
public sealed class Inertia : AngelinaCard
{
    // 额外悬浮说明：
    // 1. 惯性
    // 2. 失重
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InertiaPower>(),
        HoverTipFactory.FromPower<WeightlessPower>()
    ];

    // 动态变量：惯性提供的额外抽牌数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<InertiaPower>(2m)
    ];

    // 初始化卡牌的基础信息：1费、能力、罕见、目标为自己。
    public Inertia()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，施加一个持续能力 Power：
    // 当敌方进入失重，或回合开始时已有敌方失重时，额外抽牌。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先播放施法动作，再挂上对应的持续 Power。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<InertiaPower>(
            base.Owner.Creature,
            base.DynamicVars["InertiaPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后额外抽牌数 +1。
    protected override void OnUpgrade()
    {
        base.DynamicVars["InertiaPower"].UpgradeValueBy(1m);
    }
}