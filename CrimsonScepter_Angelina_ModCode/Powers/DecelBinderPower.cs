using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：DecelBinderPower
/// 效果：每当你打出攻击牌，对其目标施加迟滞。
/// </summary>
public sealed class DecelBinderPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：停顿。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StaggerPower>()
    ];

    // 打出攻击牌后，对被指定的目标施加停顿。
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 只在自己打出攻击牌时触发。
        if (cardPlay.Card.Owner.Creature != base.Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        // 没有目标或目标已死亡时，不施加停顿。
        if (cardPlay.Target == null || !cardPlay.Target.IsAlive)
        {
            return;
        }

        // 对攻击目标施加等同于本 Power 层数的停顿。
        Flash();
        await PowerCmd.Apply<StaggerPower>(cardPlay.Target, base.Amount, base.Owner, cardPlay.Card);
    }
}