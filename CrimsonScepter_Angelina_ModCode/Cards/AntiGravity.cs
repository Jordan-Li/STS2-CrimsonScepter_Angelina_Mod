using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：反重力
/// 费用：2
/// 稀有度：其他
/// 卡牌类型：攻击
/// 效果：施加12点失衡，造成8点法术伤害，使目标获得1层临时飞行。
/// 升级后效果：施加15点失衡，造成12点法术伤害，使目标获得1层临时飞行。
/// 备注：初始卡牌
/// </summary>
public sealed class AntiGravity : AngelinaCard
{
    // 这张牌是法术牌，会参与法术相关结算与联动。
    public override bool IsSpell => true;

    // 额外悬浮提示：
    // 1. 失衡
    // 2. 临时飞行
    // 3. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<TemporaryFlyPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：
    // 1. 失衡值
    // 2. 法术伤害
    // 3. 临时飞行层数
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(12m),
        new DamageVar(8m, ValueProp.Unpowered | ValueProp.Move),
        new PowerVar<TemporaryFlyPower>(1m)
    ];

    // 费用：2费，类型：攻击牌，稀有度：基础，目标：任意敌人
    public AntiGravity()
        : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：施加失衡
        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            this
        );

        // 第二步：造成法术伤害
        await SpellHelper.Damage(
            choiceContext,
            base.Owner.Creature,
            cardPlay.Target,
            SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue),
            this
        );

        // 第三步：给予目标临时飞行
        await PowerCmd.Apply<TemporaryFlyPower>(
            cardPlay.Target,
            base.DynamicVars["TemporaryFlyPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后：失衡值+3，法术伤害+4
    protected override void OnUpgrade()
    {
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(3m);
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        decimal displayedDamage = base.DynamicVars.Damage.BaseValue;
        if (base.IsMutable && base.Owner?.Creature != null)
        {
            displayedDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedDamage);
        }

        description.Add("DisplayedDamage", displayedDamage);
    }
}

