using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：信使准则
/// 效果：每回合额外抽1张牌，然后寄送1张牌。
/// </summary>
public sealed class MessengerCodePower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：寄送。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    // 每回合的基础抽牌数额外+1。
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != base.Owner.Player)
        {
            return count;
        }

        return count + 1m;
    }

    // 玩家回合开始时，额外选择若干张手牌并寄送。
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player)
        {
            return;
        }

        // 根据 Power 数值决定本回合要寄送多少张牌；没有手牌时直接结束。
        int handCount = player.PlayerCombatState?.Hand.Cards.Count ?? 0;
        int deliveryCount = (int)System.Math.Min(base.Amount, handCount);
        if (deliveryCount <= 0)
        {
            return;
        }

        // 从手牌中选择对应数量的牌进行寄送。
        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: player,
            prefs: new CardSelectorPrefs(new LocString("cards", "MESSENGER_CODE.selectPrompt"), deliveryCount),
            filter: _ => true,
            source: null!)).ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        Flash();

        DeliveryPower? deliveryPower = base.Owner.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner, 1m, base.Owner, null!);

        if (deliveryPower is null)
        {
            return;
        }

        // 将所选牌逐张移入 Exhaust，并逐张加入寄送队列。
        foreach (CardModel selectedCard in selectedCards)
        {
            await CardCmd.Exhaust(choiceContext, selectedCard);
            await deliveryPower.EnqueueCard(selectedCard);
        }
    }
}
