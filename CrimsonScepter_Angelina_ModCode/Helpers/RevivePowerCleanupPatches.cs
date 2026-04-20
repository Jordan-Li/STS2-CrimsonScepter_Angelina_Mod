using HarmonyLib;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

[HarmonyPatch(typeof(IllusionPower), nameof(IllusionPower.ShouldPowerBeRemovedOnDeath))]
internal static class IllusionPowerCleanupPatch
{
    private static void Postfix(PowerModel power, ref bool __result)
    {
        if (power is WeightlessPower)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(ReattachPower), nameof(ReattachPower.ShouldPowerBeRemovedOnDeath))]
internal static class ReattachPowerCleanupPatch
{
    private static void Postfix(PowerModel power, ref bool __result)
    {
        if (power is WeightlessPower)
        {
            __result = true;
        }
    }
}
