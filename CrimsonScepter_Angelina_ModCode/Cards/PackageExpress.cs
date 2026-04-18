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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：包裹速递
/// 费用：1
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：寄送1张牌。抽2张牌。
/// 升级后效果：寄送1张牌。抽3张牌。
/// </summary>
public sealed class PackageExpress : AngelinaCard
{
    // 动态变量：抽牌数，初始为2
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    // 额外悬浮说明：寄送
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    // 初始化卡牌的基础信息：1费、技能、罕见、目标为自己。
    public PackageExpress()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：从手牌中选择1张牌寄送
        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "PACKAGE_EXPRESS.selectPrompt"), 1),
            filter: null,
            source: this)).FirstOrDefault();

        if (selectedCard is not null)
        {
            // 获取现有的寄送Power；如果没有，就创建一个
            DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
            deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

            // 先把牌移到 Exhaust
            await CardCmd.Exhaust(choiceContext, selectedCard);

            // 再加入寄送队列
            if (deliveryPower != null)
            {
            await deliveryPower.EnqueueCard(selectedCard);
            }
        }

        // 第二步：播放施法动作
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 第三步：抽牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
    }

    // 升级后：抽牌数 +1（2 -> 3）
    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
