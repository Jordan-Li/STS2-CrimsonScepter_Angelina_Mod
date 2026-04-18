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
            // 记录伤害前目标是否处于浮空。
            bool wasAirborne = AirborneHelper.IsAirborne(cardPlay.Target);

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 若目标从浮空状态脱离，则本张牌只触发一次回能。
            bool isAirborneNow = cardPlay.Target.IsAlive && AirborneHelper.IsAirborne(cardPlay.Target);
            if (!grantedEnergy && wasAirborne && !isAirborneNow)
            {
                await PlayerCmd.GainEnergy(base.DynamicVars["EnergyGain"].BaseValue, base.Owner);
                grantedEnergy = true;
            }

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
}
