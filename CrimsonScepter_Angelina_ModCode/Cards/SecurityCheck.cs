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
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：能力
/// 效果：寄送一张状态牌或诅咒牌时，将其消耗并对所有敌方施加10点失衡。
/// 升级后效果：减1费。
/// </summary>
public sealed class SecurityCheck : AngelinaCard
{
    // 这张牌会用到安全检查和失衡两种悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SecurityCheckPower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    // 维护拦截寄送时施加的失衡值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SecurityCheckPower>(10m)
    ];

    public SecurityCheck()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出时获得安全检查能力，用来拦截寄送的状态牌和诅咒牌。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<SecurityCheckPower>(
            base.Owner.Creature,
            base.DynamicVars["SecurityCheckPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        // 升级后只减1费，不提高施加的失衡值。
        base.EnergyCost.UpgradeBy(-1);
    }
}