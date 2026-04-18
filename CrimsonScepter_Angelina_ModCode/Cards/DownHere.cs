using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：在下面！
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：保留。若你不处于浮空，则对所有处于浮空的敌方施加1层虚弱和5点失衡。造成6点伤害。
/// 升级后效果：保留。若你不处于浮空，则对所有处于浮空的敌方施加2层虚弱和7点失衡值。造成8点伤害。
/// </summary>
public sealed class DownHere : AngelinaCard
{
    // 关键字：保留。
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain
    ];

    // 额外悬浮说明：
    // 1. 保留
    // 2. 虚弱
    // 3. 失衡
    // 4. 飞行
    // 5. 浮空
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<FlyPower>(),
        new HoverTip(
            new LocString("powers", "AIRBORNE.title"),
            new LocString("powers", "AIRBORNE.description"))
    ];

    // 动态变量：
    // 1. 伤害
    // 2. 施加的虚弱层数
    // 3. 施加的失衡值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new PowerVar<WeakPower>(1m),
        new PowerVar<ImbalancePower>(5m)
    ];

    // 初始化卡牌的基础信息：1费、攻击、普通、目标为单体敌人。
    public DownHere()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时，若自己不处于浮空，则先对所有浮空敌人施加虚弱和失衡；之后再对目标造成伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：若自己当前不处于浮空，则筛出所有浮空敌人并施加负面效果。
        if (!AirborneHelper.IsAirborne(base.Owner.Creature))
        {
            var combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during DownHere.OnPlay.");

            IEnumerable<Creature> airborneEnemies = combatState.HittableEnemies
                .Where(AirborneHelper.IsAirborne);

            foreach (Creature enemy in airborneEnemies)
            {
                // 对浮空敌人施加虚弱。
                await PowerCmd.Apply<WeakPower>(
                    enemy,
                    base.DynamicVars["WeakPower"].BaseValue,
                    base.Owner.Creature,
                    this
                );

                // 对浮空敌人施加失衡。
                await PowerCmd.Apply<ImbalancePower>(
                    enemy,
                    base.DynamicVars["ImbalancePower"].BaseValue,
                    base.Owner.Creature,
                    this
                );
            }
        }

        // 第二步：无论前面的条件是否满足，都对选中的目标造成一次伤害。
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    // 升级后提高伤害、虚弱层数和失衡值。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars["WeakPower"].UpgradeValueBy(1m);
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(2m);
    }
}
