using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：乘势
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：造成6点伤害。所有单位每有1层飞行，抽1张牌。
/// 升级后效果：造成9点伤害。所有单位每有1层飞行，抽1张牌。
/// </summary>
public sealed class Momentum : AngelinaCard
{
    protected override bool ShouldGlowGoldInternal =>
        base.CombatState?.Creatures.Any(creature => (creature.GetPower<FlyPower>()?.Amount ?? 0) > 0) == true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FlyPower>()
    ];

    public Momentum()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        int drawCount = base.CombatState?.Creatures.Sum(creature => creature.GetPower<FlyPower>()?.Amount ?? 0) ?? 0;
        if (drawCount > 0)
        {
            await CardPileCmd.Draw(choiceContext, drawCount, base.Owner);
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_flying_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
