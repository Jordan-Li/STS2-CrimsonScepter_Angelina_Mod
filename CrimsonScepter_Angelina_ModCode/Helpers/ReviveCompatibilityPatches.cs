using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

/// <summary>
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetMoveImmediate))]
internal static class ReviveCompatibilityPatches
{
    private static void Prefix(MonsterModel __instance, MoveState state, ref bool forceTransition)
    {
        if (forceTransition)
        {
            return;
        }

        if (state.Id == "STUNNED")
        {
            forceTransition = true;
            return;
        }

        if (__instance.Creature.IsDead)
        {
            forceTransition = true;
        }
    }
}
