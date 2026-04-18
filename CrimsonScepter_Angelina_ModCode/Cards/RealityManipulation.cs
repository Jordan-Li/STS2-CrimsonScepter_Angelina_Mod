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
/// 卡牌名：实相操纵
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：能力
/// 效果：每当消耗牌堆的数量变动时，获得1点格挡。
/// 升级后效果：每当消耗牌堆的数量变动时，获得2点格挡。
/// </summary>
public sealed class RealityManipulation : AngelinaCard
{
    // 额外悬浮说明：实相操纵。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<RealityManipulationPower>()
    ];

    // 动态变量：实相操纵每次触发给予的格挡值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RealityManipulationPower>(1m)
    ];

    // 初始化卡牌的基础信息：1费、能力、罕见、目标为自己。
    public RealityManipulation()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，施加一个持续能力 Power：
    // 每当消耗牌堆的数量发生变动时，获得格挡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先播放施法动作，再挂上持续 Power。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<RealityManipulationPower>(
            base.Owner.Creature,
            base.DynamicVars["RealityManipulationPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后每次触发获得的格挡值 +1。
    protected override void OnUpgrade()
    {
        base.DynamicVars["RealityManipulationPower"].UpgradeValueBy(1m);
    }
}