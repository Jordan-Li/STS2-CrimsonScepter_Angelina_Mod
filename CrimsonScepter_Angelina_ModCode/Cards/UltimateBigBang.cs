using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 费用：0
/// 稀有度：稀有
/// 卡牌类型：攻击
/// 效果：保留。对敌方全体造成40点法术伤害。失去你最右侧的遗物，在本场战斗中此牌耗能增加1。
/// 升级后效果：保留。对敌方全体造成50点法术伤害。失去你最右侧的遗物，在本场战斗中此牌耗能增加1。
/// </summary>
public sealed class UltimateBigBang : AngelinaCard
{
    public override bool IsSpell => true;

    // 没有遗物可失去时，这张牌不能打出。
    protected override bool IsPlayable => base.Owner?.Relics.Any() == true;

    // 可打出时高亮，提示这张牌当前满足使用条件。
    protected override bool ShouldGlowGoldInternal => IsPlayable;

    // 只保留 Retain 关键字；法术由 IsSpell 控制。
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    // 额外显示法术说明，方便理解伤害会吃法术修正。
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("powers", "SPELL.title"),
            new LocString("powers", "SPELL.description"))
    ];

    // 这张牌只有一段法术伤害动态值：40，升级到 50。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(40m, ValueProp.Unpowered | ValueProp.Move)];

    public UltimateBigBang()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    // 打出时：
    // 1. 失去当前最右侧的遗物
    // 2. 对所有敌人造成法术伤害
    // 3. 让此牌本场战斗耗能 +1
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 再做一次保护，避免在异常情况下无遗物可失去。
        if (base.Owner.Relics.Count == 0)
        {
            return;
        }

        // Relics 列表尾部对应当前最右侧的遗物。
        var relicToLose = base.Owner.Relics.Last();
        await RelicCmd.Remove(relicToLose);

        // 按当前法术修正后的伤害值，对所有存活敌人逐个结算。
        decimal damage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);
        List<Creature> enemies = (base.CombatState?.HittableEnemies ?? Enumerable.Empty<Creature>())
            .Where(enemy => enemy.IsAlive)
            .ToList();

        foreach (Creature enemy in enemies)
        {
            await SpellHelper.Damage(
                choiceContext,
                base.Owner.Creature,
                enemy,
                damage,
                this
            );
        }

        // 本场战斗中此牌耗能 +1。
        base.EnergyCost.AddThisCombat(1);
        base.InvokeEnergyCostChanged();
    }

    // 升级后伤害从 40 提高到 50。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(10m);
    }

    // 给描述补上法术修正后的显示伤害。
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        decimal displayedDamage = base.DynamicVars.Damage.BaseValue;
        if (base.IsMutable && base.Owner?.Creature != null)
        {
            displayedDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedDamage);
        }

        description.Add("DisplayedDamage", displayedDamage);
    }
}
