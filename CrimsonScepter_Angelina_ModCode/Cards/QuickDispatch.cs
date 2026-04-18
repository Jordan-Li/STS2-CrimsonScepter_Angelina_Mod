using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：快速派送
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：随机选择1张已寄送的牌，将其立即送达。
/// 升级后效果：选择1张已寄送的牌，将其立即送达。
/// </summary>
public sealed class QuickDispatch : AngelinaCard
{
    // 额外悬浮说明：寄送。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    // 只有存在已寄送的牌时，这张牌才能打出。
    protected override bool IsPlayable =>
        base.Owner?.Creature.GetPower<DeliveryPower>()?.GetQueuedCards().Count > 0;

    // 有可送达目标时以金色发光提示。
    protected override bool ShouldGlowGoldInternal => IsPlayable;

    // 初始化卡牌的基础信息：0费、技能、罕见、目标为自己。
    public QuickDispatch()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，立刻让已寄送的牌中的一张送达。
    // 未升级时随机送达，升级后改为手动选择送达。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 没有已寄送的牌时，不执行任何效果。
        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        if (deliveryPower == null || deliveryPower.GetQueuedCards().Count == 0)
        {
            return;
        }

        // 升级后由玩家选择要立即送达的牌；未升级时随机送达一张。
        if (base.IsUpgraded)
        {
            await deliveryPower.DeliverChosen(choiceContext, this);
        }
        else
        {
            await deliveryPower.DeliverRandom(choiceContext, this);
        }
    }

    // 升级本身不改数值，差异体现在改为手动选择送达目标。
    protected override void OnUpgrade()
    {
    }
}