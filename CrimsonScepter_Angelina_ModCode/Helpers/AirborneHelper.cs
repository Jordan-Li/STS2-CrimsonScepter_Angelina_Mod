using System.Linq;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

public static class AirborneHelper
{
    /// <summary>
    /// 浮空的统一判定入口。
    /// 把“飞行”和官方旧实现里使用过的“振翅（FlutterPower）”都视为浮空状态。
    /// </summary>
    public static bool IsAirborne(Creature? target)
    {
        if (target == null)
        {
            return false;
        }

        return HasPositiveFly(target) || HasPositiveFlutter(target);
    }

    public static bool BecameGroundedByFlyChange(Creature? owner, decimal amountDelta)
    {
        if (owner == null || amountDelta >= 0m)
        {
            return false;
        }

        return !IsAirborne(owner);
    }

    private static bool HasPositiveFly(Creature target)
    {
        return (target.GetPower<FlyPower>()?.Amount ?? 0m) > 0m;
    }

    private static bool HasPositiveFlutter(Creature target)
    {
        return target.Powers.Any(power => power.GetType().Name == "FlutterPower" && power.Amount > 0m);
    }
}