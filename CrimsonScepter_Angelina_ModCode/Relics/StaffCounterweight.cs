using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;

/// <summary>
/// 遗物名：法杖配重
/// 稀有度：非凡
/// 效果：你的攻击会使目标额外失去1层飞行。
/// </summary>
public sealed class StaffCounterweight : AngelinaRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<FlyPower>(1m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<FlyPower>()
    };

    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != base.Owner.Creature ||
            command.ModelSource is not CardModel cardSource ||
            cardSource.Type != CardType.Attack)
        {
            return;
        }

        List<FlyPower> flyPowers = command.Results
            .Select(result => result.Receiver.GetPower<FlyPower>())
            .Where(power => power != null && power.Amount > 0m)
            .Distinct()
            .ToList()!;

        if (flyPowers.Count == 0)
        {
            return;
        }

        Flash();
        foreach (FlyPower flyPower in flyPowers)
        {
            await PowerCmd.Decrement(flyPower);
        }
    }
}