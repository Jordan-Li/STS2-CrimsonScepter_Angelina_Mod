using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：法术节奏
/// 效果：
/// 1. 记录本回合已打出的法术牌数量
/// 2. 第2张法术：获得1层临时飞行
/// 3. 第3张法术：从寄送区选择1张牌送达
/// </summary>
public sealed class SpellRhythmPower : AngelinaPower
{
    private sealed class Data
    {
        public int SpellsPlayedThisTurn;
        public bool NextSpellDiscountReady;
    }

    public override PowerType Type => PowerType.Buff;

    public override bool IsInstanced => false;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override int DisplayAmount => GetInternalData<Data>().SpellsPlayedThisTurn;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<TemporaryFlyPower>();

            DeliveryPower? deliveryPower = base.Owner.GetPower<DeliveryPower>();
            if (deliveryPower != null && deliveryPower.GetQueuedCards().Count > 0)
            {
                yield return HoverTipFactory.FromPower<DeliveryPower>();
            }
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("SpellCount", 0m)
    };

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Owner)
        {
            return;
        }

        if (!SpellHelper.IsSpell(cardPlay.Card))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.SpellsPlayedThisTurn++;
        RefreshSmartVars();

        switch (data.SpellsPlayedThisTurn)
        {
            case 2:
                Flash();
                await PowerCmd.Apply<TemporaryFlyPower>(base.Owner, 1m, base.Owner, cardPlay.Card);
                EnableNextSpellDiscount();
                break;

            case 3:
                DeliveryPower? deliveryPower = base.Owner.GetPower<DeliveryPower>();
                if (deliveryPower == null || deliveryPower.GetQueuedCards().Count == 0)
                {
                    break;
                }

                Flash();
                List<CardModel> queuedCards = new(deliveryPower.GetQueuedCards());
                await deliveryPower.DeliverChosen(context, cardPlay.Card);
                CardModel? deliveredCard = queuedCards.Find(card => card.Pile?.Type == PileType.Hand);
                if (deliveredCard != null && SpellHelper.IsSpell(deliveredCard))
                {
                    deliveredCard.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
                    deliveredCard.InvokeEnergyCostChanged();
                }
                break;
        }

        InvokeDisplayAmountChanged();
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        Data data = GetInternalData<Data>();
        if (!data.NextSpellDiscountReady ||
            card.Owner.Creature != base.Owner ||
            !SpellHelper.IsSpell(card))
        {
            return false;
        }

        bool shouldModify;
        switch (card.Pile?.Type)
        {
            case PileType.Hand:
            case PileType.Play:
                shouldModify = true;
                break;
            default:
                shouldModify = false;
                break;
        }

        if (!shouldModify)
        {
            return false;
        }

        modifiedCost = decimal.Max(0m, originalCost - 1m);
        return true;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        if (!data.NextSpellDiscountReady ||
            cardPlay.Card.Owner.Creature != base.Owner ||
            !SpellHelper.IsSpell(cardPlay.Card))
        {
            return Task.CompletedTask;
        }

        bool shouldConsume;
        switch (cardPlay.Card.Pile?.Type)
        {
            case PileType.Hand:
            case PileType.Play:
                shouldConsume = true;
                break;
            default:
                shouldConsume = false;
                break;
        }

        if (!shouldConsume)
        {
            return Task.CompletedTask;
        }

        data.NextSpellDiscountReady = false;
        RefreshHandSpellCostsDisplay();
        return Task.CompletedTask;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            Data data = GetInternalData<Data>();
            data.SpellsPlayedThisTurn = 0;
            bool hadPendingDiscount = data.NextSpellDiscountReady;
            data.NextSpellDiscountReady = false;
            RefreshSmartVars();
            InvokeDisplayAmountChanged();

            if (hadPendingDiscount)
            {
                RefreshHandSpellCostsDisplay();
            }
        }

        return Task.CompletedTask;
    }

    private void RefreshSmartVars()
    {
        base.DynamicVars["SpellCount"].BaseValue = GetInternalData<Data>().SpellsPlayedThisTurn;
    }

    private void EnableNextSpellDiscount()
    {
        Data data = GetInternalData<Data>();
        data.NextSpellDiscountReady = true;
        RefreshHandSpellCostsDisplay();
    }

    private void RefreshHandSpellCostsDisplay()
    {
        IReadOnlyList<CardModel>? handCards = base.Owner.Player?.PlayerCombatState?.Hand.Cards;
        if (handCards == null)
        {
            return;
        }

        foreach (CardModel card in handCards)
        {
            if (SpellHelper.IsSpell(card))
            {
                card.InvokeEnergyCostChanged();
            }
        }
    }
}
