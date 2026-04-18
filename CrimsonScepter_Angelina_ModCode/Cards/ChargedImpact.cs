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
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：蓄能冲击
/// 费用：3
/// 稀有度：罕见
/// 卡牌类型：攻击
/// 效果：施加20点失衡。造成18点法术伤害。送达：随机本场战斗此牌消耗的能量。
/// 升级后效果：施加28点失衡。造成25点法术伤害。送达：随机本场战斗此牌消耗的能量。
/// </summary>
public sealed class ChargedImpact : DeliveredCardModel
{
    // 额外悬浮说明：
    // 1. 失衡
    // 2. 送达
    // 3. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips => WithDeliveredTip(
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<DeliveryPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    );

    // 动态变量：
    // 1. 施加的失衡值
    // 2. 法术伤害
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(20m),
        new DamageVar(18m, ValueProp.Unpowered | ValueProp.Move)
    ];

    // 初始化卡牌的基础信息：3费、攻击、罕见、目标为单体敌人。
    public ChargedImpact()
        : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时，先施加失衡，再造成一次法术伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：对目标施加失衡。
        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            this
        );

        // 第二步：计算法术修正后的伤害并结算。
        decimal damage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);
        await SpellHelper.Damage(choiceContext, base.Owner.Creature, cardPlay.Target, damage, this);
    }

    // 升级后同时提高失衡值和法术伤害。
    protected override void OnUpgrade()
    {
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(8m);
        base.DynamicVars.Damage.UpgradeValueBy(7m);
    }

    // 送达时，随机本场战斗中这张牌的耗能。
    protected override Task OnDelivered(DeliveryPower deliveryPower)
    {
        base.EnergyCost.SetThisCombat(base.Owner.RunState.Rng.CombatEnergyCosts.NextInt(4));
        base.InvokeEnergyCostChanged();
        NCard.FindOnTable(this)?.PlayRandomizeCostAnim();
        return Task.CompletedTask;
    }

    // 额外描述参数：让描述中的法术伤害显示当前修正后的数值。
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
