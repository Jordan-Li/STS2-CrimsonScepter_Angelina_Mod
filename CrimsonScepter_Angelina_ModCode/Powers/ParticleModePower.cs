using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// 效果：
/// 1. 自己打出的攻击牌伤害减半。
/// 2. 每张攻击牌每回合首次打出后，改为回到手牌。
/// 3. 返回手牌后，本回合下一次打出前费用变为0。
/// </summary>
public sealed class ParticleModePower : AngelinaPower
{
    private sealed class Data
    {
        public HashSet<CardModel> ReturnedAttacksThisTurn { get; } = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 自己打出的攻击牌伤害减半。
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != base.Owner)
        {
            return 1m;
        }

        if (cardSource?.Owner?.Creature != base.Owner || cardSource.Type != CardType.Attack)
        {
            return 1m;
        }

        return 0.5m;
    }

    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        if (cardSource?.Owner?.Creature == base.Owner && cardSource.Type == CardType.Attack)
        {
            Flash();
        }

        return Task.CompletedTask;
    }

    // 每张攻击牌每回合首次打出后，改为回到手牌。
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card.Owner.Creature != base.Owner || card.Type != CardType.Attack)
        {
            return (pileType, position);
        }

        if (pileType == PileType.None)
        {
            return (pileType, position);
        }

        Data data = GetInternalData<Data>();
        if (data.ReturnedAttacksThisTurn.Contains(card))
        {
            return (pileType, position);
        }

        data.ReturnedAttacksThisTurn.Add(card);
        return (PileType.Hand, CardPilePosition.Bottom);
    }

    // 回到手牌后，本回合费用变为0。
    public override async Task AfterModifyingCardPlayResultPileOrPosition(
        CardModel card,
        PileType pileType,
        CardPilePosition position)
    {
        if (card.Owner.Creature != base.Owner || card.Type != CardType.Attack || pileType != PileType.Hand)
        {
            return;
        }

        card.EnergyCost.SetThisTurn(0);
        card.SetStarCostThisTurn(0);
        card.InvokeEnergyCostChanged();
        Flash();
        await Task.CompletedTask;
    }

    // 回合结束时清空“本回合已返回过”的记录。
    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            GetInternalData<Data>().ReturnedAttacksThisTurn.Clear();
        }

        return Task.CompletedTask;
    }
}
