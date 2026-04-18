using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// 效果：
/// 1. 每当你打出一张攻击牌时，若其目标没有浮空，则给予其等同于本 Power 层数的飞行。
/// 2. 每打出一张牌，对所有处于浮空状态的敌人造成2点无强化伤害。
/// </summary>
public sealed class SpecialGravityModulePower : AngelinaPower
{
    private const decimal FlyingDamage = 2m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 只响应自己打出的牌。
        if (cardPlay.Card.Owner != base.Owner.Player)
        {
            return;
        }

        bool appliedFlyThisTime = false;

        // 若打出的是攻击牌，且目标是敌人、当前没有浮空，则给予飞行。
        if (cardPlay.Card.Type == CardType.Attack)
        {
            Creature? target = cardPlay.Target;
            if (target != null && target.Side != base.Owner.Side && !AirborneHelper.IsAirborne(target))
            {
                appliedFlyThisTime = true;
                await PowerCmd.Apply<FlyPower>(target, base.Amount, base.Owner, cardPlay.Card);
            }
        }

        // 找出所有当前处于浮空状态的敌人；这里统一使用浮空判定入口。
        Creature[] airborneEnemies = (base.CombatState?.HittableEnemies ?? Enumerable.Empty<Creature>())
            .Where(AirborneHelper.IsAirborne)
            .ToArray();

        if (airborneEnemies.Length == 0)
        {
            if (appliedFlyThisTime)
            {
                Flash();
            }

            return;
        }

        // 每打出一张牌，都对浮空敌人造成固定伤害。
        Flash();
        await CreatureCmd.Damage(context, airborneEnemies, FlyingDamage, ValueProp.Unpowered, base.Owner, cardPlay.Card);
    }
}
