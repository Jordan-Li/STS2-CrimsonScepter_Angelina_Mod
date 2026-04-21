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
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：借势投送
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：寄送1张牌。每有1张寄送牌，对敌方全体造成一次5点伤害。
/// 升级后效果：寄送1张牌。每有1张寄送牌，对敌方全体造成一次7点伤害。
/// </summary>
public sealed class LeverageDelivery : AngelinaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    public LeverageDelivery()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "LEVERAGE_DELIVERY.selectPrompt"), 1),
            filter: null,
            source: this)).FirstOrDefault();

        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();

        if (selectedCard is CardModel cardToSend)
        {
            deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);
            await CardCmd.Exhaust(choiceContext, cardToSend);

            if (deliveryPower != null)
            {
                await deliveryPower.EnqueueCard(cardToSend);
            }
        }

        int hitCount = deliveryPower?.GetQueuedCards().Count ?? 0;
        if (hitCount <= 0)
        {
            return;
        }

        if (base.CombatState == null)
        {
            return;
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this)
            .TargetingAllOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_flying_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
