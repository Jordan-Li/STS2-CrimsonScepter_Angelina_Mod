using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：酸橙风暴
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：对目标造成5点法术伤害。对所有造成5点伤害。
/// 升级后效果：对目标造成7点法术伤害。对所有敌方造成7点伤害。
/// </summary>
public sealed class LimeStorm : AngelinaCard
{
    // 额外悬浮说明：法术。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：
    // 1. 单体法术伤害
    // 2. 群体普通伤害
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Unpowered | ValueProp.Move),
        new DynamicVar("SplashDamage", 5m)
    ];

    // 初始化卡牌的基础信息：1费、攻击、普通、目标为单体敌人。
    public LimeStorm()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时，先对目标造成法术伤害，再对所有敌人造成普通伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：计算法术修正后的单体伤害并结算。
        decimal spellDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);
        await SpellHelper.Damage(choiceContext, base.Owner.Creature, cardPlay.Target, spellDamage, this);

        // 第二步：对所有敌人结算群体普通伤害。
        var combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during LimeStorm.OnPlay.");
        await CreatureCmd.Damage(
            choiceContext,
            combatState.HittableEnemies,
            base.DynamicVars["SplashDamage"].BaseValue,
            ValueProp.Move,
            base.Owner.Creature,
            this
        );
    }

    // 升级后同时提高单体法术伤害和群体普通伤害。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars["SplashDamage"].UpgradeValueBy(2m);
    }

    // 额外描述参数：让描述里的法术伤害显示当前修正后的数值，并显示群体伤害。
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        decimal displayedDamage = base.DynamicVars.Damage.BaseValue;
        if (base.IsMutable && base.Owner?.Creature != null)
        {
            displayedDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedDamage);
        }

        description.Add("DisplayedDamage", displayedDamage);
        description.Add("SplashDamage", base.DynamicVars["SplashDamage"].BaseValue);
    }
}
