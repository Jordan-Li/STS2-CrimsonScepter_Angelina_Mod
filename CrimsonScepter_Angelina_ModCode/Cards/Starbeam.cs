using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：星芒
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：造成8点法术伤害。若目标处于浮空，再造成8点法术伤害。
/// 升级后效果：造成10点法术伤害。若目标处于浮空，再造成10点法术伤害。
/// </summary>
public sealed class Starbeam : AngelinaCard
{
    // 额外悬浮说明：
    // 1. 浮空
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("powers", "AIRBORNE.title"),
            new LocString("powers", "AIRBORNE.description")),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：法术伤害。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Unpowered | ValueProp.Move)
    ];

    // 初始化卡牌的基础信息：1费、攻击、普通、目标为单体敌人。
    public Starbeam()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时，先造成一次法术伤害；若目标处于浮空，再追加一次同样的法术伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：计算法术修正后的伤害。
        decimal damage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);

        // 第二步：先对目标结算一次法术伤害。
        await SpellHelper.Damage(
            choiceContext,
            base.Owner.Creature,
            cardPlay.Target,
            damage,
            this
        );

        // 第三步：若目标处于浮空，再追加一次同样的法术伤害。
        if (AirborneHelper.IsAirborne(cardPlay.Target))
        {
            await SpellHelper.Damage(
                choiceContext,
                base.Owner.Creature,
                cardPlay.Target,
                damage,
                this
            );
        }
    }

    // 升级后提高法术伤害。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
    }

    // 额外描述参数：让描述里的法术伤害显示当前修正后的数值。
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
