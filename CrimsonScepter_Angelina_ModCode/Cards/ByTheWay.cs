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
/// 卡牌名：捎带
/// 费用：0
/// 稀有度：普通
/// 卡牌类型：技能
/// 效果：获得4点格挡。寄送这张牌。
/// 升级后效果：获得6点格挡。寄送这张牌。
/// </summary>
public sealed class ByTheWay : AngelinaCard
{
    private bool pendingSelfDelivery;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DeliveryPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4m, ValueProp.Move)
    ];

    public ByTheWay()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 这张牌打出后会自己进入 Exhaust。
        // 等它真正进入 Exhaust 后，再把它加入寄送队列。
        pendingSelfDelivery = true;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card == this)
        {
            return (PileType.Exhaust, CardPilePosition.Bottom);
        }

        return base.ModifyCardPlayResultPileTypeAndPosition(card, isAutoPlay, resources, pileType, position);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        await base.AfterCardChangedPiles(card, oldPileType, source);

        if (!pendingSelfDelivery ||
            card != this ||
            oldPileType == PileType.Exhaust ||
            Pile?.Type != PileType.Exhaust)
        {
            return;
        }

        pendingSelfDelivery = false;

        DeliveryPower? deliveryPower = base.Owner.Creature.GetPower<DeliveryPower>();
        deliveryPower ??= await PowerCmd.Apply<DeliveryPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);

        if (deliveryPower != null)
        {
            await deliveryPower.EnqueueCard(this);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(2m);
    }
}
