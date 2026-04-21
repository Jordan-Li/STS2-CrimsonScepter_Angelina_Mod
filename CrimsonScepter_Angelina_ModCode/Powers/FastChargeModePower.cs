using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：FastChargeModePower
/// 效果：本回合内，你施加的正向失衡值翻倍；回合结束后移除。
/// </summary>
public sealed class FastChargeModePower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：失衡。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    // 当拥有者给目标施加失衡时，把正向数值翻倍
    public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (power is not ImbalancePower || giver != base.Owner || amount <= 0m)
        {
            return amount;
        }

        Flash();
        return amount * 2m;
    }

    // 持续到己方回合结束
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            if (base.Amount <= 1m)
            {
                await PowerCmd.Remove(this);
                return;
            }

            await PowerCmd.Apply<FastChargeModePower>(base.Owner, -1m, base.Owner, null);
        }
    }
}
