using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Enchantments;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;

public sealed class ExpressLabel : AngelinaRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        ..HoverTipFactory.FromEnchantment<ExpressEnchantment>(1),
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    public override async Task AfterObtained()
    {
        EnchantmentModel enchantment = ModelDb.Enchantment<ExpressEnchantment>();
        List<CardModel> eligibleCards = PileType.Deck
            .GetPile(base.Owner)
            .Cards
            .Where(enchantment.CanEnchant)
            .ToList();

        if (eligibleCards.Count == 0)
        {
            return;
        }

        int selectCount = System.Math.Min(base.DynamicVars.Cards.IntValue, eligibleCards.Count);
        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromDeckForEnchantment(
            player: base.Owner,
            enchantment: enchantment,
            amount: 1,
            additionalFilter: card => card != null && enchantment.CanEnchant(card),
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, selectCount));

        foreach (CardModel selectedCard in selectedCards)
        {
            CardCmd.Enchant<ExpressEnchantment>(selectedCard, 1m);

            NCardEnchantVfx? enchantVfx = NCardEnchantVfx.Create(selectedCard);
            if (enchantVfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(enchantVfx);
            }
        }
    }
}
