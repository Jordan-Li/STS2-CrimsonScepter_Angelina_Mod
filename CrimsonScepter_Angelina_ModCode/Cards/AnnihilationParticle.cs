using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：2
/// 稀有度：罕见
/// 卡牌类型：攻击
/// 效果：对所有敌方造成20点法术伤害。如果有目标被斩杀，则这场战斗不再有卡牌奖励。
/// 升级后效果：对所有敌方造成26点法术伤害。如果有目标被斩杀，则这场战斗不再有卡牌奖励。
/// </summary>
public sealed class AnnihilationParticle : AngelinaCard
{
    private static readonly HashSet<(CombatRoom Room, ulong PlayerNetId)> PendingNoCardRewards = [];

    // 这张牌会用到斩杀、法术伤害以及“本场战斗无卡牌奖励”的悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Fatal),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description")),
        HoverTipFactory.FromPower<AnnihilationParticleNoCardRewardPower>()
    ];

    // 维护法术伤害动态值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Unpowered | ValueProp.Move)
    ];

    // 这是攻击牌，但伤害部分按法术伤害结算。
    public override bool IsSpell => true;

    public AnnihilationParticle()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        CombatRoom? combatRoom = base.CombatState?.RunState?.CurrentRoom as CombatRoom;
        if (base.CombatState == null || combatRoom == null)
        {
            return;
        }

        // 先记录哪些敌人符合官方 Fatal 触发条件，爪牙等目标不会被计入斩杀。
        HashSet<Creature> fatalEligibleTargets = base.CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive && enemy.Powers.All(power => power.ShouldOwnerDeathTriggerFatal()))
            .ToHashSet();

        // 统一按法术规则造成全体伤害。不要走 AttackCommand，
        // 否则会被依赖 AfterAttack + ValueProp.Move 的怪物机制误判成普通卡牌命中。
        decimal spellDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);
        IEnumerable<DamageResult> damageResults = await SpellHelper.DamageAll(
            choiceContext,
            base.Owner.Creature,
            base.CombatState.HittableEnemies,
            spellDamage,
            this);

        // 只有真正按 Fatal 规则斩杀了非爪牙目标，才会封锁本场战斗的卡牌奖励。
        bool triggeredFatal = damageResults.Any(result =>
            result.WasTargetKilled &&
            fatalEligibleTargets.Contains(result.Receiver));

        if (!triggeredFatal)
        {
            return;
        }

        PendingNoCardRewards.Add((combatRoom, base.Owner.NetId));

        if (!base.Owner.Creature.HasPower<AnnihilationParticleNoCardRewardPower>())
        {
            await PowerCmd.Apply<AnnihilationParticleNoCardRewardPower>(
                base.Owner.Creature,
                1m,
                base.Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后仅提高法术伤害。
        base.DynamicVars.Damage.UpgradeValueBy(6m);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        decimal displayedDamage = base.DynamicVars.Damage.BaseValue;
        if (base.IsMutable && base.Owner?.Creature != null)
        {
            displayedDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedDamage);
        }

        description.Add("DisplayedDamage", displayedDamage);
        description.Add("CalculatedDamage", displayedDamage);
    }

    // 奖励结算补丁会在战斗奖励生成时消费这条“移除卡牌奖励”记录。
    internal static bool ConsumePendingNoCardReward(CombatRoom room, ulong playerNetId)
    {
        return PendingNoCardRewards.Remove((room, playerNetId));
    }
}

