using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：定位器
/// 卡牌类型：技能牌
/// 稀有度：衍生
/// 费用：0费
/// 效果：失去2点集中，获得2点力量与2点敏捷。将一张转换器加入抽牌堆。
/// 升级后效果：三项数值都提高1点。
/// 备注：这是转换器生成的衍生牌。
/// </summary>
public sealed class Locator : AngelinaCard
{
    // 额外悬浮说明：
    // - 力量
    // - 敏捷
    // - 集中
    // - 转换器
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<FocusPower>(),
        HoverTipFactory.FromCard<Converter>(base.IsUpgraded)
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

    // 关键词：消耗、保留
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Exhaust,
        CardKeyword.Retain
    };

    // 费用：0费，类型：技能牌，稀有度：衍生，目标：自己
    public Locator()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal amount = base.DynamicVars["StrengthPower"].BaseValue;

        // 第一步：失去集中
        await PowerCmd.Apply<FocusPower>(base.Owner.Creature, -amount, base.Owner.Creature, this);

        // 第二步：获得力量和敏捷
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, amount, base.Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(base.Owner.Creature, amount, base.Owner.Creature, this);

        // 第三步：生成一张转换器加入抽牌堆
        if (base.CombatState == null)
        {
            return;
        }

        CardModel converter = base.CombatState.CreateCard<Converter>(base.Owner);
        if (base.IsUpgraded)
        {
            CardCmd.Upgrade(converter);
        }

        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(
                converter,
                PileType.Draw,
                addedByPlayer: true,
                CardPilePosition.Random));

        await Cmd.Wait(0.5f);
    }

    // 升级后：三项数值都 +1
    protected override void OnUpgrade()
    {
        base.DynamicVars["StrengthPower"].UpgradeValueBy(1m);
        base.DynamicVars["DexterityPower"].UpgradeValueBy(1m);
        base.DynamicVars["FocusPower"].UpgradeValueBy(1m);
    }

    // 给描述补充“转换器”的牌名
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        CardModel converterPreview = ModelDb.Card<Converter>().ToMutable();
        if (base.IsUpgraded)
        {
            CardCmd.Upgrade(converterPreview);
        }

        description.Add("ConverterCard", converterPreview.Title);
    }
}