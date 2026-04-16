using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：提前送达！
/// 卡牌类型：技能牌
/// 稀有度：稀有
/// 费用：1费
/// 效果：立刻获得你寄送的牌。给予敌方全体6点失衡。
/// 升级后效果：给予敌方全体8点失衡。
/// 备注：已适配新版单图标寄送系统。
/// </summary>
public sealed class EarlyDelivery : AngelinaCard
{
    // 额外悬浮说明：
    // - 消耗
    // - 寄送
    // - 失衡
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<DeliveryPower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    };

    // 关键词：消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Exhaust
    };

    // 动态变量：施加的失衡，初始为6
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ImbalancePower>(6m)
    };

    // 费用：1费，类型：技能牌，稀有度：非凡，目标：全体敌人
    public EarlyDelivery()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一步：立刻拿回当前所有寄送牌
        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        if (deliveryPower != null)
        {
            await deliveryPower.DeliverAllNow();
        }

        // 第二步：给敌方全体施加失衡
        foreach (Creature enemy in base.CombatState?.HittableEnemies ?? Enumerable.Empty<Creature>())
        {
            await PowerCmd.Apply<ImbalancePower>(
                enemy,
                base.DynamicVars["ImbalancePower"].BaseValue,
                base.Owner.Creature,
                this
            );
        }
    }

    // 升级后：失衡 +2（6 -> 8）
    protected override void OnUpgrade()
    {
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(2m);
    }
}