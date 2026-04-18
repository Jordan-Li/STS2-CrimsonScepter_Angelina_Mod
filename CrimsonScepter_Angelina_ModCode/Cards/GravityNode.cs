using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：重力节点
/// 费用：2
/// 稀有度：稀有
/// 卡牌类型：攻击
/// 效果：施加10点失衡，造成10点伤害。若此牌斩杀目标或失重，永久使此牌增加3点失衡和伤害。消耗。
/// 升级后效果：施加10点失衡，造成10点伤害。若此牌斩杀目标或失重，永久使此牌增加5点失衡和伤害。消耗。
/// </summary>
public sealed class GravityNode : AngelinaCard
{
    // 这张牌的基础伤害
    private const int BaseDamage = 10;

    // 这张牌的基础失衡
    private const int BaseImbalance = 10;

    // 当前实际伤害
    private int currentDamage = BaseDamage;

    // 当前实际失衡
    private int currentImbalance = BaseImbalance;

    // 这张牌累计获得的额外伤害
    private int increasedDamage;

    // 这张牌累计获得的额外失衡
    private int increasedImbalance;

    /// <summary>
    /// 持久化保存当前伤害。
    /// 这样这张牌在一局内成长后，不会因为离开手牌就丢失。
    /// </summary>
    [SavedProperty]
    public int CurrentDamage
    {
        get => currentDamage;
        set
        {
            AssertMutable();
            currentDamage = value;
            base.DynamicVars.Damage.BaseValue = currentDamage;
        }
    }

    /// <summary>
    /// 持久化保存当前失衡。
    /// </summary>
    [SavedProperty]
    public int CurrentImbalance
    {
        get => currentImbalance;
        set
        {
            AssertMutable();
            currentImbalance = value;
            base.DynamicVars["ImbalancePower"].BaseValue = currentImbalance;
        }
    }

    /// <summary>
    /// 持久化保存累计额外伤害。
    /// </summary>
    [SavedProperty]
    public int IncreasedDamage
    {
        get => increasedDamage;
        set
        {
            AssertMutable();
            increasedDamage = value;
        }
    }

    /// <summary>
    /// 持久化保存累计额外失衡。
    /// </summary>
    [SavedProperty]
    public int IncreasedImbalance
    {
        get => increasedImbalance;
        set
        {
            AssertMutable();
            increasedImbalance = value;
        }
    }

    // 定义四个动态变量：
    // 1. 当前施加的失衡
    // 2. 当前造成的伤害
    // 3. 每次进入失重后增加的失衡值
    // 4. 每次斩杀后增加的伤害值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(CurrentImbalance),
        new DamageVar(CurrentDamage, ValueProp.Move),
        new IntVar("ImbalanceIncrease", 3m),
        new IntVar("DamageIncrease", 3m)
    ];

    // 额外悬浮说明：
    // - 消耗
    // - 失衡
    // - 失重
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<WeightlessPower>()
    ];

    // 添加卡牌关键词：消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    // 费用：2费，类型：攻击牌，稀有度：稀有，目标：任选一个敌人
    public GravityNode()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果没有目标，就直接报错，避免空引用
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 按照游戏原生 Fatal 的定义，先记录这个目标是否允许触发斩杀奖励。
        // 爪牙类目标会因为相关 Power 返回 false，从而不被计入斩杀。
        bool shouldTriggerFatal = cardPlay.Target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

        // 记录目标打出前是否已经处于失重
        bool wasWeightless = cardPlay.Target.GetPower<WeightlessPower>() != null;

        // 第一步：先施加失衡
        await PowerCmd.Apply<ImbalancePower>(
            cardPlay.Target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            this
        );

        // 如果目标原本没有失重，但施加后进入了失重，
        // 说明这张牌满足了一个永久成长条件。
        bool enteredWeightless = !wasWeightless && cardPlay.Target.GetPower<WeightlessPower>() != null;

        // 第二步：如果目标还活着，再造成伤害，并记录这次伤害是否完成了真正的斩杀。
        bool fatalKilledTarget = false;
        if (cardPlay.Target.IsAlive)
        {
            var attackCommand = await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_flying_slash")
                .Execute(choiceContext);

            fatalKilledTarget = shouldTriggerFatal && attackCommand.Results.Any(r => r.WasTargetKilled);
        }

        // 只要这张牌让目标进入失重，或按游戏原生 Fatal 规则完成了斩杀，
        // 就永久同时提高这张牌的失衡和伤害。
        if (enteredWeightless || fatalKilledTarget)
        {
            BuffImbalance(base.DynamicVars["ImbalanceIncrease"].IntValue);
            (base.DeckVersion as GravityNode)?.BuffImbalance(base.DynamicVars["ImbalanceIncrease"].IntValue);

            BuffDamage(base.DynamicVars["DamageIncrease"].IntValue);
            (base.DeckVersion as GravityNode)?.BuffDamage(base.DynamicVars["DamageIncrease"].IntValue);
        }
    }

    // 升级后：
    // 1. 失衡成长值 +2（3 -> 5）
    // 2. 伤害成长值 +2（3 -> 5）
    protected override void OnUpgrade()
    {
        base.DynamicVars["ImbalanceIncrease"].UpgradeValueBy(2m);
        base.DynamicVars["DamageIncrease"].UpgradeValueBy(2m);
        UpdateCurrentStats();
    }

    // 降级后重新同步当前数值
    protected override void AfterDowngraded()
    {
        UpdateCurrentStats();
    }

    // 永久提高这张牌的伤害
    private void BuffDamage(int extraDamage)
    {
        IncreasedDamage += extraDamage;
        UpdateCurrentStats();
    }

    // 永久提高这张牌的失衡
    private void BuffImbalance(int extraImbalance)
    {
        IncreasedImbalance += extraImbalance;
        UpdateCurrentStats();
    }

    // 根据基础值和累计成长值，刷新当前面板数值
    private void UpdateCurrentStats()
    {
        CurrentDamage = BaseDamage + IncreasedDamage;
        CurrentImbalance = BaseImbalance + IncreasedImbalance;
    }
}
