using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：引力模式
/// 效果：每回合首次造成法术伤害时，向目标施加等额的失衡值。
/// </summary>
public sealed class GravityModePower : AngelinaPower
{
    private sealed class Data
    {
        public bool HasTriggeredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        _ = choiceContext;
        _ = props;

        if (dealer != base.Owner || cardSource == null || !SpellHelper.IsSpell(cardSource))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.HasTriggeredThisTurn || result.UnblockedDamage <= 0)
        {
            return;
        }

        data.HasTriggeredThisTurn = true;
        Flash();
        await PowerCmd.Apply<ImbalancePower>(target, result.UnblockedDamage, base.Owner, cardSource);
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        _ = choiceContext;
        _ = combatState;

        if (side == base.Owner.Side)
        {
            GetInternalData<Data>().HasTriggeredThisTurn = false;
        }

        return Task.CompletedTask;
    }
}
