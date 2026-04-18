using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：RealityManipulationPower
/// 效果：当你的 Exhaust 数量发生变化时，获得格挡。
/// </summary>
public sealed class RealityManipulationPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 只要拥有者的牌进出 Exhaust，且数量发生变化，就获得格挡。
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        // 只统计拥有者自己的牌。
        if (card.Owner != base.Owner.Player)
        {
            return;
        }

        // 只有当一张牌进入或离开消耗牌堆时，才算“消耗牌堆数量变动”。
        PileType newPileType = card.Pile?.Type ?? PileType.None;
        bool exhaustCountChanged = oldPileType == PileType.Exhaust ^ newPileType == PileType.Exhaust;
        if (!exhaustCountChanged)
        {
            return;
        }

        // 每次数量变动时，获得等同于本 Power 层数的格挡。
        Flash();
        await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
    }
}