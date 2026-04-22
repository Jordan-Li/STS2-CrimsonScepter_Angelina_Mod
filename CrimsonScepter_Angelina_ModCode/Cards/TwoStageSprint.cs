using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：技能
/// 效果：获得8点格挡。若本场战斗中打出过同名牌，获得1层飞行。
/// 升级后效果：获得12点格挡。若本场战斗中打出过同名牌，获得2层飞行。
/// </summary>
public sealed class TwoStageSprint : AngelinaCard
{
    // 这是一张获得格挡的技能牌。
    public override bool GainsBlock => true;

    // 若本场战斗中已经打出过同名牌，则用金色边框提示二段效果已可触发。
    protected override bool ShouldGlowGoldInternal => HasBeenPlayedThisCombat;

    // 这张牌会用到临时飞行的悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TemporaryFlyPower>()
    ];

    // 维护格挡和临时飞行两个动态数值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<TemporaryFlyPower>(1m)
    ];

    // 只要本场战斗中已经打出过同名牌，就视为满足二段条件。
    private bool HasBeenPlayedThisCombat => GetTimesPlayedThisCombat() >= 1;

    public TwoStageSprint()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先获得格挡。
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 若这是本场战斗中第二次及以后打出同名牌，则再获得临时飞行。
        if (GetTimesPlayedThisCombat() >= 2)
        {
            await PowerCmd.Apply<TemporaryFlyPower>(
                base.Owner.Creature,
                base.DynamicVars["TemporaryFlyPower"].BaseValue,
                base.Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后同时提高格挡和临时飞行层数。
        base.DynamicVars.Block.UpgradeValueBy(4m);
        base.DynamicVars["TemporaryFlyPower"].UpgradeValueBy(1m);
    }

    private int GetTimesPlayedThisCombat()
    {
        // 统计本场战斗中已经开始打出的同名牌次数，用来驱动二段效果。
        if (base.CombatState == null)
        {
            return 0;
        }

        return CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
            entry.CardPlay.Card.Owner == base.Owner &&
            entry.CardPlay.Card.Id == Id);
    }
}
