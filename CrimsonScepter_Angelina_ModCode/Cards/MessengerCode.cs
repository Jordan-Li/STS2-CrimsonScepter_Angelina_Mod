using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：信使准则
/// 费用：1
/// 稀有度：稀有
/// 卡牌类型：能力
/// 效果：每回合额外抽1张牌，然后寄送1张牌。
/// 升级后效果：固有。每回合额外抽1张牌，然后寄送3张牌。
/// </summary>
public sealed class MessengerCode : AngelinaCard
{
    // 升级后获得固有关键词。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Innate]
        : [];

    // 额外悬浮说明：
    // 1. 固有（仅升级后）
    // 2. 信使准则对应的持续能力
    // 3. 寄送
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [
            HoverTipFactory.FromKeyword(CardKeyword.Innate),
            HoverTipFactory.FromPower<MessengerCodePower>(),
            HoverTipFactory.FromPower<DeliveryPower>()
        ]
        : [
            HoverTipFactory.FromPower<MessengerCodePower>(),
            HoverTipFactory.FromPower<DeliveryPower>()
        ];

    // 初始化卡牌的基础信息：1费、能力、稀有、目标为自己。
    public MessengerCode()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时，施加一个持续能力 Power：
    // 每回合额外抽1张牌，并在抽牌后额外寄送若干张牌。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 升级前每回合寄送1张，升级后每回合寄送3张。
        decimal deliveryCount = IsUpgraded ? 3m : 1m;

        // 先播放施法动作，再挂上持续 Power。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<MessengerCodePower>(base.Owner.Creature, deliveryCount, base.Owner.Creature, this);
    }

    // 升级后为这张能力牌添加固有。
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
