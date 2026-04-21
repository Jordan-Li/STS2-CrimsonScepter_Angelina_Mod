using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：凝滞师
/// Power类型：状态型Power
/// 效果：每当你打出一张攻击牌时，为其目标施加等同于本 Power 层数的停顿。
/// </summary>
public sealed class DecelBinderPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    // 额外悬浮说明：补充“停顿”效果的说明文本。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StaggerPower>()
    ];

    // 每当自己打出攻击牌后，对该牌实际锁定的目标施加停顿。
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 只响应自己打出的攻击牌，避免队友或其他类型牌误触发。
        if (cardPlay.Card.Owner != base.Owner.Player || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        // 没有目标的攻击牌不施加停顿。
        Creature? target = cardPlay.Target;
        if (target == null)
        {
            return;
        }

        // 对本次攻击目标施加等同于当前层数的停顿。
        Flash();
        await PowerCmd.Apply<StaggerPower>(target, base.Amount, base.Owner, cardPlay.Card);
    }
}
