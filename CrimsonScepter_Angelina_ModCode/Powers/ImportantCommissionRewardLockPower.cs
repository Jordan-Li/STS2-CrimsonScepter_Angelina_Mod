using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：重要委托奖励锁
/// Power类型：状态型Power
/// 效果：标记本场战斗已经通过重要委托领取过随机遗物，防止重复领奖。
/// </summary>
public sealed class ImportantCommissionRewardLockPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;
}