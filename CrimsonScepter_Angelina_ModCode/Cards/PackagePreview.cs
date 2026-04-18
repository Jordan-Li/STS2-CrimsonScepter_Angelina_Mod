using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：预览
/// 费用：1
/// 稀有度：普通
/// 卡牌类型：攻击
/// 效果：造成10点伤害。将最近寄送的1张牌送达后置于抽牌堆顶。
/// 升级后效果：造成15点伤害。将最近寄送的1张牌送达后置于抽牌堆顶。
/// </summary>
public sealed class PackagePreview : AngelinaCard
{
    // 定义一个动态伤害变量，初始值为10点
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    // 额外悬浮说明：寄送
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    // 费用：1费，类型：攻击牌，稀有度：普通，目标：任选一个敌人
    public PackagePreview()
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

        // 第二步：获取当前唯一的寄送Power
        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        if (deliveryPower == null)
        {
            return;
        }

        // 第三步：取出最近寄送的一张牌
        CardModel? deliveredCard = deliveryPower.PeekLatest();
        if (deliveredCard?.Pile?.Type != PileType.Exhaust)
        {
            return;
        }

        // 第四步：先把这张牌真正送达，确保送达效果会照常触发
        if (!await deliveryPower.DeliverLatestNow())
        {
            return;
        }

        // 第五步：再把这张已送达的牌从手牌移到抽牌堆顶
        await CardPileCmd.Add(deliveredCard, PileType.Draw, CardPilePosition.Top, this);
    }

    // 升级后，将动态变量 DynamicVars 的伤害提高5点（10 -> 15）
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
