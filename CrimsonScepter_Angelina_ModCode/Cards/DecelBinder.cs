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
/// 卡牌名：凝滞师
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：能力
/// 效果：每当你打出一张攻击牌时，为其目标施加1层停顿。
/// 升级后效果：固有。
/// </summary>
public sealed class DecelBinder : AngelinaCard
{
    // 升级后获得固有关键词。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Innate]
        : [];

    // 额外悬浮说明：
    // 1. 停顿
    // 2. 凝滞师对应的持续能力
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
            HoverTipFactory.FromPower<StaggerPower>(),
            HoverTipFactory.FromPower<DecelBinderPower>()
        ]
        : [
            HoverTipFactory.FromPower<StaggerPower>(),
            HoverTipFactory.FromPower<DecelBinderPower>()
        ];

    // 动态变量：每次施加的停顿层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DecelBinderPower>(1m)
    ];

    // 初始化卡牌的基础信息：1费、能力、罕见、目标为自己。
    public DecelBinder()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，施加一个持续能力 Power：
    // 之后每次打出攻击牌时，为其目标施加停顿。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先播放施法动作，再挂上持续 Power。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<DecelBinderPower>(
            base.Owner.Creature,
            base.DynamicVars["DecelBinderPower"].BaseValue,
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
