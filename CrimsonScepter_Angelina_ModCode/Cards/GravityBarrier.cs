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
/// 卡牌名：重力结界
/// 费用：2
/// 稀有度：稀有
/// 卡牌类型：技能
/// 效果：获得12点法术格挡。本回合内，当来自敌方的伤害完全被格挡时，对伤害来源施加8点失衡。
/// 升级后效果：获得15点法术格挡。本回合内，当来自敌方的伤害完全被格挡时，对来源施加12点失衡。
/// </summary>
public sealed class GravityBarrier : AngelinaCard
{
    // 这张牌会提供格挡。
    public override bool GainsBlock => true;

    // 额外悬浮说明：
    // 1. 失衡
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：
    // 1. 法术格挡数值
    // 2. 重力结界附加的失衡数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Unpowered | ValueProp.Move),
        new PowerVar<GravityBarrierPower>(8m)
    ];

    // 初始化卡牌的基础信息：2费、技能、稀有、目标为自己。
    public GravityBarrier()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 显式标记：这是一张法术牌。
    public override bool IsSpell => true;

    // 打出时，先获得法术格挡，再施加本回合生效的重力结界效果。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：按法术规则计算当前的实际格挡值。
        decimal block = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Block.BaseValue);

        // 第二步：获得法术格挡。
        await SpellHelper.GainBlock(base.Owner.Creature, base.Owner.Creature, block, cardPlay);

        // 第三步：对自己施加重力结界 Power，用于本回合追踪“完全格挡敌伤”后的反制效果。
        await PowerCmd.Apply<GravityBarrierPower>(
            base.Owner.Creature,
            base.DynamicVars["GravityBarrierPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后提高法术格挡，并把施加的失衡从 8 提高到 12。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
        base.DynamicVars["GravityBarrierPower"].UpgradeValueBy(4m);
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
