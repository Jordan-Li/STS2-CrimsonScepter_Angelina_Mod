using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using Godot;
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
/// 遗物名：秘杖·绯红权杖
/// 稀有度：初始（隐藏/特殊用）
/// 效果：
/// 1. 当敌人从浮空状态因受击脱离浮空时，施加15点失衡。
/// 2. 每回合开始时，给予所有单位1层临时飞行。
/// </summary>
public sealed class SecretCrimsonScepter : AngelinaRelic
{
    private readonly HashSet<Creature> _pendingGroundedChecks = [];

    // 当前项目图片文件名还是旧 typo：sercet_secrimson_scepter.png
    public override string PackedIconPath
    {
        get
        {
            string typoPath = "sercet_secrimson_scepter.png".RelicImagePath();
            return ResourceLoader.Exists(typoPath) ? typoPath : base.PackedIconPath;
        }
    }

    protected override string BigIconPath
    {
        get
        {
            string typoPath = "sercet_secrimson_scepter.png".BigRelicImagePath();
            return ResourceLoader.Exists(typoPath) ? typoPath : base.BigIconPath;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(15m),
        new PowerVar<TemporaryFlyPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<TemporaryFlyPower>(),
        HoverTipFactory.FromPower<FlyPower>(),
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
        if (target.Side == base.Owner.Creature.Side || !target.IsAlive || AirborneHelper.IsAirborne(target))
        {
            return;
        }

        Flash([target]);
        await PowerCmd.Apply<ImbalancePower>(
            target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            null
        );
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        _ = choiceContext;

        if (side != base.Owner.Creature.Side)
        {
            return;
        }

        List<Creature> creatures = combatState.Creatures
            .Where(creature => creature.IsAlive)
            .ToList();

        if (creatures.Count == 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<TemporaryFlyPower>(
            creatures,
            base.DynamicVars["TemporaryFlyPower"].BaseValue,
            base.Owner.Creature,
            null
        );
    }

    private static bool IsPoweredAttack(ValueProp props)
    {
        return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
    }
}
