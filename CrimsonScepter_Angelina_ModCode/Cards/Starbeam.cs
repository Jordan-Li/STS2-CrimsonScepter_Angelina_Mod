using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：星芒
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：造成8点法术伤害。若目标处于浮空，再造成8点法术伤害。
/// 升级后效果：造成10点法术伤害。若目标处于浮空，再造成10点法术伤害。
/// </summary>
public sealed class Starbeam : AngelinaCard
{
    private Creature? _pendingGroundedTarget;

    private bool _pendingGroundedCheck;

    private bool _groundedByCurrentHit;

    // 额外悬浮说明：
    // 1. 浮空
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("powers", "AIRBORNE.title"),
            new LocString("powers", "AIRBORNE.description")),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：法术伤害。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Unpowered | ValueProp.Move),
        new CalculationBaseVar(8m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Unpowered | ValueProp.Move)
            .WithMultiplier(static (card, _) => card.Owner?.Creature?.GetPower<FocusPower>()?.Amount ?? 0m)
    ];

    // 初始化卡牌的基础信息：1费、攻击、普通、目标为单体敌人。
    public Starbeam()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时，先造成一次法术伤害；若目标处于浮空，再追加一次同样的法术伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：计算法术修正后的伤害。
        decimal damage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);
        bool wasAirborne = AirborneHelper.IsAirborne(cardPlay.Target);
        PrepareGroundedCheck(cardPlay.Target, wasAirborne);

        // 第二步：先对目标结算一次法术伤害。
        await SpellHelper.Damage(
            choiceContext,
            base.Owner.Creature,
            cardPlay.Target,
            damage,
            this
        );

        // 第三步：若目标处于浮空，再追加一次同样的法术伤害。
        bool shouldFollowUp = wasAirborne && !_groundedByCurrentHit && cardPlay.Target.IsAlive;
        ResetGroundedCheck();
        if (shouldFollowUp)
        {
            await SpellHelper.Damage(
                choiceContext,
                base.Owner.Creature,
                cardPlay.Target,
                damage,
                this
            );
        }
    }

    // 升级后提高法术伤害。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars.CalculationBase.UpgradeValueBy(2m);
    }

    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = applier;

        // 和落空陷阱同理：真正让目标掉飞行的是 FlyPower.AfterDamageReceived -> PowerCmd.Decrement(this)，
        // 这条链里的 cardSource 不是当前牌本身。若这里强卡 cardSource == this，
        // 就会导致第一段明明已经把目标打落地，后续仍错误地追加第二段伤害。
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

    private void PrepareGroundedCheck(Creature target, bool wasAirborne)
    {
        _pendingGroundedTarget = target;
        _groundedByCurrentHit = false;
        _pendingGroundedCheck = target.IsAlive && !TemporaryFlyPower.IsResolvingExpiration && wasAirborne;
    }

    private void ResetGroundedCheck()
    {
        _pendingGroundedTarget = null;
        _pendingGroundedCheck = false;
        _groundedByCurrentHit = false;
    }
}
