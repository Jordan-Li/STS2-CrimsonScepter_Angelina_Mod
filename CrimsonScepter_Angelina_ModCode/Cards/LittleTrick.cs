using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：小戏法
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：选择1张牌。寄送其复制品。
/// 升级后效果：减1费。
/// </summary>
public sealed class LittleTrick : AngelinaCard
{
    // 额外悬浮说明：寄送
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    // 费用：1费，类型：技能牌，稀有度：非凡，目标：自己
    public LittleTrick()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：从手牌中选择1张牌
        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "LITTLE_TRICK.selectPrompt"), 1),
            filter: card => card != this,
            source: this)).FirstOrDefault();

        if (selectedCard == null)
        {
            return;
        }

        // 第二步：复制该牌
        CardModel copy = selectedCard.CreateClone();

        // 第三步：把复制品直接生成到 Exhaust
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(
                copy,
                PileType.Exhaust,
                addedByPlayer: true));

        // 第四步：加入寄送队列
        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

        if (deliveryPower != null)
        {
            await deliveryPower.EnqueueCard(copy);
        }
    }

    // 升级后减1费
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
