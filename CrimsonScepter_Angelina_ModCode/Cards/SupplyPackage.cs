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
/// 卡牌类型：技能牌
/// 稀有度：非凡
/// 费用：0费
/// 效果：施加失衡。送达时获得能量。
/// 升级后效果：提高施加的失衡数值，同时提高送达时获得的能量。
/// 备注：寄送体系中的功能牌，前端负责挂失衡，后端负责回能。
/// </summary>
public sealed class SupplyPackage : DeliveredCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ImbalancePower>(5m),
        new EnergyVar("EnergyGain", 1)
    };

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
        if (cardPlay.Target == null)
        {
            return;
        }

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