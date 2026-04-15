using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：打击
/// 卡牌类型：攻击牌
/// 稀有度：基础
/// 费用：1费
/// 效果：造成6点伤害
/// 升级后效果：造成9点伤害
/// 备注：基础卡牌
/// </summary>
public sealed class StrikeAngelina : AngelinaCard
{
    // 添加卡牌标签：打击
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    // 定义一个动态伤害变量，初始值为6点
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move)
    };

    // 费用：1费，类型：攻击牌，稀有度：基础，打击目标：任选一个目标
    public StrikeAngelina()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果没有目标，就直接报错，避免空引用
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 执行攻击命令
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)                                 // 说明这次伤害来自这张牌
            .Targeting(cardPlay.Target)                     // 指定攻击目标
            .WithHitFx("vfx/vfx_flying_slash")          // 命中特效
            .Execute(choiceContext);                        // 真正执行
    }

    // 升级后，将动态伤害变量 DynamicVars 的伤害提高3点
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}