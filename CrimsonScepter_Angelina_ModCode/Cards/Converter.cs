using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：转换器
/// 卡牌类型：技能牌
/// 稀有度：非凡
/// 费用：0费
/// 效果：失去2点力量与2点敏捷，获得2点集中。寄送一张定位器。
/// 升级后效果：三项数值都提高1点。
/// 备注：已适配新版单图标寄送系统。
/// </summary>
public sealed class Converter : AngelinaCard
{
    // 额外悬浮说明：
    // - 力量
    // - 敏捷
    // - 集中
    // - 寄送
    // - 定位器
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<FocusPower>(),
        HoverTipFactory.FromPower<DeliveryPower>(),
        HoverTipFactory.FromCard<Locator>(base.IsUpgraded)
    };

    // 动态变量：
    // 1. 力量变化值
    // 2. 敏捷变化值
    // 3. 集中变化值
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(2m),
        new PowerVar<DexterityPower>(2m),
        new PowerVar<FocusPower>(2m)
    };

    // 关键词：消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust
    };

    // 费用：0费，类型：技能牌，稀有度：非凡，目标：自己
    public Converter()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal amount = base.DynamicVars["StrengthPower"].BaseValue;

        // 第一步：失去力量和敏捷
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, -amount, base.Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(base.Owner.Creature, -amount, base.Owner.Creature, this);

        // 第二步：获得集中
        await PowerCmd.Apply<FocusPower>(base.Owner.Creature, amount, base.Owner.Creature, this);

        // 第三步：生成一张定位器
        if (base.CombatState == null)
        {
            return;
        }

        CardModel locator = base.CombatState.CreateCard<Locator>(base.Owner);
        if (base.IsUpgraded)
        {
            CardCmd.Upgrade(locator);
        }

        // 第四步：先把定位器生成到 Exhaust
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(
                locator,
                PileType.Exhaust,
                addedByPlayer: true));

        // 第五步：加入新版单图标寄送队列
        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

        if (deliveryPower != null)
        {
            await deliveryPower.SetSelectedCard(locator);
        }
    }

    // 升级后：三项数值都 +1
    protected override void OnUpgrade()
    {
        base.DynamicVars["StrengthPower"].UpgradeValueBy(1m);
        base.DynamicVars["DexterityPower"].UpgradeValueBy(1m);
        base.DynamicVars["FocusPower"].UpgradeValueBy(1m);
    }

    // 给描述补充“定位器”的牌名
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        CardModel locatorPreview = ModelDb.Card<Locator>().ToMutable();
        if (base.IsUpgraded)
        {
            CardCmd.Upgrade(locatorPreview);
        }

        description.Add("LocatorCard", locatorPreview.Title);
    }
}