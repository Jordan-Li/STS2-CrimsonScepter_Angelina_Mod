using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

public sealed class ChainTechniqueTemporaryFocusPower : AngelinaPower, ITemporaryPower
{
    private bool shouldIgnoreNextInstance;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public AbstractModel OriginModel => ModelDb.Card<ChainTechnique>();

    public PowerModel InternallyAppliedPower => ModelDb.Power<FocusPower>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard((CardModel)OriginModel),
        HoverTipFactory.FromPower<FocusPower>()
    ];

    public void IgnoreNextInstance()
    {
        shouldIgnoreNextInstance = true;
    }

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (shouldIgnoreNextInstance)
        {
            shouldIgnoreNextInstance = false;
            return;
        }

        await PowerCmd.Apply<FocusPower>(target, amount, applier, cardSource, silent: true);
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this || amount == base.Amount)
        {
            return;
        }

        if (shouldIgnoreNextInstance)
        {
            shouldIgnoreNextInstance = false;
            return;
        }

        await PowerCmd.Apply<FocusPower>(base.Owner, amount, applier, cardSource, silent: true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Side)
        {
            return;
        }

        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<FocusPower>(base.Owner, -base.Amount, base.Owner, null);
    }
}
