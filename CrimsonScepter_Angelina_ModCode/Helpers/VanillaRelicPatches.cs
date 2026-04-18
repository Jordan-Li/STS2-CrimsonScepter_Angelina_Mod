using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

/// <summary>
/// 兼容原版特定遗物对安洁莉娜的特殊处理：
/// 1. Touch of Orobas 会把绯红权杖升级为秘杖·绯红权杖。
/// 2. Archaic Tooth 会把起始卡“反重力”替换成“秘杖·反重力模式”。
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
internal static class AngelinaTouchOfOrobasUpgradePatch
{
    // 原版尝试升级安洁莉娜的初始遗物时，改为返回隐藏版秘杖。
    private static bool Prefix(RelicModel starterRelic, ref RelicModel __result)
    {
        if (starterRelic.Id != ModelDb.Relic<CrimsonScepter>().Id)
        {
            return true;
        }

        __result = ModelDb.Relic<SecretCrimsonScepter>();
        return false;
    }
}

[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer))]
internal static class AngelinaArchaicToothSetupPatch
{
    private static readonly MethodInfo StarterCardSetter =
        AccessTools.PropertySetter(typeof(ArchaicTooth), nameof(ArchaicTooth.StarterCard))!;

    private static readonly MethodInfo AncientCardSetter =
        AccessTools.PropertySetter(typeof(ArchaicTooth), nameof(ArchaicTooth.AncientCard))!;

    // 让原版远古之牙识别安洁莉娜的起始卡，并准备好对应的远古替代卡。
    private static bool Prefix(ArchaicTooth __instance, Player player, ref bool __result)
    {
        if (!TryGetAngelinaArchaicCards(player, out CardModel starterCard, out CardModel ancientCard))
        {
            return true;
        }

        StarterCardSetter.Invoke(__instance, [starterCard.ToSerializable()]);
        AncientCardSetter.Invoke(__instance, [ancientCard.ToSerializable()]);
        __result = true;
        return false;
    }

    private static bool TryGetAngelinaArchaicCards(Player player, out CardModel starterCard, out CardModel ancientCard)
    {
        starterCard = player.Deck.Cards.FirstOrDefault(card => card.Id == ModelDb.Card<AntiGravity>().Id)!;
        if (starterCard == null)
        {
            ancientCard = null!;
            return false;
        }

        ancientCard = CreateUpgradedAncientCard(starterCard);
        return true;
    }

    internal static CardModel CreateUpgradedAncientCard(CardModel starterCard)
    {
        Player owner = starterCard.Owner
            ?? throw new System.InvalidOperationException("AntiGravity should have an owner when transformed by Archaic Tooth.");
        var runState = owner.RunState
            ?? throw new System.InvalidOperationException("Owner should have a run state when transformed by Archaic Tooth.");

        CardModel ancientCard = runState.CreateCard<ScepterAntigravityMode>(owner);

        if (starterCard.IsUpgraded)
        {
            CardCmd.Upgrade(ancientCard);
        }

        if (starterCard.Enchantment != null)
        {
            EnchantmentModel enchantment = (EnchantmentModel)starterCard.Enchantment.MutableClone();
            CardCmd.Enchant(enchantment, ancientCard, enchantment.Amount);
        }

        return ancientCard;
    }
}

[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained))]
internal static class AngelinaArchaicToothObtainPatch
{
    // 真正获得远古之牙后，把牌组里的反重力替换成对应的秘杖版本。
    private static bool Prefix(ArchaicTooth __instance, ref Task __result)
    {
        CardModel? starterCard = __instance.Owner.Deck.Cards
            .FirstOrDefault(card => card.Id == ModelDb.Card<AntiGravity>().Id);

        if (starterCard == null)
        {
            return true;
        }

        __result = CardCmd.Transform(
            starterCard,
            AngelinaArchaicToothSetupPatch.CreateUpgradedAncientCard(starterCard));
        return false;
    }
}
