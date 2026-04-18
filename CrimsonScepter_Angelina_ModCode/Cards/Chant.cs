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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：吟唱
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：获得8点法术格挡。下回合获得1点集中。
/// 升级后效果：获得10点法术格挡。下回合获得2点集中。
/// </summary>
public sealed class Chant : AngelinaCard
{
    // 这张牌会提供格挡。
    public override bool GainsBlock => true;

    // 显式标记：这是一张法术牌。
    public override bool IsSpell => true;

    // 额外悬浮说明：
    // 1. 集中
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FocusPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：
    // 1. 法术格挡数值
    // 2. 下回合获得的集中数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Unpowered | ValueProp.Move),
        new PowerVar<ChantTemporaryFocusNextTurnPower>(1m)
    ];

    // 初始化卡牌的基础信息：1费、技能、罕见、目标为自己。
    public Chant()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，先获得法术格挡，再挂上“下回合获得集中”的延时效果。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：按法术规则计算当前实际格挡值。
        decimal block = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Block.BaseValue);

        // 第二步：获得法术格挡。
        await SpellHelper.GainBlock(base.Owner.Creature, base.Owner.Creature, block, cardPlay);

        // 第三步：施加延时 Power，在下回合开始时给予集中。
        await PowerCmd.Apply<ChantTemporaryFocusNextTurnPower>(
            base.Owner.Creature,
            base.DynamicVars["ChantTemporaryFocusNextTurnPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后同时提高法术格挡与下回合获得的集中数值。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
        base.DynamicVars["ChantTemporaryFocusNextTurnPower"].UpgradeValueBy(1m);
    }

    // 预览描述时，补入受集中影响后的法术格挡数值。
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
