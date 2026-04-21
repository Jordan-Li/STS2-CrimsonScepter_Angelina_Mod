using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：应急法术
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：向弃牌堆中加入1张眩晕。获得1点能量。抽2张牌。
/// 升级后效果：向弃牌堆中加入1张眩晕。获得2点能量。抽2张牌。
/// </summary>
public sealed class EmergencySpell : AngelinaCard
{
    // 额外悬浮说明：
    // 1. 眩晕
    // 2. 能量
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Dazed>(),
        HoverTipFactory.ForEnergy(this)
    ];

    // 动态变量：
    // 1. 获得的能量数值
    // 2. 抽牌数量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(2)
    ];

    // 初始化卡牌的基础信息：0费、技能、罕见、目标为自己。
    public EmergencySpell()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，先加入眩晕，再获得能量并抽牌。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CombatState combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null during EmergencySpell.OnPlay.");

        // 第一步：向弃牌堆中加入1张眩晕。
        CardModel dazed = combatState.CreateCard<Dazed>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Discard, addedByPlayer: true));

        // 第二步：获得能量。
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);

        // 第三步：抽牌。
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
    }

    // 升级后只提高获得的能量。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
