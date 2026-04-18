using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：重力结界
/// 效果：本回合内，当来自敌方的伤害完全被格挡时，对伤害来源施加失衡。
/// </summary>
public sealed class GravityBarrierPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：失衡。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 只有自身受到了来自敌方的伤害时，才继续检查完全格挡的反制效果。
        if (target != base.Owner || dealer == null || dealer.Side == base.Owner.Side)
        {
            return;
        }

        // 必须是这次伤害被完全格挡，且原始伤害大于 0。
        if (!result.WasFullyBlocked || result.TotalDamage <= 0)
        {
            return;
        }

        // 触发结界反制，对伤害来源施加失衡。
        Flash();
        await PowerCmd.Apply<ImbalancePower>(dealer, base.Amount, base.Owner, cardSource);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 回合结束后移除此效果，使其只在本回合内生效。
        if (base.Owner.Side != side)
        {
            await PowerCmd.Remove(this);
        }
    }
}