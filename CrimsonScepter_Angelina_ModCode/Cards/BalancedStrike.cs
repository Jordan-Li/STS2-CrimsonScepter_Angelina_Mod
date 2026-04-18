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
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：造成7点伤害。施加8点失衡。
/// 升级后效果：造成9点伤害。施加10点失衡。
/// </summary>
public sealed class BalancedStrike : AngelinaCard
{
    // 定义这张牌的基础伤害和失衡数值，供卡面显示与结算共用。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<ImbalancePower>(8m)
    ];

    // 这张牌带有 Strike 标签，供其他“打击”相关效果识别。
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 初始化卡牌的基础信息：1费、攻击牌、普通、目标为单体敌人。
    public BalancedStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时，先对目标造成伤害，再施加失衡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：先对目标造成一次普通攻击伤害。
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_flying_slash")
            .Execute(choiceContext);

        // 第二步：再给目标施加失衡。
        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后将伤害提高2点、失衡提高2点，对应卡面变为9伤害和10失衡。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(2m);
    }
}
