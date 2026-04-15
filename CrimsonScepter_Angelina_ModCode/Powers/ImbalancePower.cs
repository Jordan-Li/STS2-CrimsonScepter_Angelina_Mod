using System;
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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：失衡
/// Power类型：计数型Power
/// 效果：累计失衡层数，达到阈值后触发失重
/// 触发规则：
/// 1. 阈值 = 目标最大生命值的一半，最多按100计算
/// 2. 触发后移除当前失衡层数
/// 3. 目标进入持续3回合的“失重”状态
/// 4. 目标失去当前失衡层数一半的生命
/// 5. 如果目标正处于自己的回合：
///    - 玩家：直接结束回合
///    - 敌人：直接击晕
/// </summary>
public sealed class ImbalancePower : AngelinaPower
{
    // 内部数据：防止一次失衡触发尚未结算完成时重复进入
    private sealed class Data
    {
        public bool IsResolvingTrigger;
    }

    // 失衡触发阈值最多按100计算
    private const int MaxThreshold = 100;

    // 失重状态持续3回合
    private const int WeightlessDuration = 3;

    // 当前先按旧工程逻辑，显示为Buff
    public override PowerType Type => PowerType.Buff;

    // 这是一个计数型Power
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 定义一个额外动态变量：
    // RemainingToTrigger = 距离触发失重还差多少层
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("RemainingToTrigger", 0m)
    };

    // 鼠标悬浮时额外显示：
    // - 失重状态的HoverTip
    // - 击晕的HoverTip
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WeightlessPower>(),
        HoverTipFactory.Static(StaticHoverTip.Stun)
    };

    // 计算失衡触发阈值：
    // 最大生命值的一半，向上取整，最多100
    private int TriggerThreshold
    {
        get
        {
            decimal halfMaxHp = Math.Ceiling(base.Owner.MaxHp / 2m);
            return (int)Math.Min(MaxThreshold, halfMaxHp);
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 当失衡第一次被施加后：
    // 1. 先规范化数值
    // 2. 刷新“还差多少层触发”
    // 3. 尝试触发失重
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (await NormalizeAmount())
        {
            return;
        }

        RefreshSmartVars();
        await TryTriggerWeightless(applier, cardSource);
    }

    // 当失衡层数变化时：
    // 1. 只处理自己这层Power的数值变化
    // 2. 先规范化数值
    // 3. 刷新“还差多少层触发”
    // 4. 只有在数值增加时，才尝试触发失重
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

    // 规范化当前失衡层数：
    // - 小于0时修正为0
    // - 小于等于0时直接移除这层Power
    private async Task<bool> NormalizeAmount()
    {
        if (base.Amount < 0)
        {
            base.Amount = 0;
        }

        if (base.Amount <= 0)
        {
            await PowerCmd.Remove(this);
            return true;
        }

        return false;
    }

    // 刷新 DynamicVars 中的 RemainingToTrigger
    private void RefreshSmartVars()
    {
        base.DynamicVars["RemainingToTrigger"].BaseValue = Math.Max(0m, TriggerThreshold - base.Amount);
    }

    // 真正的失重触发逻辑
    private async Task TryTriggerWeightless(Creature? applier, CardModel? cardSource)
    {
        Data data = GetInternalData<Data>();

        // 如果正在结算，或者当前层数还没达到阈值，就不触发
        if (data.IsResolvingTrigger || base.Amount < TriggerThreshold)
        {
            return;
        }

        data.IsResolvingTrigger = true;

        try
        {
            // 记录本次触发时的失衡层数
            decimal triggeredAmount = base.Amount;

            // 计算本次触发后要失去的生命值 = 当前失衡层数的一半
            decimal hpLoss = triggeredAmount / 2m;

            Flash();

            // 移除当前这层失衡
            await PowerCmd.Remove(this);

            // 给目标施加持续3回合的“失重”状态
            await PowerCmd.Apply<WeightlessPower>(base.Owner, WeightlessDuration, applier ?? base.Owner, cardSource);

            if (!base.Owner.IsAlive)
            {
                return;
            }

            // 让目标失去生命：
            // - 不可格挡
            // - 不吃额外Power修正
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

            // 如果目标正处于自己的回合：
            // - 玩家直接结束回合
            // - 敌人直接击晕
            if (base.Owner.IsPlayer)
            {
                if (base.CombatState.CurrentSide == base.Owner.Side)
                {
                    if (base.Owner.Player != null)
                    {
                        PlayerCmd.EndTurn(base.Owner.Player, canBackOut: false);
                    }
                }
            }
            else
            {
                await CreatureCmd.Stun(base.Owner);
            }
        }
        finally
        {
            data.IsResolvingTrigger = false;
        }
    }
}