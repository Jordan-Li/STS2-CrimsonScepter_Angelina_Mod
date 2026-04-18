using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：在上面！
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：保留。获得7点法术格挡。若你处于浮空，则对所有不处于浮空的敌方施加10点失衡。
/// 升级后效果：保留。获得9点法术格挡。若你处于浮空，则对所有不处于浮空的敌方施加15点失衡。
/// </summary>
public sealed class UpThere : AngelinaCard
{
    // 这张牌会提供格挡，用于驱动格挡相关显示与结算。
    public override bool GainsBlock => true;

    // 关键字：保留。
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain
    ];

    // 额外悬浮说明：
    // 1. 保留
    // 2. 失衡
    // 3. 飞行
    // 4. 法术
    // 5. 浮空
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<FlyPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description")),
        new HoverTip(
            new LocString("powers", "AIRBORNE.title"),
            new LocString("powers", "AIRBORNE.description"))
    ];

    // 动态变量：
    // 1. 法术格挡
    // 2. 对地面敌人施加的失衡
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Unpowered | ValueProp.Move),
        new PowerVar<ImbalancePower>(10m)
    ];

    // 初始化卡牌的基础信息：1费、技能、罕见、目标为自己。
    public UpThere()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，先获得法术格挡；若自己处于浮空，再对所有未浮空的敌人施加失衡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：计算法术修正后的格挡并给予自己。
        decimal block = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Block.BaseValue);
        await SpellHelper.GainBlock(base.Owner.Creature, base.Owner.Creature, block, cardPlay);

        // 第二步：若自己不处于浮空，则后续群体失衡不触发。
        if (!AirborneHelper.IsAirborne(base.Owner.Creature))
        {
            return;
        }

        var combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during UpThere.OnPlay.");

        // 第三步：筛出所有当前不处于浮空的敌人。
        IEnumerable<Creature> groundedEnemies = combatState.HittableEnemies
            .Where(enemy => !AirborneHelper.IsAirborne(enemy));

        // 第四步：逐个对这些敌人施加失衡。
        foreach (Creature enemy in groundedEnemies)
        {
            await PowerCmd.Apply<ImbalancePower>(
                enemy,
                base.DynamicVars["ImbalancePower"].BaseValue,
                base.Owner.Creature,
                this
            );
        }
    }

    // 升级后同时提高法术格挡和施加的失衡值。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(5m);
    }

    // 额外描述参数：让描述里的法术格挡显示当前修正后的数值。
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        decimal displayedBlock = base.DynamicVars.Block.BaseValue;
        if (base.IsMutable && base.Owner?.Creature != null)
        {
            displayedBlock = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedBlock);
        }

        description.Add("DisplayedBlock", displayedBlock);
    }
}
