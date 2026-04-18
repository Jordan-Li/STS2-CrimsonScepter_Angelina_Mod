using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：补给包裹
/// 费用：0
/// 稀有度：罕见
/// 卡牌类型：技能
/// 效果：施加5点失衡。送达：回复1点能量。
/// 升级后效果：施加8点失衡。送达：回复2点能量。
/// </summary>
public sealed class SupplyPackage : DeliveredCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(5m),
        new EnergyVar("EnergyGain", 1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => WithDeliveredTip(
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.ForEnergy(this),
        HoverTipFactory.FromPower<DeliveryPower>()
    );

    public SupplyPackage()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    protected override async Task OnDelivered(DeliveryPower deliveryPower)
    {
        await PlayerCmd.GainEnergy(base.DynamicVars["EnergyGain"].BaseValue, base.Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(3m);
        base.DynamicVars["EnergyGain"].UpgradeValueBy(1);
    }
}