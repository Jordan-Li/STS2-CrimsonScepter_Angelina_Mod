using System;
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
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：失衡
/// Power类型：计数型Power
/// 效果：
/// 1. 累积失衡层数
/// 2. 达到阈值后触发失重
/// 3. 触发时移除当前失衡并造成一次生命损失
/// 4. 若目标正处于自己的回合，还会追加回合打断效果
/// 备注：这是安洁莉娜整套体系的核心异常状态，不能删
/// </summary>
public sealed class ImbalancePower : AngelinaPower
{
    /// <summary>
    /// 防止一次触发尚未结算完成时重复进入。
    /// </summary>
    private sealed class Data
    {
        public bool IsResolvingTrigger;
    }

    private const int MaxThreshold = 100;
    private const int WeightlessDuration = 3;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => false;

    /// <summary>
    /// RemainingToTrigger 用于显示离触发失重还差多少层。
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("RemainingToTrigger", 0m)];

    /// <summary>
    /// 顺带提示失重和击晕，方便读懂触发后果。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeightlessPower>(), HoverTipFactory.Static(StaticHoverTip.Stun)];

    /// <summary>
    /// 失衡阈值 = 最大生命值的一半，向上取整，且最多按 100 计算。
    /// </summary>
    private int TriggerThreshold => (int)Math.Min(MaxThreshold, Math.Ceiling(base.Owner.MaxHp / 2m));

    protected override object InitInternalData()
    {
        return new Data();
    }

    /// <summary>
    /// 初次施加后立即刷新说明，并检查是否直接触发失重。
    /// </summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (await NormalizeAmount())
        {
            return;
        }

        RefreshSmartVars();
        await TryTriggerWeightless(applier, cardSource);
    }

    /// <summary>
    /// 层数变化后刷新说明；只有在层数增加时才尝试触发。
    /// </summary>
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this)
        {
            return;
        }

        if (await NormalizeAmount())
        {
            return;
        }

        RefreshSmartVars();

        if (amount > 0m)
        {
            await TryTriggerWeightless(applier, cardSource);
        }
    }

    /// <summary>
    /// 失衡层数降到 0 或以下时直接移除。
    /// </summary>
    private async Task<bool> NormalizeAmount()
    {
        if (base.Amount <= 0)
        {
            await PowerCmd.Remove(this);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 刷新离下次触发还差多少层。
    /// </summary>
    private void RefreshSmartVars()
    {
        base.DynamicVars["RemainingToTrigger"].BaseValue = Math.Max(0m, TriggerThreshold - base.Amount);
    }

    /// <summary>
    /// 达到阈值后结算失重：
    /// 1. 清空当前失衡
    /// 2. 施加失重
    /// 3. 按当前失衡一半造成生命损失
    /// 4. 若目标正处于自己的回合，则追加打断
    /// </summary>
    private async Task TryTriggerWeightless(Creature? applier, CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();

        if (data.IsResolvingTrigger || base.Amount < TriggerThreshold)
        {
            return;
        }

        data.IsResolvingTrigger = true;

        try
        {
            decimal triggeredAmount = base.Amount;

            decimal hpLoss = triggeredAmount / 2m;

            Flash();

            await PowerCmd.Remove(this);

            await PowerCmd.Apply<WeightlessPower>(base.Owner, WeightlessDuration, applier ?? base.Owner, cardSource);

            if (!base.Owner.IsAlive)
            {
                return;
            }

            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                base.Owner,
                hpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null
            );

            if (!base.Owner.IsAlive)
            {
                return;
            }

            await InterruptCurrentTurnIfNeeded();
        }
        finally
        {
            data.IsResolvingTrigger = false;
        }
    }

    /// <summary>
    /// 失衡触发后，若目标正处于自己的回合，则立刻打断其行动。
    /// </summary>
    private async Task InterruptCurrentTurnIfNeeded()
    {
        if (base.Owner.IsPlayer)
        {
            if (base.CombatState.CurrentSide == base.Owner.Side && base.Owner.Player != null)
            {
                PlayerCmd.EndTurn(base.Owner.Player, canBackOut: false);
            }

            return;
        }

        // 寄生惧魔的幻象复活依赖自身的 REVIVE_MOVE。
        // 这里不能走原版 CreatureCmd.Stun，但可以在存活状态下安全地强制插入一个 STUNNED move，
        // 并保留它当前动作作为后续恢复点。
        if (base.Owner.Monster is Parafright)
        {
            IllusionPower? illusionPower = base.Owner.GetPower<IllusionPower>();
            if (illusionPower?.IsReviving == true)
            {
                return;
            }

            await StunHelper.ForceStun(base.Owner);
            return;
        }

        // 千足虫的重组依赖自身的 DEAD_MOVE -> REATTACH_MOVE。
        // 这里同样不能走原版 CreatureCmd.Stun，但可以在存活状态下插入一个 STUNNED move，
        // 并显式保留当前 NextMove 作为后续恢复点。
        if (base.Owner.Monster is DecimillipedeSegment decimillipedeSegment)
        {
            string? nextMoveId = decimillipedeSegment.NextMove?.Id;
            await StunHelper.ForceStun(base.Owner, nextMoveId);
            return;
        }

        // Some enemies switch into a special dead/revive move after death.
        // Putting them into the built-in STUNNED move prevents that transition
        // if they are killed later in the same action chain, leaving them in a
        // fake-dead state with stale intents.
        if (base.CombatState != null && !Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(base.CombatState, base.Owner))
        {
            return;
        }

        await CreatureCmd.Stun(base.Owner);
    }
}
