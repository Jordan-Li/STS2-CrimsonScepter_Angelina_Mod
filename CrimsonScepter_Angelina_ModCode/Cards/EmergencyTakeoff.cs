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
/// 卡牌名：紧急升空
/// 费用：0
/// 稀有度：普通
/// 卡牌类型：技能
/// 效果：获得1层飞行。获得8点法术格挡。
/// 升级后效果：获得1层飞行。获得11点法术格挡。
/// </summary>
public sealed class EmergencyTakeoff : AngelinaCard
{
    // 这张牌会提供格挡，用于驱动格挡相关显示与结算。
    public override bool GainsBlock => true;

    // 额外悬浮说明：
    // 1. 飞行
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FlyPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：
    // 1. 飞行层数
    // 2. 法术格挡
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FlyPower>(1m),
        new BlockVar(8m, ValueProp.Unpowered | ValueProp.Move)
    ];

    // 初始化卡牌的基础信息：0费、技能、普通、目标为自己。
    public EmergencyTakeoff()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时，先获得飞行，再获得法术格挡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：给予自己飞行。
        await PowerCmd.Apply<FlyPower>(
            base.Owner.Creature,
            base.DynamicVars["FlyPower"].BaseValue,
            base.Owner.Creature,
            this
        );

        // 第二步：计算法术修正后的格挡并给予自己。
        decimal block = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Block.BaseValue);
        await SpellHelper.GainBlock(base.Owner.Creature, base.Owner.Creature, block, cardPlay);
    }

    // 升级后提高法术格挡。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
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
