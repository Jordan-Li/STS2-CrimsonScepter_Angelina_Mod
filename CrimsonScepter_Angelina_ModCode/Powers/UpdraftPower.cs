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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：上升气流
/// 效果：
/// 1. 自身回合开始时，获得等量临时飞行
/// 2. 若自身因受到攻击而脱离浮空，向抽牌堆加入一张眩晕
/// </summary>
public sealed class UpdraftPower : AngelinaPower
{
    private sealed class Data
    {
        public bool PendingAttackGroundedCheck;
        public bool WasAirborneBeforeAttack;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：临时飞行与眩晕。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TemporaryFlyPower>(),
        HoverTipFactory.FromCard<Dazed>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        // 仅在自身回合开始时，给予等量临时飞行。
        if (side != base.Owner.Side)
        {
            return;
        }

        await PowerCmd.Apply<TemporaryFlyPower>(base.Owner, base.Amount, base.Owner, null);
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 只在自身受到带威力的攻击且出现未被格挡伤害时，才检查是否因此脱离浮空。
        if (target != base.Owner || result.UnblockedDamage <= 0 || !IsPoweredAttack(props))
        {
            return Task.CompletedTask;
        }

        // 临时飞行自然到期时不应触发“受击脱离浮空”。
        if (TemporaryFlyPower.IsResolvingExpiration)
        {
            return Task.CompletedTask;
        }

        // 只有受击前处于浮空时，才需要记录后续是否脱离浮空。
        if (!AirborneHelper.IsAirborne(base.Owner))
        {
            return Task.CompletedTask;
        }

        Data data = GetInternalData<Data>();
        data.PendingAttackGroundedCheck = true;
        data.WasAirborneBeforeAttack = true;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();
        if (!data.PendingAttackGroundedCheck)
        {
            return;
        }

        if (!CombatManager.Instance.IsInProgress || TemporaryFlyPower.IsResolvingExpiration)
        {
            data.PendingAttackGroundedCheck = false;
            data.WasAirborneBeforeAttack = false;
            return;
        }

        // 只在自身的浮空相关状态变化后继续检查，避免被无关 Power 变化误触发。
        if (power.Owner != base.Owner || !IsAirbornePower(power))
        {
            return;
        }

        // 若受击前处于浮空，且当前已经不再浮空，则视为因本次攻击脱离浮空。
        if (!data.WasAirborneBeforeAttack || AirborneHelper.IsAirborne(base.Owner))
        {
            return;
        }

        data.PendingAttackGroundedCheck = false;
        data.WasAirborneBeforeAttack = false;
        if (base.Owner.Player == null)
        {
            return;
        }

        // 脱离浮空后，向抽牌堆中加入一张眩晕并闪烁提示。
        CardModel dazed = base.CombatState.CreateCard<Dazed>(base.Owner.Player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Draw, addedByPlayer: true));
        Flash();
    }

    private static bool IsAirbornePower(PowerModel power)
    {
        return power is FlyPower || power.GetType().Name == "FlutterPower";
    }

    private static bool IsPoweredAttack(ValueProp props)
    {
        return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
    }
}
