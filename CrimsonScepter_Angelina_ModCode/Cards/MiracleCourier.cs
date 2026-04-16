using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：奇迹信使
/// 卡牌类型：技能牌
/// 稀有度：稀有
/// 费用：1费
/// 效果：寄送所有其他手牌，而后抽等量的牌。
/// 升级后效果：减1费。
/// 备注：已适配新版单图标寄送系统。
/// </summary>
public sealed class MiracleCourier : AngelinaCard
{
    // 额外悬浮说明：
    // - 消耗
    // - 寄送
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Exhaust
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<DeliveryPower>()
    };

    // 费用：1费，类型：技能牌，稀有度：稀有，目标：自己
    public MiracleCourier()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取手牌中除自己以外的所有牌
        List<CardModel> cardsToDeliver = (base.Owner.PlayerCombatState?.Hand.Cards
            .Where(card => card != this)
            .ToList()) ?? new List<CardModel>();

        if (cardsToDeliver.Count == 0)
        {
            return;
        }

        // 获取现有的寄送Power；如果没有，就创建一个
        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

        if (deliveryPower == null)
        {
            return;
        }

        int deliveredCount = 0;

        // 逐张寄送
        foreach (CardModel card in cardsToDeliver)
        {
            // 先把牌移到 Exhaust
            await CardCmd.Exhaust(choiceContext, card);

            // 再加入寄送队列
            await deliveryPower.SetSelectedCard(card);

            deliveredCount++;
        }

        // 播放施法动作
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 抽回等量的牌
        if (deliveredCount > 0)
        {
            await CardPileCmd.Draw(choiceContext, deliveredCount, base.Owner);
        }
    }

    // 升级后减1费
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}