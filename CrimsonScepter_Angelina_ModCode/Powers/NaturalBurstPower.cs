using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：NaturalBurstPower
/// 效果：当拥有者造成法术伤害后，对所有敌人施加中毒。
/// </summary>
public sealed class NaturalBurstPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：
    // 1. 中毒
    // 2. 法术
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PoisonPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 当拥有者用“法术伤害”打中敌人后，对所有敌人上毒。
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // 只在自己对敌方造成了未被完全格挡的伤害时触发。
        if (dealer != base.Owner || result.UnblockedDamage <= 0 || target.Side == base.Owner.Side)
        {
            return;
        }

        // 只有法术伤害才会触发自然爆发。
        if (!props.HasFlag(ValueProp.Move) || !props.HasFlag(ValueProp.Unpowered))
        {
            return;
        }

        // 找出当前仍然存活且可被作用的所有敌方单位。
        List<Creature> enemies = base.CombatState
            .GetOpponentsOf(base.Owner)
            .Where(enemy => enemy.IsAlive && enemy.IsHittable)
            .ToList();

        if (enemies.Count == 0)
        {
            return;
        }

        // 对所有敌方施加等同于本 Power 层数的中毒。
        Flash();
        await PowerCmd.Apply<PoisonPower>(enemies, base.Amount, base.Owner, cardSource);
    }
}
