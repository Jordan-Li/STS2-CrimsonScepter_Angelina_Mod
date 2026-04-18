using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// 效果：
/// 1. 接下来若干回合开始时，获得1点力量、1点敏捷和1点集中。
/// 2. 彩蛋：打出攻击牌时有10%概率额外打出一次。
/// </summary>
public sealed class QualityVisitorPower : AngelinaPower
{
    private const float SecretReplayChance = 0.1f;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<FocusPower>()
    ];

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        // 彩蛋逻辑：只有自己打出的攻击牌，才有概率额外多打一次。
        if (card.Owner.Creature != base.Owner)
        {
            return playCount;
        }

        if (card.Type != CardType.Attack)
        {
            return playCount;
        }

        if (base.Owner.Player?.RunState?.Rng == null)
        {
            return playCount;
        }

        if (base.Owner.Player.RunState.Rng.Niche.NextFloat() >= SecretReplayChance)
        {
            return playCount;
        }

        return playCount + 1;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        // 若成功触发彩蛋，则显示一次隐藏提示能力。
        if (card.Owner.Creature != base.Owner || card.Type != CardType.Attack)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<QualityVisitorSecretPower>(base.Owner, 1m, base.Owner, card);
    }

    public override async Task AfterEnergyReset(Player player)
    {
        // 每回合开始时获得力量、敏捷和集中各 1 点，然后减少持续回合数。
        if (player != base.Owner.Player)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(base.Owner, 1m, base.Owner, null);
        await PowerCmd.Apply<DexterityPower>(base.Owner, 1m, base.Owner, null);
        await PowerCmd.Apply<FocusPower>(base.Owner, 1m, base.Owner, null);
        await PowerCmd.Decrement(this);
    }
}
