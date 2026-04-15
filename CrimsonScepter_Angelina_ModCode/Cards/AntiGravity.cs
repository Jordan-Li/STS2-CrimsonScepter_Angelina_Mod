using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：反重力
/// 卡牌类型：攻击牌
/// 稀有度：基础
/// 费用：2费
/// 效果：先施加12点失衡，再造成8点法术伤害
/// 升级后效果：先施加12点失衡，再造成12点法术伤害
/// 备注：当前先不添加临时飞行；法术伤害暂时用普通伤害实现，后续接回SpellHelper再改
/// </summary>
public sealed class AntiGravity : AngelinaCard
{
    // 这张牌当前施加的失衡数值
    // 先固定为12，升级不变
    private const decimal ImbalanceAmount = 12m;

    // 定义一个动态伤害变量，初始值为8点
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move)
    };

    // 费用：2费，类型：攻击牌，稀有度：基础，打击目标：任选一个目标
    public AntiGravity()
        : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果没有目标，就直接报错，避免空引用
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：先给目标施加12点失衡
        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            ImbalanceAmount,
            base.Owner.Creature,
            this
        );

        // 第二步：再对目标造成伤害
        // 这里原设计应为“法术伤害”，但当前先用普通伤害实现，
        // 后续把SpellHelper迁回后，再替换成真正的法术伤害逻辑
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)                             // 说明这次伤害来自这张牌
            .Targeting(cardPlay.Target)                 // 指定攻击目标
            .WithHitFx("vfx/vfx_flying_slash")          // 命中特效
            .Execute(choiceContext);                    // 真正执行
    }

    // 升级后，将动态变量 DynamicVars 的伤害提高4点（8 -> 12）
    // 当前先不改失衡数值，保持12点
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}