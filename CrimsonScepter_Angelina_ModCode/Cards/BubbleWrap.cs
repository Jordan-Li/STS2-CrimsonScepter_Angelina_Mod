using System.Collections.Generic;
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
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：泡沫纸
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：攻击
/// 效果：造成4点法术伤害。寄送这张牌的复制品。
/// 升级后效果：造成6点法术伤害。寄送这张牌的复制品。
/// </summary>
public sealed class BubbleWrap : AngelinaCard
{
    public override bool IsSpell => true;

    // 额外悬浮说明：
    // 1. 寄送
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 动态变量：法术伤害，初始值为3
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Unpowered | ValueProp.Move),
        new CalculationBaseVar(4m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Unpowered | ValueProp.Move)
            .WithMultiplier(static (card, _) => card.Owner?.Creature?.GetPower<FocusPower>()?.Amount ?? 0m)
    ];

    // 费用：0费，类型：攻击牌，稀有度：普通，目标：任意敌人
    public BubbleWrap()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 第一步：造成法术伤害
        decimal damage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);
        await SpellHelper.Damage(choiceContext, base.Owner.Creature, cardPlay.Target, damage, this);

        // 第二步：复制自己
        CardModel copy = CreateClone();

        // 第三步：把复制品放进 Exhaust
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

    // 升级后伤害 +2
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars.CalculationBase.UpgradeValueBy(2m);
    }
}
