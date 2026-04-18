using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：一触即摔！
/// 费用：3
/// 稀有度：稀有
/// 卡牌类型：技能
/// 效果：若目标敌人有失衡，使其立刻失重，清除其当前失衡，使其失去当前失衡一半的生命值并击晕。每因此失去20点失衡，获得1点能量。消耗。
/// 升级后效果：保留。
/// </summary>
public sealed class TouchAndTumble : AngelinaCard
{
    // 每失去20层失衡，获得1点能量
    private const decimal EnergyPerThreshold = 20m;

    // 进入失重后持续3回合
    private const decimal WeightlessDuration = 3m;

    // 这张牌带有 Exhaust 标签，打出后会被消耗。
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 额外悬浮说明：
    // - 失衡
    // - 失重
    // - 击晕
    // - 消耗
    // - 能量
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<WeightlessPower>(),
        HoverTipFactory.Static(StaticHoverTip.Stun),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.ForEnergy(this)
    ];

    // 定义一个动态能量变量，用来显示“每20点失衡回1能量”这类收益。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    // 初始化卡牌的基础信息：3费、技能牌、稀有、目标为单体敌人。
    public TouchAndTumble()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时，将目标当前累计的失衡直接结算成一次完整的“进入失重”结果，并按移除的失衡量回能。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 技能牌必须有目标，这里先做空检查，避免后续结算时报错。
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 读取目标当前的失衡层数
        ImbalancePower? imbalancePower = cardPlay.Target.GetPower<ImbalancePower>();

        // 如果目标没有失衡，就什么都不做。
        if (imbalancePower == null || imbalancePower.Amount <= 0m)
        {
            return;
        }

        decimal removedImbalance = imbalancePower.Amount;
        
        decimal gainedEnergy = Math.Floor(removedImbalance / EnergyPerThreshold);
        

        // 第一步：移除目标当前全部失衡。
        await PowerCmd.Remove(imbalancePower!);

        // 第二步：让目标立刻进入失重。
        if (base.Owner.Creature == null)
        {
            return;
        }

        await PowerCmd.Apply<WeightlessPower>(cardPlay.Target, WeightlessDuration, base.Owner.Creature, this);

        // 第三步：如果目标还活着，让其失去“原失衡层数一半”的生命值。
        if (cardPlay.Target.IsAlive)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                cardPlay.Target,
                removedImbalance / 2m,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null
            );
        }

        // 第四步：如果目标还活着，再处理回合打断。
        if (cardPlay.Target.IsAlive)
        {
            // 如果目标是玩家，并且正处于它自己的回合，就直接结束回合。
            if (cardPlay.Target.IsPlayer)
            {
                if (base.CombatState != null &&
                    base.CombatState.CurrentSide == cardPlay.Target.Side &&
                    cardPlay.Target.Player != null)
                {
                    PlayerCmd.EndTurn(cardPlay.Target.Player, canBackOut: false);
                }
            }
            else
            {
                // 如果目标是敌人，就直接击晕。
                await CreatureCmd.Stun(cardPlay.Target);
            }
        }

        // 第五步：最后按移除的失衡层数给予能量收益。
        if (gainedEnergy > 0m && base.Owner != null)
        {
            await PlayerCmd.GainEnergy(gainedEnergy, base.Owner);
        }
    }

    // 升级后获得保留。
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
