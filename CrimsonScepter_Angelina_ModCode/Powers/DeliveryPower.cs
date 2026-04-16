using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：寄送
/// Power类型：状态型Power
/// 效果：
/// 1. 所有被寄送的牌都整合到同一个Power里显示
/// 2. Amount显示当前寄送牌数量
/// 3. 悬浮描述完整显示全部寄送牌名
/// 4. 在下个回合抽牌前，把所有寄送牌送回手牌
/// 备注：这是新版单图标寄送实现，用来替代旧版“一张牌一个DeliveryPower图标”的方案
/// </summary>
public sealed class DeliveryPower : AngelinaPower
{
    // 内部数据：
    // QueuedCards = 当前所有被寄送的牌
    private sealed class Data
    {
        public List<CardModel> QueuedCards = new();
    }

    // 当前按Buff显示
    public override PowerType Type => PowerType.Buff;

    // 单实例Power：所有寄送牌都合并到这一个Power里
    public override bool IsInstanced => false;

    // 使用计数层数显示当前寄送牌数量
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 动态变量：
    // Cards = 当前所有寄送牌的完整名字列表
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new StringVar("Cards")
    };

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 在抽牌前，把当前所有寄送牌全部送回手牌
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner.Player)
        {
            return;
        }

        await DeliverAllNow();
    }

    /// <summary>
    /// 兼容旧版调用。
    /// 旧版每次只记录一张牌；新版这里改成“加入寄送队列”。
    /// </summary>
    public async Task SetSelectedCard(CardModel card)
    {
        await EnqueueCard(card);
    }

    /// <summary>
    /// 兼容旧版调用。
    /// 返回最近寄送的一张牌。
    /// 这个主要给 PackagePreview 之类的牌继续使用。
    /// </summary>
    public CardModel? GetSelectedCard()
    {
        return PeekLatest();
    }

    /// <summary>
    /// 兼容旧版调用。
    /// 旧版 DeliverNow 默认送回当前这层Power记录的那张牌；
    /// 新版这里改成送回“最近寄送的一张牌”。
    /// </summary>
    public async Task<bool> DeliverNow()
    {
        return await DeliverLatestNow();
    }

    /// <summary>
    /// 新版：把一张牌加入寄送队列。
    /// 这里不会额外创建新Power，只会把牌塞进当前单实例Power中。
    /// </summary>
    public async Task EnqueueCard(CardModel card)
    {
        Data data = GetInternalData<Data>();

        if (!data.QueuedCards.Contains(card))
        {
            data.QueuedCards.Add(card);
        }

        CleanupQueue();
        RefreshQueueDisplay();

        await Task.CompletedTask;
    }

    /// <summary>
    /// 返回当前所有仍然有效的寄送牌。
    /// 只保留还在 Exhaust 堆里的牌。
    /// </summary>
    public IReadOnlyList<CardModel> GetQueuedCards()
    {
        CleanupQueue();
        return GetInternalData<Data>().QueuedCards.ToList();
    }

    /// <summary>
    /// 返回最近寄送的一张仍然有效的牌。
    /// </summary>
    public CardModel? PeekLatest()
    {
        CleanupQueue();
        return GetInternalData<Data>().QueuedCards.LastOrDefault();
    }

    /// <summary>
    /// 立刻送回指定的一张寄送牌。
    /// 成功返回 true，失败返回 false。
    /// </summary>
    public async Task<bool> DeliverCardNow(CardModel card)
    {
        Data data = GetInternalData<Data>();

        CleanupQueue();

        if (!data.QueuedCards.Contains(card) || card.Pile?.Type != PileType.Exhaust)
        {
            return false;
        }

        await CardPileCmd.Add(card, PileType.Hand, source: this);

        data.QueuedCards.Remove(card);
        RefreshQueueDisplay();

        if (data.QueuedCards.Count == 0)
        {
            await PowerCmd.Remove(this);
        }

        return true;
    }

    /// <summary>
    /// 立刻送回最近寄送的一张牌。
    /// </summary>
    public async Task<bool> DeliverLatestNow()
    {
        CardModel? latestCard = PeekLatest();
        if (latestCard == null)
        {
            return false;
        }

        return await DeliverCardNow(latestCard);
    }

    /// <summary>
    /// 立刻送回当前所有寄送牌。
    /// 返回成功送回的数量。
    /// </summary>
    public async Task<int> DeliverAllNow()
    {
        CleanupQueue();

        List<CardModel> queuedCards = GetInternalData<Data>().QueuedCards.ToList();
        int deliveredCount = 0;

        foreach (CardModel card in queuedCards)
        {
            if (await DeliverCardNow(card))
            {
                deliveredCount++;
            }
        }

        CleanupQueue();
        RefreshQueueDisplay();

        if (GetInternalData<Data>().QueuedCards.Count == 0)
        {
            await PowerCmd.Remove(this);
        }

        return deliveredCount;
    }

    /// <summary>
    /// 清理无效寄送牌：
    /// - null
    /// - 已经不在 Exhaust 里的牌
    /// </summary>
    private void CleanupQueue()
    {
        Data data = GetInternalData<Data>();

        data.QueuedCards = data.QueuedCards
            .Where(card => card != null && card.Pile?.Type == PileType.Exhaust)
            .ToList();
    }

    /// <summary>
    /// 刷新：
    /// 1. Power层数（当前寄送牌数量）
    /// 2. 悬浮描述中的完整牌名列表
    /// </summary>
    private void RefreshQueueDisplay()
    {
        Data data = GetInternalData<Data>();

        base.Amount = data.QueuedCards.Count;

        // 按你的要求：不截断，不写“等X张”，而是把所有寄送牌完整列出来
        string fullCardList = string.Join("\n", data.QueuedCards.Select(card => $"• {card.Title}"));

        ((StringVar)base.DynamicVars["Cards"]).StringValue = fullCardList;
    }
}