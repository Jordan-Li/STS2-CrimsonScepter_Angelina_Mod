using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：兼职工作
/// 效果：回合结束时，若本回合没有打出过攻击牌，则恢复生命值，然后移除自身。
/// </summary>
public sealed class PartTimeJobPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 回合结束时检查这一回合是否打出过攻击牌，若没有则恢复生命值，然后移除此 Power。
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 只在自身这一侧的回合结束时触发；缺少玩家或战斗状态时直接跳过。
        if (side != base.Owner.Side || base.Owner.Player == null || base.CombatState == null)
        {
            return;
        }

        // 遍历本回合开始过的出牌记录，只要其中有自己打出的攻击牌，就视为不满足兼职工作的恢复条件。
        bool playedAttackThisTurn = CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
            entry.HappenedThisTurn(base.CombatState) &&
            entry.CardPlay.Card.Owner == base.Owner.Player &&
            entry.CardPlay.Card.Type == CardType.Attack);

        // 本回合没有打过攻击牌时，执行恢复。
        if (!playedAttackThisTurn)
        {
            Flash();
            await CreatureCmd.Heal(base.Owner, base.Amount);
        }

        // 无论是否恢复，回合结束后都移除此 Power。
        await PowerCmd.Remove(this);
    }
}