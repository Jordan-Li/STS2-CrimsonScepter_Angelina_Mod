using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

/// <summary>
/// The built-in STUNNED move uses MustPerformOnceBeforeTransitioning, which can block
/// both entering stun from certain monster states and leaving stun after death/revive.
/// If game code explicitly calls CreatureCmd.Stun, we want that stun to actually stick.
/// Likewise, once the owner is dead, death/revive states must still be able to override
/// STUNNED immediately.
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
