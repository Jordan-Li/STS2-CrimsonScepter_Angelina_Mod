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
/// 卡牌名：能量循环
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：能力
/// 效果：每回合中，打出耗能大于等于2的攻击、技能或能力牌时，回复1点能量。每种类型牌至多各触发1次。
/// 升级后效果：固有。
/// </summary>
public sealed class EnergyCycle : AngelinaCard
{
    // 升级后获得固有关键词。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Innate]
        : [];

    // 额外悬浮说明：
    // 1. 固有（仅升级后）
    // 2. 能量图标
    // 3. 能量循环对应的持续能力
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
            HoverTipFactory.ForEnergy(this),
            HoverTipFactory.FromPower<EnergyCyclePower>()
        ]
        : [
            HoverTipFactory.ForEnergy(this),
            HoverTipFactory.FromPower<EnergyCyclePower>()
        ];

    // 动态变量：
    // 1. 每次触发时回复的能量
    // 2. 每种类型每回合可触发的次数
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new PowerVar<EnergyCyclePower>(1m)
    ];

    // 初始化卡牌的基础信息：1费、能力、罕见、目标为自己。
    public EnergyCycle()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，施加一个持续能力 Power：
    // 每回合中，高耗能的攻击/技能/能力牌会回能。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先播放施法动作，再挂上持续 Power。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<EnergyCyclePower>(
            base.Owner.Creature,
            base.DynamicVars["EnergyCyclePower"].BaseValue,
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
