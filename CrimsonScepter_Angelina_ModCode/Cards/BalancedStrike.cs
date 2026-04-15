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
/// 卡牌名：平衡打击
/// 卡牌类型：攻击牌
/// 稀有度：普通
/// 费用：1费
/// 效果：造成7点伤害，并施加8点失衡
/// 升级后效果：造成10点伤害，并施加10点失衡
/// 备注：这是失衡体系的中段铺垫牌
/// </summary>
public sealed class BalancedStrike : AngelinaCard
{
    // 这张牌当前施加的失衡数值
    // 初始为8，升级后改成10
    private decimal imbalanceAmount = 8m;

    // 定义一个动态伤害变量，初始值为7点
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move)
    };

    // 添加卡牌标签：打击
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    // 费用：1费，类型：攻击牌，稀有度：普通，打击目标：任选一个目标
    public BalancedStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果没有目标，就直接报错，避免空引用
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：先对目标造成伤害
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)                             // 说明这次伤害来自这张牌
            .Targeting(cardPlay.Target)                 // 指定攻击目标
            .WithHitFx("vfx/vfx_flying_slash")          // 命中特效
            .Execute(choiceContext);                    // 真正执行

        // 第二步：再给目标施加失衡
        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            imbalanceAmount,
            base.Owner.Creature,
            this
        );
    }

    // 升级后：
    // 1. 伤害提高3点（7 -> 10）
    // 2. 失衡提高2点（8 -> 10）
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
        imbalanceAmount = 10m;
    }
}