using System.Linq;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

public static class AirborneHelper
{
    /// <summary>
    /// 浮空状态的统一判断入口。
    /// 当前把“飞行”和旧实现里出现过的“FlutterPower”都视为浮空来源。
    /// </summary>
    public static bool IsAirborne(Creature? target)
    {
        if (target == null)
        {
            return false;
        }

        return HasPositiveFly(target) || HasPositiveFlutter(target);
    }

    /// <summary>
    /// 在只关心 FlyPower 变化时，判断目标是否因为这次变化而落地。
    /// 该方法保留给现有只基于 FlyPower 的调用点使用。
    /// </summary>
    public static bool BecameGroundedByFlyChange(Creature? owner, decimal amountDelta)
    {
        if (owner == null || amountDelta >= 0m)
        {
            return false;
        }

        return !IsAirborne(owner);
    }

    /// <summary>
    /// 判断某个 Power 是否属于“浮空来源”。
    /// 这里兼容当前项目里的 FlyPower，以及旧版语义里使用过的 FlutterPower。
    /// </summary>
    public static bool IsAirbornePower(PowerModel power)
    {
        return power is FlyPower || power.GetType().Name == "FlutterPower";
    }

    /// <summary>
    /// 判断一次 Power 层数变化是否导致目标从“浮空”变为“非浮空”。
    /// 这比单纯检查 FlyPower 是否减少更准确，因为目标可能还有其他浮空来源。
    /// </summary>
    public static bool BecameGrounded(PowerModel power, decimal amount)
    {
        if (!IsAirbornePower(power) || power.Owner == null)
        {
            return false;
        }

        if (power is FlyPower flyPower)
        {
            decimal previousFlyAmount = flyPower.Amount - amount;
            if (previousFlyAmount > 0m && flyPower.Amount < 0m)
            {
                return false;
            }
        }

        bool wasAirborne = WasAirborneBeforeChange(power, amount);
        bool isAirborneNow = IsAirborne(power.Owner);
        return wasAirborne && !isAirborneNow;
    }

    /// <summary>
    /// 根据当前值和本次变动量，反推目标在变化前是否处于浮空。
    /// </summary>
    private static bool WasAirborneBeforeChange(PowerModel power, decimal amount)
    {
        Creature owner = power.Owner!;
        bool hadOtherAirborne = IsAirborneExcluding(owner, power);

        if (power is FlyPower flyPower)
        {
            return hadOtherAirborne || flyPower.Amount - amount > 0m;
        }

        return hadOtherAirborne || power.Amount - amount > 0m;
    }

    /// <summary>
    /// 判断排除当前这个 Power 后，目标是否仍然有其他浮空来源。
    /// </summary>
    private static bool IsAirborneExcluding(Creature creature, PowerModel excludedPower)
    {
        if (excludedPower is not FlyPower && HasPositiveFly(creature))
        {
            return true;
        }

        if (excludedPower.GetType().Name != "FlutterPower" && HasPositiveFlutter(creature))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判断目标当前是否有正层数的飞行。
    /// </summary>
    private static bool HasPositiveFly(Creature target)
    {
        return (target.GetPower<FlyPower>()?.Amount ?? 0m) > 0m;
    }

    /// <summary>
    /// 判断目标当前是否有正层数的 FlutterPower。
    /// 这里按类型名匹配，是为了兼容项目当前没有直接引用该类型的情况。
    /// </summary>
    private static bool HasPositiveFlutter(Creature target)
    {
        return target.Powers.Any(power => power.GetType().Name == "FlutterPower" && power.Amount > 0m);
    }
}
