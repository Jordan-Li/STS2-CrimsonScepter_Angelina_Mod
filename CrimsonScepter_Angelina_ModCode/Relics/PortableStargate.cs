using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;

/// <summary>
/// 遗物名：便携星门
/// 稀有度：稀有
/// 效果：当你寄送牌时，抽2张牌。随后休眠2回合。
/// </summary>
public sealed class PortableStargate : AngelinaRelic
{
    private int _cooldown;
    private bool _triggeredDeliveryLastTurn;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => DisplayAmount > 0;

    public override int DisplayAmount
    {
        get
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                return -1;
            }

            if (base.IsCanonical || _cooldown <= 0)
            {
                return -1;
            }

            return _cooldown;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(2),
        new DynamicVar("Turns", 2m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<DeliveryPower>()
    };

    private int Cooldown
    {
        get => _cooldown;
        set
        {
            AssertMutable();
            _cooldown = value;
            InvokeDisplayAmountChanged();
        }
    }

    private bool TriggeredDeliveryLastTurn
    {
        get => _triggeredDeliveryLastTurn;
        set
        {
            AssertMutable();
            _triggeredDeliveryLastTurn = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        _ = source;

        if (card.Owner != base.Owner || card.Pile?.Type != PileType.Exhaust || oldPileType == PileType.Exhaust)
        {
            return;
        }

        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        if (deliveryPower == null)
        {
            return;
        }

        // 兼容新版 DeliveryPower 的队列实现
        if (deliveryPower.PeekLatest() != card && !deliveryPower.GetQueuedCards().Contains(card))
        {
            return;
        }

        await TryTriggerOnSend();
    }

    public async Task TryTriggerOnSend()
    {
        if (Cooldown > 0)
        {
            return;
        }

        Flash();
        Cooldown = base.DynamicVars["Turns"].IntValue;
        base.Status = RelicStatus.Normal;
        TriggeredDeliveryLastTurn = true;
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), base.DynamicVars.Cards.IntValue, base.Owner);
    }

    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        _ = combatState;

        if (side != base.Owner.Creature.Side)
        {
            return Task.CompletedTask;
        }

        if (TriggeredDeliveryLastTurn)
        {
            TriggeredDeliveryLastTurn = false;
            return Task.CompletedTask;
        }

        bool wasCoolingDown = Cooldown > 0;
        Cooldown--;

        if (Cooldown <= 0 && wasCoolingDown)
        {
            base.Status = RelicStatus.Active;
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _ = room;

        base.Status = RelicStatus.Normal;
        Cooldown = 0;
        TriggeredDeliveryLastTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
