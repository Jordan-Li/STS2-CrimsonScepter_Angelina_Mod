using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;

/// <summary>
/// 遗物名：绯红权杖
/// 稀有度：初始
/// 效果：当敌方角色从浮空状态因受击脱离浮空时，施加10点失衡。
/// </summary>
public sealed class CrimsonScepter : AngelinaRelic
{
    private readonly HashSet<Creature> _pendingGroundedChecks = [];

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ImbalancePower>(10m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FlyPower>(),
        HoverTipFactory.FromPower<ImbalancePower>(),
        new HoverTip(
            new LocString("powers", "AIRBORNE.title"),
            new LocString("powers", "AIRBORNE.description"))
    ];

    /// <summary>
    /// 只有敌方真正吃到一次有威力攻击、且受击前处于浮空时，
    /// 才记录这次攻击后是否因受击脱离浮空。
    /// </summary>
    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _ = choiceContext;
        _ = dealer;
        _ = cardSource;

        if (target.Side == base.Owner.Creature.Side || result.UnblockedDamage <= 0 || !IsPoweredAttack(props))
        {
            return Task.CompletedTask;
        }

        if (TemporaryFlyPower.IsResolvingExpiration || !AirborneHelper.IsAirborne(target))
        {
            return Task.CompletedTask;
        }

        _pendingGroundedChecks.Add(target);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 若这次受击导致目标从浮空变为非浮空，则施加失衡。
    /// 这里只响应本次受击后紧接着发生的浮空相关 Power 变化。
    /// </summary>
    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        _ = applier;

        if (!CombatManager.Instance.IsInProgress || TemporaryFlyPower.IsResolvingExpiration || amount >= 0m)
        {
            return;
        }

        Creature? target = power.Owner;
        if (target == null || !_pendingGroundedChecks.Contains(target))
        {
            return;
        }

        if (!AirborneHelper.IsAirbornePower(power))
        {
            return;
        }

        _pendingGroundedChecks.Remove(target);
        if (target.Side == base.Owner.Creature.Side || AirborneHelper.IsAirborne(target))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<ImbalancePower>(
            target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            cardSource
        );
    }

    private static bool IsPoweredAttack(ValueProp props)
    {
        return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
    }
}
