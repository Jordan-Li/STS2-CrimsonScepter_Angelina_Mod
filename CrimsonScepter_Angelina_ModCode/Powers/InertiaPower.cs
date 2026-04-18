using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：InertiaPower
/// 效果：
/// 1. 抽牌前，若场上已有失重敌人，则额外抽牌。
/// 2. 当敌人新进入失重时，也会立刻额外抽牌。
/// 备注：旧项目监听 ImbalanceStatePower；这里改为监听 WeightlessPower。
/// </summary>
public sealed class InertiaPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：失重。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeightlessPower>()
    ];

    // 在正常抽牌前，如果已经有失重敌人，则额外抽牌。
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner.Player || !HasWeightlessEnemy())
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, base.Amount, player);
    }

    // 当任意 Power 数值变化后，尝试监听敌方新增的失重。
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount > 0m)
        {
            await TryTriggerFromPower(power);
        }
    }

    // 如果是敌方新获得失重，则立刻额外抽牌。
    private async Task TryTriggerFromPower(PowerModel power)
    {
        if (power is not WeightlessPower || power.Owner.Side == base.Owner.Side || base.Owner.Player == null)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), base.Amount, base.Owner.Player);
    }

    // 当前场上是否存在处于失重的敌方单位。
    private bool HasWeightlessEnemy()
    {
        return (base.CombatState?.HittableEnemies ?? Enumerable.Empty<Creature>())
            .Any(enemy => enemy.Side != base.Owner.Side && enemy.HasPower<WeightlessPower>());
    }
}
