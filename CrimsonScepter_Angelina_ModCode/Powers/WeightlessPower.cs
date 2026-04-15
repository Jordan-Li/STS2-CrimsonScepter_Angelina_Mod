using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：失重
/// Power类型：状态型Power
/// 效果：
/// 1. 持续3回合
/// 2. 受到的伤害翻倍
/// 3. 期间再次获得失衡时，不再正常叠层，而是改为失去等量生命
/// </summary>
public sealed class WeightlessPower : AngelinaPower
{
    // 内部数据：
    // PendingRedirectedHpLoss = 本次被转成“失去生命”的失衡累计量
    // IgnoreNextDamageMultiplier = 避免刚进入失重状态时，某些结算被误判成翻倍伤害
    private sealed class Data
    {
        public decimal PendingRedirectedHpLoss;

        public bool IgnoreNextDamageMultiplier;
    }

    // 当前先按旧工程逻辑，显示为Buff
    public override PowerType Type => PowerType.Buff;

    // 这是一个计数型Power
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 当失重状态第一次被施加时：
    // 先忽略下一次伤害翻倍判定，避免和触发当帧的其他结算打架
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().IgnoreNextDamageMultiplier = true;
        return Task.CompletedTask;
    }

    // 当目标已经处于“失重”状态时，如果再次获得失衡：
    // 就不再正常叠失衡，而是把这部分量记录到待掉血数值中
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (target != base.Owner)
        {
            return false;
        }

        if (canonicalPower is not ImbalancePower || amount <= 0m)
        {
            return false;
        }

        GetInternalData<Data>().PendingRedirectedHpLoss += amount;
        modifiedAmount = 0m;
        return true;
    }

    // 在上面的“转向修改”完成后，
    // 把累计的失衡改成等量失去生命
    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        Data data = GetInternalData<Data>();

        if (power is not ImbalancePower || data.PendingRedirectedHpLoss <= 0m)
        {
            return;
        }

        decimal hpLoss = data.PendingRedirectedHpLoss;
        data.PendingRedirectedHpLoss = 0m;
        data.IgnoreNextDamageMultiplier = true;

        Flash();

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            base.Owner,
            hpLoss,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null
        );
    }

    // 失重状态下，目标受到的伤害翻倍
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner)
        {
            return 1m;
        }

        Data data = GetInternalData<Data>();

        if (data.IgnoreNextDamageMultiplier)
        {
            data.IgnoreNextDamageMultiplier = false;
            return 1m;
        }

        return 2m;
    }

    // 在拥有者所属阵营的回合开始时：
    // - 持续时间减少1
    // - 归0时移除这层Power
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Side)
        {
            return;
        }

        if (base.Amount <= 1)
        {
            await PowerCmd.Remove(this);
            return;
        }

        base.Amount -= 1;
    }
}