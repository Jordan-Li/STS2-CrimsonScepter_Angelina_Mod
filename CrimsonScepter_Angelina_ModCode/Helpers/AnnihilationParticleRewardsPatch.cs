using System.Linq;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

/// <summary>
/// 奖励生成补丁：
/// 若本场战斗曾被湮灭粒子按 Fatal 规则斩杀过目标，则移除战斗后的卡牌奖励。
/// </summary>
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
internal static class AnnihilationParticleRewardsPatch
{
    private static void Postfix(RewardsSet __instance, AbstractRoom room)
    {
        // 只处理战斗房间；非战斗奖励不受影响。
        if (room is not CombatRoom combatRoom)
        {
            return;
        }

        // 只有这场战斗里确实记录过“无卡牌奖励”标记时，才移除卡牌奖励。
        if (!AnnihilationParticle.ConsumePendingNoCardReward(combatRoom, __instance.Player.NetId))
        {
            return;
        }

        foreach (Reward reward in __instance.Rewards.Where(static reward => reward is CardReward).ToList())
        {
            __instance.Rewards.Remove(reward);
        }
    }
}