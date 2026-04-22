using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：快速派送
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：获得最近1张已寄送的牌。寄送1张牌。
/// 升级后效果：获得最近2张已寄送的牌。寄送2张牌。
/// </summary>
public sealed class QuickDispatch : AngelinaCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new IntVar("DeliveredCards", 1)
    ];

    protected override bool IsPlayable => base.Owner?.PlayerCombatState?.Hand.Cards.Count > 0
        || base.Owner?.Creature.GetPower<DeliveryPower>()?.GetQueuedCards().Count > 0;

    public QuickDispatch()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();

        int deliverCount = System.Math.Min(base.DynamicVars["DeliveredCards"].IntValue, deliveryPower?.GetQueuedCards().Count ?? 0);
        for (int i = 0; i < deliverCount; i++)
        {
            if (deliveryPower == null || !await deliveryPower.DeliverLatestNow())
            {
                break;
            }
        }

        int handCount = base.Owner.PlayerCombatState?.Hand.Cards.Count ?? 0;
        int sendCount = System.Math.Min(base.DynamicVars.Cards.IntValue, handCount);
        if (sendCount <= 0)
        {
            return;
        }

        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "QUICK_DISPATCH.sendPrompt"), sendCount),
            filter: null,
            source: this)).ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        // 立即送达可能会把寄送队列送空，并导致旧的 DeliveryPower 自行移除。
        // 这里重新获取一次，避免后续把新牌加入到已经失效的旧实例里。
        deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);
        if (deliveryPower == null)
        {
            return;
        }

        foreach (CardModel selectedCard in selectedCards)
        {
            await CardCmd.Exhaust(choiceContext, selectedCard);
            await deliveryPower.EnqueueCard(selectedCard);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
        base.DynamicVars["DeliveredCards"].UpgradeValueBy(1m);
    }
}
