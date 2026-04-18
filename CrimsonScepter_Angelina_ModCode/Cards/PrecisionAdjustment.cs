using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：精密调整
/// 费用：0
/// 稀有度：普通
/// 卡牌类型：技能
/// 效果：对所有敌方施加12点失衡，并在下回合开始时失去5点失衡。
/// 升级后效果：对所有敌方施加18点失衡，并在下回合开始时失去5点失衡。
/// </summary>
public sealed class PrecisionAdjustment : AngelinaCard
{
    // 动态变量：
    // 1. 施加的失衡数值
    // 2. 下回合失去的失衡数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(12m),
        new PowerVar<PrecisionAdjustmentPower>(5m)
    ];

    // 额外悬浮说明：
    // 1. 失衡
    // 2. 精密调整
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<PrecisionAdjustmentPower>()
    ];

    // 初始化卡牌的基础信息：0费、技能、普通、目标为所有敌人。
    public PrecisionAdjustment()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    // 打出时，对所有敌人施加失衡，并附加“下回合失去失衡”的延时效果。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (Creature enemy in base.CombatState?.HittableEnemies ?? Enumerable.Empty<Creature>())
        {
            // 第一步：对目标施加失衡。
            await PowerCmd.Apply<ImbalancePower>(
                enemy,
                base.DynamicVars["ImbalancePower"].BaseValue,
                base.Owner.Creature,
                this
            );

            // 第二步：施加延时 Power，在下回合开始时扣回固定数值的失衡。
            await PowerCmd.Apply<PrecisionAdjustmentPower>(
                enemy,
                base.DynamicVars["PrecisionAdjustmentPower"].BaseValue,
                base.Owner.Creature,
                this
            );
        }
    }

    // 升级后仅提高施加的失衡数值。
    protected override void OnUpgrade()
    {
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(6m);
    }
}
