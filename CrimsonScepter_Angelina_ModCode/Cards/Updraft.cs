using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：上升气流
/// 费用：2
/// 稀有度：罕见
/// 卡牌类型：能力
/// 效果：回合开始时，获得1层临时飞行。若你因受到攻击脱离浮空，向抽牌堆内添加一张眩晕。
/// 升级后效果：减1费。
/// </summary>
public sealed class Updraft : AngelinaCard
{
    // 动态变量：上升气流的基础层数。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<UpdraftPower>(1m)
    ];

    // 额外悬浮说明：
    // 1. 临时飞行
    // 2. 眩晕
    // 3. 上升气流
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TemporaryFlyPower>(),
        HoverTipFactory.FromCard<Dazed>(),
        HoverTipFactory.FromPower<UpdraftPower>()
    ];

    // 初始化卡牌的基础信息：2费、能力、罕见、目标为自己。
    public Updraft()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时，对自己施加上升气流。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施法表现：播放施法动作。
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 对自己施加上升气流。
        await PowerCmd.Apply<UpdraftPower>(
            base.Owner.Creature,
            base.DynamicVars["UpdraftPower"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    // 升级后仅减少1点费用。
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}