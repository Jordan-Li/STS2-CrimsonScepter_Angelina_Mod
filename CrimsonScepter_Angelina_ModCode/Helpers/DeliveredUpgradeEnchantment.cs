using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

/// <summary>
/// 附魔名：送达升级
/// 附魔效果：当此牌因寄送回到手牌时，使当前战斗中的这张牌获得升级，然后移除此附魔。
/// 额外卡面文本：送达时，本场战斗中升级
/// </summary>
public sealed class DeliveredUpgradeEnchantment : EnchantmentModel
{
    // 这类附魔需要在卡面上显示额外文本。
    public override bool HasExtraCardText => true;

    // 不额外启用金色高亮，避免和普通重要提示混在一起。
    public override bool ShouldGlowGold => false;

    // 额外悬浮说明：补一条“送达”的通用提示。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(new LocString("cards", "DELIVERED.title"), new LocString("cards", "DELIVERED.description"))
    ];

    // 当卡牌换堆时，检查是否是因为寄送而从 Exhaust 回到手牌。
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        // 只有这张附魔挂载的卡、从 Exhaust 返回手牌、且来源是寄送时，才触发升级。
        if (card != Card ||
            oldPileType != PileType.Exhaust ||
            card.Pile?.Type != PileType.Hand ||
            source is not DeliveryPower)
        {
            return Task.CompletedTask;
        }

        // 若此牌仍可升级，则让当前战斗中的这张卡在送达时获得升级。
        // 战斗中的手牌本来就是牌库牌的战斗实例，这里不会把牌库版本永久升级。
        if (card.IsUpgradable)
        {
            CardCmd.Upgrade(card);
        }

        // 升级结算完成后，移除此附魔，避免重复触发。
        CardCmd.ClearEnchantment(card);
        return Task.CompletedTask;
    }
}