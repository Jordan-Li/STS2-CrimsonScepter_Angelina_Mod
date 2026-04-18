using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：自然爆发
/// 费用：2
/// 稀有度：稀有
/// 卡牌类型：能力
/// 效果：当你对敌方造成法术伤害后，使所有敌方获得3层中毒。
/// 升级后效果：当你对敌方造成法术伤害后，使所有敌方获得4层中毒。
/// </summary>
public sealed class NaturalBurst : AngelinaCard
{
    // 额外悬浮说明：
    // 1. 中毒
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PoisonPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：每次触发时施加的中毒层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<NaturalBurstPower>(3m)
    ];

    // 初始化卡牌的基础信息：2费、能力、稀有、目标为自己。
    public NaturalBurst()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时，施加一个持续能力 Power：
    // 当你造成法术伤害后，使所有敌方获得中毒。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先播放施法动作，再挂上持续 Power。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NaturalBurstPower>(
            base.Owner.Creature,
            base.DynamicVars["NaturalBurstPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后中毒层数 +1。
    protected override void OnUpgrade()
    {
        base.DynamicVars["NaturalBurstPower"].UpgradeValueBy(1m);
    }
}
