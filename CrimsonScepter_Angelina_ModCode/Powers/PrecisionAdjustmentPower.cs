using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：精密调整
/// 效果：下回合开始时，失去失衡。
/// </summary>
public sealed class PrecisionAdjustmentPower : AngelinaPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：失衡。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        // 仅在此 Power 所属单位的下回合开始时触发。
        if (side != base.Owner.Side)
        {
            return;
        }

        // 扣除固定数值的失衡，然后移除此延时 Power。
        Flash();
        await PowerCmd.Apply<ImbalancePower>(base.Owner, -base.Amount, base.Owner, null, silent: true);
        await PowerCmd.Remove(this);
    }
}