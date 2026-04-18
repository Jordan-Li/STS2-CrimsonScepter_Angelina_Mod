using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：信使训练
/// 效果：每回合开始时，获得飞行。
/// </summary>
public sealed class CourierTrainingPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 在自身每回合开始时，给予等量飞行。
    public override async Task AfterEnergyReset(Player player)
    {
        // 只在自身的回合开始时触发。
        if (player != base.Owner.Player)
        {
            return;
        }

        // 播放闪光提示，然后施加飞行。
        Flash();
        await PowerCmd.Apply<FlyPower>(base.Owner, base.Amount, base.Owner, null);
    }
}