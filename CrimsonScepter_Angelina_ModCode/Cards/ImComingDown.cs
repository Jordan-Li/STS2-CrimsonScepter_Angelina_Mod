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
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：我下来啦！
/// 费用：1
/// 稀有度：稀有
/// 卡牌类型：攻击
/// 效果：失去所有飞行。每失去1层飞行，对所有敌人造成20点伤害。
/// 升级后效果：失去所有飞行。每失去1层飞行，对所有敌人造成25点伤害。
/// </summary>
public sealed class ImComingDown : AngelinaCard
{
    // 额外悬浮说明：飞行。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FlyPower>()
    ];

    // 动态变量：每层飞行转化出的单段群体伤害。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Move)
    ];

    // 初始化卡牌的基础信息：1费、攻击、稀有、目标为全体敌人。
    public ImComingDown()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    // 打出时，先失去所有飞行，再按失去的飞行层数对所有敌人重复造成伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先记录当前实际拥有的飞行层数，后续伤害次数按这里计算。
        FlyPower? flyPower = base.Owner.Creature.GetPower<FlyPower>();
        int lostFly = flyPower?.Amount ?? 0;

        // “失去所有飞行”不仅要移除飞行本身，也要清掉仍在追踪中的临时飞行。
        TemporaryFlyPower? temporaryFlyPower = base.Owner.Creature.GetPower<TemporaryFlyPower>();

        if (flyPower != null)
        {
            await PowerCmd.Remove(flyPower);
        }

        if (temporaryFlyPower != null)
        {
            await PowerCmd.Remove(temporaryFlyPower);
        }

        // 若失去的实际飞行层数为0，则本次不造成伤害。
        if (lostFly <= 0)
        {
            return;
        }

        // 读取战斗状态，用于对全体敌人重复结算攻击。
        var combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during ImComingDown.OnPlay.");

        // 每失去1层飞行，就对所有敌人造成一次群体伤害。
        for (int i = 0; i < lostFly; i++)
        {
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(combatState)
                .Execute(choiceContext);
        }
    }

    // 升级后将每段伤害从20提高到25。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
