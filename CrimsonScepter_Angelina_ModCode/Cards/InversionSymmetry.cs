using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：X+
/// 稀有度：稀有
/// 卡牌类型：技能
/// 效果：消耗X点能量，依次打出弃牌堆顶开始数的前X张非反演对称牌。
/// 升级后效果：保留。
/// </summary>
public sealed class InversionSymmetry : AngelinaCard
{
    // 升级后获得保留，关键字会由游戏自动追加到卡牌显示中。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [CardKeyword.Retain] : [];

    // 这是一张 X 费技能牌，实际消耗在打出时结算。
    protected override bool HasEnergyCostX => true;

    public InversionSymmetry()
        : base(-1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先结算这次实际消耗的 X 费；若没有投入能量，则不继续执行。
        int spentEnergy = ResolveEnergyXValue();
        if (spentEnergy <= 0)
        {
            return;
        }

        // 从弃牌堆顶开始往下找，依次取前 X 张“非反演对称”的牌来打出。
        CardPile discardPile = PileType.Discard.GetPile(base.Owner);
        List<CardModel> cards = discardPile.Cards
            .Reverse()
            .Where(card => card is not InversionSymmetry)
            .Take(spentEnergy)
            .ToList();

        // 按顺序依次打出这些牌；一旦战斗进入结束态，就停止继续打牌。
        foreach (CardModel card in cards)
        {
            if (CombatManager.Instance.IsOverOrEnding)
            {
                break;
            }

            await CardCmd.AutoPlay(choiceContext, card, null, skipXCapture: true);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后获得保留。
        AddKeyword(CardKeyword.Retain);
    }
}