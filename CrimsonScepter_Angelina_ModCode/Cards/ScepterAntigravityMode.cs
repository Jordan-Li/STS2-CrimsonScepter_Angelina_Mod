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
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：1
/// 稀有度：其他
/// 卡牌类型：攻击
/// 效果：对敌方全体施加18点失衡值和1层临时飞行。造成18点法术伤害。
/// 升级后效果：对敌方全体施加25点失衡值与1层临时飞行。造成25点法术伤害。
/// </summary>
public sealed class ScepterAntigravityMode : AngelinaCard
{
    // 这张牌会用到失衡、临时飞行、飞行和法术伤害的悬浮说明。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<TemporaryFlyPower>(),
        HoverTipFactory.FromPower<FlyPower>(),
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 维护失衡、法术伤害和临时飞行三个动态数值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ImbalancePower>(18m),
        new DamageVar(18m, ValueProp.Unpowered | ValueProp.Move),
        new PowerVar<TemporaryFlyPower>(1m)
    ];

    // 这是攻击牌，但伤害部分按法术伤害结算。
    public override bool IsSpell => true;

    public ScepterAntigravityMode()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先取当前所有可命中的存活敌人；若没有目标，则直接结束。
        List<Creature> enemies = (base.CombatState?.HittableEnemies ?? Enumerable.Empty<Creature>())
            .Where(enemy => enemy.IsAlive)
            .ToList();

        if (enemies.Count == 0)
        {
            return;
        }

        // 在逐个目标结算前，先计算当前法术伤害显示值对应的实际伤害。
        decimal spellDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);

        // 按表格顺序逐个敌人结算：先施加失衡和临时飞行，再造成法术伤害。
        foreach (Creature enemy in enemies)
        {
            await PowerCmd.Apply<ImbalancePower>(
                enemy,
                base.DynamicVars["ImbalancePower"].BaseValue,
                base.Owner.Creature,
                this
            );

            await PowerCmd.Apply<TemporaryFlyPower>(
                enemy,
                base.DynamicVars["TemporaryFlyPower"].BaseValue,
                base.Owner.Creature,
                this
            );

            await SpellHelper.Damage(
                choiceContext,
                base.Owner.Creature,
                enemy,
                spellDamage,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后同时提高失衡值和法术伤害。
        base.DynamicVars["ImbalancePower"].UpgradeValueBy(7m);
        base.DynamicVars.Damage.UpgradeValueBy(7m);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        // 为本地化描述补上法术修正后的实际显示伤害。
        base.AddExtraArgsToDescription(description);

        decimal displayedDamage = base.DynamicVars.Damage.BaseValue;
        if (base.IsMutable && base.Owner?.Creature != null)
        {
            displayedDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedDamage);
        }

        description.Add("DisplayedDamage", displayedDamage);
    }
}
