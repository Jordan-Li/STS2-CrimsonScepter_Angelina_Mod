using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;

public abstract class DeliveredCardModel : AngelinaCard
{
    protected static readonly IHoverTip DeliveredHoverTip =
        new HoverTip(
            new LocString("cards", "DELIVERED.title"),
            new LocString("cards", "DELIVERED.description"));

    protected DeliveredCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType)
        : base(energyCost, type, rarity, targetType)
    {
    }

    protected static IEnumerable<IHoverTip> WithDeliveredTip(params IHoverTip[] hoverTips)
    {
        foreach (IHoverTip hoverTip in hoverTips)
        {
            yield return hoverTip;
        }

        yield return DeliveredHoverTip;
    }

    public sealed override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        await base.AfterCardChangedPiles(card, oldPileType, source);

        if (card != this ||
            oldPileType != PileType.Exhaust ||
            Pile?.Type != PileType.Hand ||
            source is not DeliveryPower deliveryPower)
        {
            return;
        }

        await OnDelivered(deliveryPower);
    }

    protected abstract Task OnDelivered(DeliveryPower deliveryPower);
}