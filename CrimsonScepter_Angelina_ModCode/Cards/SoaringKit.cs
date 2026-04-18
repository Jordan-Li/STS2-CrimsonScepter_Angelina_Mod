using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：腾空套组
/// 费用：0
/// 稀有度：普通
/// 卡牌类型：技能
/// 效果：给予自己1层飞行。送达：给予所有敌方1层飞行。消耗。
/// 升级后效果：给予自己2层飞行。送达：给予所有敌方1层飞行。消耗。
/// </summary>
public sealed class SoaringKit : DeliveredCardModel
{
    // 关键字：消耗。
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    // 额外悬浮说明：
    // 1. 消耗
    // 2. 飞行
    // 3. 送达
    protected override IEnumerable<IHoverTip> ExtraHoverTips => WithDeliveredTip(
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<FlyPower>(),
        HoverTipFactory.FromPower<DeliveryPower>()
    );

    // 动态变量：自己获得的飞行层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FlyPower>(1m)
    ];

    // 初始化卡牌的基础信息：0费、技能、普通、目标为自己。
    public SoaringKit()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时，给予自己飞行。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对自己施加飞行层数。
        await PowerCmd.Apply<FlyPower>(
            base.Owner.Creature,
            base.DynamicVars["FlyPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后提高自己获得的飞行层数。
    protected override void OnUpgrade()
    {
        base.DynamicVars["FlyPower"].UpgradeValueBy(1m);
    }

    // 送达时，给予所有敌方1层飞行。
    protected override async Task OnDelivered(DeliveryPower deliveryPower)
    {
        var combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during SoaringKit.OnDelivered.");

        // 遍历所有敌人，逐个施加飞行。
        foreach (Creature enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<FlyPower>(enemy, 1m, base.Owner.Creature, this);
        }
    }
}
