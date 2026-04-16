using System;
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
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：寄送流程
/// 卡牌类型：攻击牌
/// 稀有度：普通
/// 费用：1费
/// 效果：造成8点伤害。然后从手牌中选择1张牌，将其寄送。
/// 升级后效果：造成12点伤害。
/// 备注：这张牌已适配新版单图标寄送系统。
/// </summary>
public sealed class DeliveryFlow : AngelinaCard
{
    // 定义一个动态伤害变量，初始值为8点
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move)
    };

    // 额外悬浮说明：寄送
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<DeliveryPower>()
    };

    // 费用：1费，类型：攻击牌，稀有度：普通，目标：任选一个敌人
    public DeliveryFlow()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果没有目标，就直接报错，避免空引用
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：先对目标造成伤害
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)                             // 说明这次伤害来自这张牌
            .Targeting(cardPlay.Target)                 // 指定攻击目标
            .WithHitFx("vfx/vfx_flying_slash")          // 命中特效
            .Execute(choiceContext);                    // 真正执行

        // 第二步：从手牌中选择1张牌寄送
        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "DELIVERY_FLOW.selectPrompt"), 1),
            filter: null,
            source: this)).FirstOrDefault();

        // 如果没有选到牌，就直接结束
        if (selectedCard == null)
        {
            return;
        }

        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

        // 先把牌移到 Exhaust
        await CardCmd.Exhaust(choiceContext, selectedCard);

        // 再加入寄送队列
        if (deliveryPower != null)
        {
            await deliveryPower.SetSelectedCard(selectedCard);
        }

        // 第五步：把这张牌移到 Exhaust，等待下回合开始时送回
        await CardCmd.Exhaust(choiceContext, selectedCard);
    }

    // 升级后，将动态变量 DynamicVars 的伤害提高4点（8 -> 12）
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(4m);
    }
}