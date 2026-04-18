using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：包扎带
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：获得8点格挡。为1张手牌添加附魔：送达时，在本场战斗中获得升级。
/// 升级后效果：获得12点格挡。为所有手牌添加附魔：送达时，在本场战斗中获得升级。
/// </summary>
public sealed class Bandage : AngelinaCard
{
    // 这张牌会提供格挡。
    public override bool GainsBlock => true;

    // 额外悬浮说明：同时展示“送达升级”附魔和“送达”的效果提示。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        ModelDb.Enchantment<DeliveredUpgradeEnchantment>().HoverTip,
        new HoverTip(
            new LocString("cards", "DELIVERED.title"),
            new LocString("cards", "DELIVERED.description"))
    ];

    // 动态变量：本牌提供的格挡数值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    // 初始化卡牌的基础信息：1费、技能、罕见、目标为自己。
    public Bandage()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，先获得格挡，再为手牌添加“送达时在本场战斗中升级”的附魔。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：先获得格挡。
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 第二步：收集当前手牌里所有可升级、且允许附魔的牌。
        EnchantmentModel deliveredUpgrade = ModelDb.Enchantment<DeliveredUpgradeEnchantment>();
        List<CardModel> validHandCards = PileType.Hand
            .GetPile(base.Owner)
            .Cards
            .Where(card => card.IsUpgradable && deliveredUpgrade.CanEnchant(card))
            .ToList();

        if (validHandCards.Count == 0)
        {
            return;
        }

        // 第三步：
        // 未升级时，选择1张手牌附魔。
        // 升级后，直接为所有符合条件的手牌附魔。
        List<CardModel> selectedCards = base.IsUpgraded
            ? validHandCards
            : (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: base.Owner,
                prefs: new CardSelectorPrefs(new LocString("cards", "BANDAGE.selectPrompt"), 1),
                filter: card => card.IsUpgradable && deliveredUpgrade.CanEnchant(card),
                source: this)).ToList();

        // 第四步：为被选中的手牌添加“送达时在本场战斗中升级”附魔。
        foreach (CardModel selectedCard in selectedCards)
        {
            CardCmd.Enchant<DeliveredUpgradeEnchantment>(selectedCard, 1m);
        }
    }

    // 升级后提升格挡，并把附魔范围改为所有符合条件的手牌。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(4m);
    }
}
