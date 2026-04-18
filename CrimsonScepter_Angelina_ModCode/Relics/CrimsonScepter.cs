using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;

/// <summary>
/// 遗物名：绯红权杖
/// 稀有度：初始
/// 效果：
/// 当敌方角色从浮空状态变为非浮空状态时，使其获得失衡值。
/// </summary>
public sealed class CrimsonScepter : AngelinaRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 先按旧版语义写成 10；如果你记得旧版不是 10，只改这里
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ImbalancePower>(10m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<FlyPower>(),
        HoverTipFactory.FromPower<ImbalancePower>()
    };

    /// <summary>
    /// 监听 Power 层数变化。
    /// 当敌人的 FlyPower 从 >0 下降到 <=0 时，视为“从浮空变为非浮空”，施加失衡。
    /// </summary>
    public override async Task BeforePowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        _ = applier;
        
        // 临时飞行到期导致的落地，不触发初始遗物
        if (TemporaryFlyPower.IsResolvingExpiration)
        {
            return;
        }
        
        // 只关心飞行层数下降
        if (power is not FlyPower || amount >= 0m)
        {
            return;
        }

        // 只对敌方生效
        if (target.Side == base.Owner.Creature.Side)
        {
            return;
        }

        // 当前 > 0，变化后 <= 0，说明正好从浮空落地
        decimal currentAmount = power.Amount;
        decimal nextAmount = currentAmount + amount;
        if (currentAmount <= 0m || nextAmount > 0m)
        {
            return;
        }

        Flash();

        await PowerCmd.Apply<ImbalancePower>(
            target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            cardSource
        );
    }
}
