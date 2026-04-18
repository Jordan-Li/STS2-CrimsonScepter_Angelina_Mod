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
    private sealed class Data
    {
        // 本次被改写为“失去生命”的失衡累计量
        public decimal PendingRedirectedHpLoss;

        // 避免刚进入失重或刚结算转血时，把同一批伤害再次翻倍
        public bool IgnoreNextDamageMultiplier;
    }

    // 当前按旧工程逻辑仍显示为 Buff。
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    protected override object InitInternalData() => new Data();

    // 首次进入失重时，先忽略下一次伤害翻倍，避免和进入当帧的其他结算打架。
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        GetInternalData<Data>().IgnoreNextDamageMultiplier = true;
        return Task.CompletedTask;
    }

    // 失重期间再次获得失衡时，不再叠失衡，而是把数值改记成待失去生命。
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        _ = applier;
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

    // 把上一阶段累计的失衡改写成等量失去生命。
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

    // 失重状态下，目标受到的伤害翻倍。
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = amount;
        _ = props;
        _ = dealer;
        _ = cardSource;

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

    // 在拥有者所属阵营的回合开始时，持续时间减 1；归零时移除。
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        _ = choiceContext;
        _ = combatState;

        if (side != base.Owner.Side)
        {
            return;
        }

        if (base.Amount <= 1)
        {
            await PowerCmd.Remove(this);
            return;
        }

        await PowerCmd.Decrement(this);
    }
}
