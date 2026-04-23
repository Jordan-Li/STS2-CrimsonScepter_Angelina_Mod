using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

public sealed class TimeCapsulePower : AngelinaPower
{
    private sealed class Data
    {
        public List<CardModel> TrackedCards { get; set; } = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => false;

    public override int DisplayAmount => GetTrackedCards().Count;

    public override bool ShouldScaleInMultiplayer => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("TrackedCount", 0m),
        new StringVar("TrackedCards")
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public async Task TrackCard(CardModel card)
    {
        Data data = GetInternalData<Data>();
        if (!data.TrackedCards.Contains(card))
        {
            data.TrackedCards.Add(card);
        }

        await RefreshAfterTrackedCardsChanged();
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        await base.AfterCardChangedPiles(card, oldPileType, source);

        if (oldPileType != PileType.Exhaust ||
            card.Pile?.Type != PileType.Hand ||
            source is not DeliveryPower)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (!data.TrackedCards.Remove(card))
        {
            return;
        }

        if (!card.EnergyCost.CostsX)
        {
            card.EnergyCost.AddUntilPlayed(-1, reduceOnly: true);
        }

        await RefreshAfterTrackedCardsChanged();
    }

    private IReadOnlyList<CardModel> GetTrackedCards()
    {
        CleanupTrackedCards();
        return GetInternalData<Data>().TrackedCards.ToList();
    }

    private void CleanupTrackedCards()
    {
        Data data = GetInternalData<Data>();
        data.TrackedCards.RemoveAll(card => card == null || card.HasBeenRemovedFromState);
    }

    private void RefreshDisplay()
    {
        Data data = GetInternalData<Data>();
        base.DynamicVars["TrackedCount"].BaseValue = data.TrackedCards.Count;
        ((StringVar)base.DynamicVars["TrackedCards"]).StringValue = data.TrackedCards.Count == 0
            ? "无"
            : string.Join("\n", data.TrackedCards.Select(card => $"• {card.Title}"));
        InvokeDisplayAmountChanged();
    }

    private async Task RefreshAfterTrackedCardsChanged()
    {
        CleanupTrackedCards();
        RefreshDisplay();

        if (GetInternalData<Data>().TrackedCards.Count == 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
