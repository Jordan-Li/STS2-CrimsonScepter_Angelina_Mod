using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：兼职工作
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：回合结束时，若你在本回合没有打出过攻击牌，恢复5点生命值。消耗。
/// 升级后效果：回合结束时，若你在本回合没有打出过攻击牌，恢复8点生命值。消耗。
/// </summary>
public sealed class PartTimeJob : AngelinaCard
{
    // 关键词说明：这张牌带有消耗。
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    // 额外悬浮说明：
    // 1. 消耗
    // 2. 兼职工作对应的延时恢复效果
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<PartTimeJobPower>()
    ];

    // 动态变量：回合结束时恢复的生命值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Heal", 5m)
    ];

    // 初始化卡牌的基础信息：0费、技能、罕见、目标为自己。
    public PartTimeJob()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，施加一个持续到回合结束的延时恢复 Power。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 这张牌本身不立即回血，而是把“回合结束时检查是否打过攻击牌”的逻辑挂到 Power 里。
        await PowerCmd.Apply<PartTimeJobPower>(
            base.Owner.Creature,
            base.DynamicVars["Heal"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后将恢复量从5提高到8。
    protected override void OnUpgrade()
    {
        base.DynamicVars["Heal"].UpgradeValueBy(3m);
    }
}