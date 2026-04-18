using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：能力
/// 效果：接下来2个回合开始时，获得1点力量、1点敏捷和1点集中。
/// 升级后效果：接下来3个回合开始时，获得1点力量、1点敏捷和1点集中。
/// </summary>
public sealed class QualityVisitor : AngelinaCard
{
    // 这张牌会用到质素访客、力量、敏捷和集中四种悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<QualityVisitorPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<FocusPower>()
    ];

    // 维护持续回合数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<QualityVisitorPower>(2m)
    ];

    public QualityVisitor()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出时获得质素访客能力，在接下来若干回合开始时给属性。
        await PowerCmd.Apply<QualityVisitorPower>(
            base.Owner.Creature,
            base.DynamicVars["QualityVisitorPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        // 升级后把持续回合数从 2 提高到 3。
        base.DynamicVars["QualityVisitorPower"].UpgradeValueBy(1m);
    }
}