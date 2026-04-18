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
/// 卡牌名：滞空
/// 费用：2
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：获得5层飞行。
/// 升级后效果：获得7层飞行。
/// </summary>
public sealed class Suspended : AngelinaCard
{
    // 额外悬浮说明：飞行。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FlyPower>()
    ];

    // 动态变量：获得的飞行层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FlyPower>(5m)
    ];

    // 初始化卡牌的基础信息：2费、技能、罕见、目标为自己。
    public Suspended()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，给予自己飞行。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对自己施加飞行层数。
        await PowerCmd.Apply<FlyPower>(
            base.Owner.Creature,
            base.DynamicVars["FlyPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后额外提高2层飞行。
    protected override void OnUpgrade()
    {
        base.DynamicVars["FlyPower"].UpgradeValueBy(2m);
    }
}