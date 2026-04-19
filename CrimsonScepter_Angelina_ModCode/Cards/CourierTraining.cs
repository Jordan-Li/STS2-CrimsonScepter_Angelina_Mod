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
/// 卡牌名：信使训练
/// 费用：2
/// 稀有度：稀有
/// 卡牌类型：能力
/// 效果：每回合开始时，获得1层飞行。
/// 升级后效果：固有。
/// </summary>
public sealed class CourierTraining : AngelinaCard
{
    // 升级后获得固有关键词。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Innate]
        : [];

    // 额外悬浮说明：
    // 1. 固有（仅升级后）
    // 2. 信使训练对应的持续能力
    // 3. 飞行
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
            HoverTipFactory.FromPower<CourierTrainingPower>(),
            HoverTipFactory.FromPower<FlyPower>()
        ]
        : [
            HoverTipFactory.FromPower<CourierTrainingPower>(),
            HoverTipFactory.FromPower<FlyPower>()
        ];

    // 动态变量：每回合开始时获得的飞行层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<CourierTrainingPower>(1m)
    ];

    // 初始化卡牌的基础信息：2费、能力、稀有、目标为自己。
    public CourierTraining()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时，施加一个持续能力 Power：
    // 每回合开始时给予自己飞行。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先播放施法动作，再挂上对应的持续 Power。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<CourierTrainingPower>(
            base.Owner.Creature,
            base.DynamicVars["CourierTrainingPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后为这张能力牌添加固有。
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
