using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：寄送
/// Power类型：状态型Power
/// 效果：
/// 1. 用单个Power统一管理所有已寄送的牌
/// 2. 层数显示当前寄送牌数量
/// 3. 动态描述展示完整寄送列表
/// 4. 在下个回合抽牌前统一送达
/// </summary>
public sealed class DeliveryPower : AngelinaPower
{
    /// <summary>
    /// 用内部列表保存当前仍停留在消耗牌堆中的寄送牌。
    /// </summary>
    private sealed class Data
    {
        public List<CardModel> QueuedCards { get; set; } = new();
    }

    public override PowerType Type => PowerType.Buff;

    /// <summary>
    /// 所有寄送牌共用同一个图标，不再一张牌一个Power。
    /// </summary>
    public override bool IsInstanced => false;

    /// <summary>
    /// 层数直接显示当前寄送队列中的牌数。
    /// </summary>
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => GetQueuedCards().Count;
    public override bool ShouldScaleInMultiplayer => false;

    /// <summary>
    /// QueuedCount 和 Cards 分别用于展示寄送数量与完整寄送列表。
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("QueuedCount", 0m), new StringVar("Cards")];

    protected override object InitInternalData()
    {
        return new Data();
    }

    /// <summary>
    /// 每回合抽牌前自动结算全部寄送牌。
    /// </summary>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        _ = choiceContext;
        _ = combatState;

        if (player != base.Owner.Player)
        {
            return;
        }

        await DeliverAllNow();
    }

    /// <summary>
    /// 把一张牌加入寄送队列；若被安检拦截则改走安检流程。
    /// </summary>
    public async Task EnqueueCard(CardModel card)
    {
        SecurityCheckPower? securityCheckPower = base.Owner.GetPower<SecurityCheckPower>();
        if (securityCheckPower != null && await securityCheckPower.TryInterceptDelivery(this, card))
        {
            await RefreshAfterQueueChanged();
            return;
        }

        Data data = GetInternalData<Data>();

        if (!data.QueuedCards.Contains(card))
        {
            data.QueuedCards.Add(card);
        }

        await RefreshAfterQueueChanged();
    }

    /// <summary>
    /// 把寄送队列中的旧牌引用替换成变化后的新牌，避免队列残留已失效对象。
    /// 这里不再重复走一次安检，因为这张牌本来就已经处于寄送流程中。
    /// </summary>
    public async Task ReplaceQueuedCard(CardModel originalCard, CardModel replacementCard)
    {
        Data data = GetInternalData<Data>();
        int originalIndex = data.QueuedCards.IndexOf(originalCard);

        if (originalIndex >= 0)
        {
            data.QueuedCards[originalIndex] = replacementCard;

            for (int i = data.QueuedCards.Count - 1; i >= 0; i--)
            {
                if (i != originalIndex && data.QueuedCards[i] == replacementCard)
                {
                    data.QueuedCards.RemoveAt(i);
                }
            }
        }
        else if (!data.QueuedCards.Contains(replacementCard))
        {
            data.QueuedCards.Add(replacementCard);
        }

        await RefreshAfterQueueChanged();
    }

    /// <summary>
    /// 返回当前仍有效的寄送牌，只保留还在消耗牌堆中的牌。
    /// </summary>
    public IReadOnlyList<CardModel> GetQueuedCards()
    {
        CleanupQueue();
        return GetInternalData<Data>().QueuedCards.ToList();
    }

    /// <summary>
    /// 查看最近寄送的一张有效牌。
    /// </summary>
    public CardModel? PeekLatest()
    {
        CleanupQueue();
        return GetInternalData<Data>().QueuedCards.LastOrDefault();
    }

    /// <summary>
    /// 随机立即送达一张寄送牌。
    /// </summary>
    public async Task<CardModel?> DeliverRandom(PlayerChoiceContext choiceContext, CardModel source)
    {
        _ = choiceContext;
        _ = source;

        List<CardModel> queuedCards = GetQueuedCards().ToList();
        if (queuedCards.Count == 0)
        {
            return null;
        }

        CardModel? selectedCard = base.Owner.Player?.RunState.Rng.CombatCardSelection.NextItem(queuedCards);
        return selectedCard == null ? null : await DeliverCardNow(selectedCard);
    }

    /// <summary>
    /// 选择一张寄送牌立即送达。
    /// </summary>
    public async Task<CardModel?> DeliverChosen(PlayerChoiceContext choiceContext, CardModel source)
    {
        _ = source;

        List<CardModel> queuedCards = GetQueuedCards().ToList();
        if (queuedCards.Count == 0 || base.Owner.Player == null)
        {
            return null;
        }

        if (queuedCards.Count == 1)
        {
            return await DeliverCardNow(queuedCards[0]);
        }

        CardModel? selectedCard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            queuedCards,
            base.Owner.Player,
            new CardSelectorPrefs(new LocString("cards", "QUICK_DISPATCH.selectPrompt"), 1)))
            .FirstOrDefault();

        if (selectedCard == null)
        {
            return null;
        }

        return await DeliverCardNow(selectedCard);
    }

    /// <summary>
    /// 立即送达指定寄送牌。
    /// </summary>
    public async Task<CardModel?> DeliverCardNow(CardModel card)
    {
        CleanupQueue();

        Data data = GetInternalData<Data>();
        if (!data.QueuedCards.Contains(card) || card.Pile?.Type != PileType.Exhaust)
        {
            return null;
        }

        data.QueuedCards.Remove(card);
        await CardPileCmd.Add(card, PileType.Hand, source: this);
        await RefreshAfterQueueChanged();

        return card;
    }

    /// <summary>
    /// 立即送达最近寄送的一张牌。
    /// </summary>
    public Task<bool> DeliverLatestNow()
    {
        CardModel? latestCard = PeekLatest();
        return latestCard == null ? Task.FromResult(false) : DeliverLatestNowInternal(latestCard);
    }

    private async Task<bool> DeliverLatestNowInternal(CardModel latestCard)
    {
        return await DeliverCardNow(latestCard) != null;
    }

    /// <summary>
    /// 立即送达当前所有寄送牌。
    /// </summary>
    public async Task<int> DeliverAllNow()
    {
        CleanupQueue();

        Data data = GetInternalData<Data>();
        List<CardModel> queuedCards = data.QueuedCards.ToList();
        int deliveredCount = 0;

        foreach (CardModel card in queuedCards)
        {
            if (!data.QueuedCards.Contains(card) || card.Pile?.Type != PileType.Exhaust)
            {
                continue;
            }

            data.QueuedCards.Remove(card);
            await CardPileCmd.Add(card, PileType.Hand, source: this);
            deliveredCount++;
        }

        await RefreshAfterQueueChanged();

        return deliveredCount;
    }

    /// <summary>
    /// 清理已经失效的寄送记录，避免显示脏数据。
    /// </summary>
    private void CleanupQueue()
    {
        Data data = GetInternalData<Data>();
        data.QueuedCards.RemoveAll(card => card == null || card.HasBeenRemovedFromState || card.Pile?.Type != PileType.Exhaust);
    }

    /// <summary>
    /// 刷新层数显示和完整寄送列表描述。
    /// </summary>
    private void RefreshQueueDisplay()
    {
        Data data = GetInternalData<Data>();
        int queuedCount = data.QueuedCards.Count;

        string fullCardList = queuedCount == 0
            ? "无"
            : string.Join("\n", data.QueuedCards.Select(card => $"• {card.Title}"));

        base.DynamicVars["QueuedCount"].BaseValue = queuedCount;
        ((StringVar)base.DynamicVars["Cards"]).StringValue = fullCardList;
        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 每次队列发生变化后统一刷新显示，并在空队列时移除自身。
    /// </summary>
    private async Task RefreshAfterQueueChanged()
    {
        CleanupQueue();
        RefreshQueueDisplay();
        await RemoveSelfIfEmpty();
    }

    /// <summary>
    /// 队列为空时移除自身，避免场上残留空壳Power。
    /// </summary>
    private Task RemoveSelfIfEmpty()
    {
        return GetInternalData<Data>().QueuedCards.Count == 0
            ? PowerCmd.Remove(this)
            : Task.CompletedTask;
    }
}
