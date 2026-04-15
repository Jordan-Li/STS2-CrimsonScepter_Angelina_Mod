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
/// 卡牌类型：技能牌
/// 稀有度：稀有
/// 费用：3费
/// 效果：若目标有失衡，使目标立刻进入失重。每因此失去20层失衡，获得1点能量。
/// 升级后效果：获得保留
/// 备注：这是失衡体系的终结牌，直接把累计的失衡转化为失重和掉血
/// </summary>
public sealed class TouchAndTumble : AngelinaCard
{
    // 每失去20层失衡，获得1点能量
    private const decimal EnergyPerThreshold = 20m;

    // 进入失重后持续3回合
    private const decimal WeightlessDuration = 3m;

    // 添加卡牌标签：消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Exhaust
    };

    // 额外悬浮说明：
    // - 失衡
    // - 失重
    // - 击晕
    // - 消耗
    // - 能量
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<WeightlessPower>(),
        HoverTipFactory.Static(StaticHoverTip.Stun),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.ForEnergy(this)
    };

    // 定义一个动态能量变量，初始显示为1个能量图标
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1)
    };

    // 费用：3费，类型：技能牌，稀有度：稀有，目标：任选一个敌人
    public TouchAndTumble()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果没有目标，就直接报错，避免空引用
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 读取目标当前的失衡层数
        ImbalancePower? imbalancePower = cardPlay.Target.GetPower<ImbalancePower>();

        // 如果目标没有失衡，就什么都不做
        if (imbalancePower == null || imbalancePower.Amount <= 0m)
        {
            return;
        }

        decimal removedImbalance = imbalancePower.Amount;
        
        decimal gainedEnergy = Math.Floor(removedImbalance / EnergyPerThreshold);
        

        // 第一步：移除目标当前全部失衡
        await PowerCmd.Remove(imbalancePower!);

        // 第二步：让目标立刻进入失重
        if (base.Owner.Creature == null)
        {
            return;
        }

        await PowerCmd.Apply<WeightlessPower>(cardPlay.Target, WeightlessDuration, base.Owner.Creature, this);

        // 第三步：如果目标还活着，让其失去“原失衡层数一半”的生命
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

        // 第四步：如果目标还活着，再处理回合打断
        if (cardPlay.Target.IsAlive)
        {
            // 如果目标是玩家，并且正处于它自己的回合，就直接结束回合
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
                // 如果目标是敌人，就直接击晕
                await CreatureCmd.Stun(cardPlay.Target);
            }
        }

        // 第五步：最后给予玩家能量收益
        if (gainedEnergy > 0m && base.Owner != null)
        {
            await PlayerCmd.GainEnergy(gainedEnergy, base.Owner);
        }
    }

    // 升级后获得保留
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}