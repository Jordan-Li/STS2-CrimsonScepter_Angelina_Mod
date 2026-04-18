using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：3
/// 稀有度：稀有
/// 卡牌类型：能力
/// 效果：虚无。你打出的攻击牌的伤害减半。每张攻击牌在每回合首次打出时返回手牌，在本回合下次打出时不消耗能量。
/// 升级后效果：移除虚无。
/// </summary>
public sealed class ParticleMode : AngelinaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? []
        : [CardKeyword.Ethereal];

    // 这张牌会用到虚无和对应能力的悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [HoverTipFactory.FromPower<ParticleModePower>()]
        : [HoverTipFactory.FromKeyword(CardKeyword.Ethereal), HoverTipFactory.FromPower<ParticleModePower>()];

    // 维护微粒模式能力层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ParticleModePower>(1m)
    ];

    // 费用 3，能力牌，稀有度为稀有，自身目标。
    public ParticleMode()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时获得微粒模式能力。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<ParticleModePower>(
            base.Owner.Creature,
            base.DynamicVars["ParticleModePower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后移除虚无。
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}