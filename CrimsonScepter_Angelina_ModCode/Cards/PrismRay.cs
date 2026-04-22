using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：棱镜射线
/// 费用：1
/// 稀有度：稀有
/// 卡牌类型：攻击
/// 效果：造成12点法术伤害。变化所有寄送的牌。
/// 升级后效果：造成16点法术伤害。变化所有寄送牌并升级。
/// </summary>
public sealed class PrismRay : AngelinaCard
{
    public override bool IsSpell => true;

    // 额外悬浮说明：
    // 1. 寄送
    // 2. 变化
    // 3. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>(),
        HoverTipFactory.Static(StaticHoverTip.Transform),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：法术伤害。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Unpowered | ValueProp.Move),
        new CalculationBaseVar(12m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Unpowered | ValueProp.Move)
            .WithMultiplier(static (card, _) => card.Owner?.Creature?.GetPower<FocusPower>()?.Amount ?? 0m)
    ];

    // 初始化卡牌的基础信息：1费、攻击、稀有、目标为单体敌人。
    public PrismRay()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时，先造成法术伤害，再变化所有寄送中的牌。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：计算法术修正后的伤害并结算。
        decimal damage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);
        await SpellHelper.Damage(choiceContext, base.Owner.Creature, cardPlay.Target, damage, this);

        // 第二步：变化所有寄送中的牌。
        await TransformDeliveredCards();
    }

    // 升级后提高法术伤害。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
        base.DynamicVars.CalculationBase.UpgradeValueBy(4m);
    }

    // 变化当前所有寄送中的牌；若此牌已升级，则变化后的牌额外升级。
    private async Task TransformDeliveredCards()
    {
        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        if (deliveryPower == null)
        {
            return;
        }

        List<CardModel> queuedCards = deliveryPower.GetQueuedCards().ToList();
        if (queuedCards.Count == 0)
        {
            return;
        }

        // 逐张处理当前寄送队列中的牌。
        foreach (CardModel queuedCard in queuedCards)
        {
            if (queuedCard.Pile?.Type != PileType.Exhaust || !queuedCard.IsTransformable)
            {
                continue;
            }

            // 将寄送中的牌变化为一张随机牌。
            CardPileAddResult result = await CardCmd.TransformToRandom(
                queuedCard,
                base.Owner.RunState.Rng.Niche,
                CardPreviewStyle.None);

            CardModel transformedCard = result.cardAdded;

            // 升级版会把变化后的牌额外升级一次。
            if (base.IsUpgraded && transformedCard.IsUpgradable)
            {
                CardCmd.Upgrade(transformedCard, CardPreviewStyle.None);
            }

            // 直接替换寄送队列中的旧牌引用，避免残留已被变化移除的旧对象。
            await deliveryPower.ReplaceQueuedCard(queuedCard, transformedCard);
        }
    }
}
