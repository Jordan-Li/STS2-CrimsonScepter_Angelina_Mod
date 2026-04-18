using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：浮空压制
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：给予目标1层飞行，使其本回合失去8点力量。
/// 升级后效果：给予目标1层飞行，使其本回合失去10点力量。
/// </summary>

public sealed class AerialSuppression : AngelinaCard
{
    // 动态变量：
    // 1. 给予目标的飞行层数
    // 2. 本回合失去的力量值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FlyPower>(1m),
        new DynamicVar("StrengthLoss", 8m)
    ];

    // 额外悬浮说明：
    // 1. 飞行
    // 2. 力量
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FlyPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    // 初始化卡牌的基础信息：1费、技能、罕见、目标为单体敌人。
    public AerialSuppression()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时，先给予目标飞行，再让其本回合失去力量。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：给予目标飞行。
        await PowerCmd.Apply<FlyPower>(
            cardPlay.Target,
            base.DynamicVars["FlyPower"].BaseValue,
            base.Owner.Creature,
            this
        );

        // 第二步：让目标本回合失去力量。
        await PowerCmd.Apply<PiercingWailPower>(
            cardPlay.Target,
            base.DynamicVars["StrengthLoss"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后提高失去的力量值。
    protected override void OnUpgrade()
    {
        base.DynamicVars["StrengthLoss"].UpgradeValueBy(2m);
    }
}
