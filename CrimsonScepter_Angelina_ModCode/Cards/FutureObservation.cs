using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：2
/// 稀有度：稀有
/// 卡牌类型：能力
/// 效果：打出时，选择5张卡组中的牌出现在你下次战斗的抽牌堆顶部。
/// 升级后效果：减1费。
/// 备注：通过“祈愿星”遗物跨战斗保存选择结果。
/// </summary>
public sealed class FutureObservation : AngelinaCard
{
    // 这张牌会显示多人模式警告，以及“祈愿星”遗物的悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("cards", "FUTURE_OBSERVATION.multiplayerWarningTitle"),
            new LocString("cards", "FUTURE_OBSERVATION.multiplayerWarningDescription")),
        .. HoverTipFactory.FromRelic<WishingStar>()
    ];

    // 若已经持有“祈愿星”，则不能重复打出这张牌。
    protected override bool IsPlayable => base.Owner != null &&
                                          !base.Owner.Relics.Any(relic => relic.Id == ModelDb.Relic<WishingStar>().Id);

    // 可打出时用金色边框提示，提醒这张牌当前可以生效。
    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public FutureObservation()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 若已经持有“祈愿星”，则不重复获得。
        if (base.Owner.Relics.Any(relic => relic.Id == ModelDb.Relic<WishingStar>().Id))
        {
            return;
        }

        // 打出时获得“祈愿星”，并通过遗物完成跨战斗选牌与下场战斗置顶。
        await RelicCmd.Obtain(ModelDb.Relic<WishingStar>().ToMutable(), base.Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级后费用从 2 降为 1。
        base.EnergyCost.UpgradeBy(-1);
    }
}
