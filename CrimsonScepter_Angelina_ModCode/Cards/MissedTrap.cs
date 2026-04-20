using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：落空陷阱
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：攻击
/// 效果：造成2点伤害2次。若使敌方脱离浮空，获得1点能量。
/// 升级后效果：造成3点伤害2次。若使敌方脱离浮空，获得2点能量。
/// </summary>
public sealed class MissedTrap : AngelinaCard
{
    // 当前这次命中正在跟踪的目标。
    private MegaCrit.Sts2.Core.Entities.Creatures.Creature? _pendingGroundedTarget;

    // 这次命中是否需要监听“脱离浮空”。
    private bool _pendingGroundedCheck;

    // 这次命中是否已经确认打掉了浮空。
    private bool _groundedByCurrentHit;

    // 动态变量：
    // 1. 单段伤害
    // 2. 脱离浮空后获得的能量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2m, ValueProp.Move),
        new EnergyVar("EnergyGain", 1)
    ];

    // 额外悬浮说明：
    // 1. 飞行
    // 2. 浮空
    // 3. 能量
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FlyPower>(),
        new HoverTip(
            new LocString("powers", "AIRBORNE.title"),
            new LocString("powers", "AIRBORNE.description")),
        HoverTipFactory.ForEnergy(this)
    ];

    // 初始化卡牌的基础信息：0费、攻击、罕见、目标为单体敌人。
    public MissedTrap()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时，对目标造成两次伤害；若目标在某次伤害后脱离浮空，则获得能量。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        bool grantedEnergy = false;

        // 依次结算两次攻击。
        for (int i = 0; i < 2; i++)
        {
            PrepareGroundedCheck(cardPlay.Target, grantedEnergy);

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 只在本次命中实际打掉浮空时回能，避免多人中因异步结算后轮询状态而分歧。
            if (!grantedEnergy && _groundedByCurrentHit)
            {
                await PlayerCmd.GainEnergy(base.DynamicVars["EnergyGain"].BaseValue, base.Owner);
                grantedEnergy = true;
            }

            ResetGroundedCheck();

            // 若目标已经死亡，则不再继续后续攻击。
            if (!cardPlay.Target.IsAlive)
            {
                break;
            }
        }
    }

    // 升级后提高单段伤害和回能数量。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
        base.DynamicVars["EnergyGain"].UpgradeValueBy(1m);
    }

    public override Task AfterPowerAmountChanged(MegaCrit.Sts2.Core.Models.PowerModel power, decimal amount, MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        _ = applier;

        // 这里不能再要求 cardSource == this。
        // 落空陷阱打掉飞行时，实际触发的是 FlyPower.AfterDamageReceived -> PowerCmd.Decrement(this)，
        // 这条链里的 cardSource 不是当前卡牌本身；如果卡死要求 cardSource == this，就会导致
        // “确实把目标打落地了，但不回能”。
        if (!_pendingGroundedCheck || TemporaryFlyPower.IsResolvingExpiration)
        {
            return Task.CompletedTask;
        }

        if (power.Owner != _pendingGroundedTarget || !AirborneHelper.IsAirbornePower(power))
        {
            return Task.CompletedTask;
        }

        if (!AirborneHelper.BecameGrounded(power, amount))
        {
            return Task.CompletedTask;
        }

        _groundedByCurrentHit = true;
        _pendingGroundedCheck = false;
        return Task.CompletedTask;
    }

    private void PrepareGroundedCheck(MegaCrit.Sts2.Core.Entities.Creatures.Creature target, bool grantedEnergy)
    {
        _pendingGroundedTarget = target;
        _groundedByCurrentHit = false;
        _pendingGroundedCheck = !grantedEnergy && target.IsAlive && !TemporaryFlyPower.IsResolvingExpiration && AirborneHelper.IsAirborne(target);
    }

    private void ResetGroundedCheck()
    {
        _pendingGroundedTarget = null;
        _pendingGroundedCheck = false;
        _groundedByCurrentHit = false;
    }
}
