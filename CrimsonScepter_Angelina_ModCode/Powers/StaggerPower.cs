using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：StaggerPower
/// 效果：目标受到的伤害提高10%/层；在其回合结束时移除。
/// </summary>
public sealed class StaggerPower : AngelinaPower
{
    private const string PercentKey = "Percent";

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override int DisplayAmount => base.Amount * 10;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(PercentKey, 10m)
    ];

    // 迟滞会提高目标承受的伤害倍率
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner)
        {
            return 1m;
        }

        return 1m + 0.1m * base.Amount;
    }

    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            RefreshDisplay();
        }

        return Task.CompletedTask;
    }

    // 到拥有者自己回合结束时移除
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }

    private void RefreshDisplay()
    {
        base.DynamicVars[PercentKey].BaseValue = base.Amount * 10m;
        InvokeDisplayAmountChanged();
    }
}
