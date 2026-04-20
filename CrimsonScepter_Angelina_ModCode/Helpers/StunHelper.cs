using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

internal static class StunHelper
{
    public static Task ForceStun(Creature creature, string? nextMoveId = null)
    {
        return ForceStun(creature, _ => Task.CompletedTask, nextMoveId);
    }

    public static Task ForceStun(Creature creature, Func<IReadOnlyList<Creature>, Task> stunMove, string? nextMoveId = null)
    {
        MonsterModel monster = creature.Monster ?? throw new InvalidOperationException("Can't stun a player.");

        if (creature.CombatState == null || creature.IsDead)
        {
            Log.Info($"[AngelinaDebug][StunHelper] Skip stun owner={monster.Id.Entry} combatStateNull={creature.CombatState == null} isDead={creature.IsDead}");
            return Task.CompletedTask;
        }

        string followUpStateId = nextMoveId ?? string.Empty;
        if (string.IsNullOrEmpty(followUpStateId))
        {
            MonsterMoveStateMachine? moveStateMachine = monster.MoveStateMachine;
            string? lastLoggedStateId = moveStateMachine?.StateLog.LastOrDefault()?.Id;
            followUpStateId = lastLoggedStateId ?? monster.NextMove?.Id ?? string.Empty;
        }

        MoveState state = new("STUNNED", Wrapper, new StunIntent())
        {
            FollowUpStateId = followUpStateId,
            MustPerformOnceBeforeTransitioning = true
        };

        string nextMoveBefore = monster.NextMove?.Id ?? "<none>";
        Log.Info($"[AngelinaDebug][StunHelper] Force stun owner={monster.Id.Entry} nextMoveBefore={nextMoveBefore} followUp={followUpStateId}");
        monster.SetMoveImmediate(state, forceTransition: true);
        string nextMoveAfter = monster.NextMove?.Id ?? "<none>";
        Log.Info($"[AngelinaDebug][StunHelper] Force stun applied owner={monster.Id.Entry} nextMoveAfter={nextMoveAfter}");
        return Task.CompletedTask;

        async Task Wrapper(IReadOnlyList<Creature> targets)
        {
            NStunnedVfx? vfx = NStunnedVfx.Create(creature);
            if (vfx != null)
            {
                Callable.From(delegate
                {
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
                }).CallDeferred();
            }

            await stunMove(targets);
        }
    }
}
