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
/// 卡牌名：彗星连击
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：攻击
/// 效果：造成3点法术伤害。若目标处于失重，抽1张牌。
/// 升级后效果：造成3点法术伤害。若目标处于失重，抽2张牌。
/// </summary>
public sealed class CometCombo : AngelinaCard
{
    public override bool IsSpell => true;

    // 额外悬浮说明：
    // 1. 失重
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeightlessPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：
    // 1. 法术伤害
    // 2. 处于失重时抽牌数
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Unpowered | ValueProp.Move),
        new CardsVar(1)
    ];

    // 初始化卡牌的基础信息：0费、攻击、罕见、目标为单体敌人。
    public CometCombo()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时，先造成法术伤害；若目标原本处于失重，则再抽牌。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：记录目标是否处于失重，用于决定后续是否抽牌。
        bool shouldDraw = cardPlay.Target.GetPower<WeightlessPower>() != null;

        // 第二步：结算法术伤害。
        await SpellHelper.Damage(
            choiceContext,
            base.Owner.Creature,
            cardPlay.Target,
            SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue),
            this
        );

        // 第三步：若目标原本处于失重，则抽牌。
        if (shouldDraw)
        {
            await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
        }
    }

    // 升级后提高抽牌数。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
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
