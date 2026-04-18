using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：小礼物
/// 费用：0
/// 稀有度：其他
/// 卡牌类型：攻击
/// 效果：造成3点法术伤。获得3点法术格挡。
/// 升级后效果：造成3点法术伤害。获得3点法术格挡。
/// 备注：初始卡牌
/// </summary>
public sealed class LittleGift : AngelinaCard
{
    // 这张牌是法术牌，会参与法术相关结算与联动。
    public override bool IsSpell => true;

    // 这张牌会提供格挡，供游戏 UI 和相关系统识别。
    public override bool GainsBlock => true;

    // 为这张牌补充“法术”关键词提示。
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new HoverTip(new LocString("powers", "SPELL.title"), new LocString("powers", "SPELL.description"))
    };

    // 定义这张牌的基础法术伤害和法术格挡数值，供卡面显示和结算共用。
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Unpowered | ValueProp.Move),
        new BlockVar(3m, ValueProp.Unpowered | ValueProp.Move)
    };

    // 初始化卡牌的基础信息：0费、攻击牌、其他、目标为单体敌人。
    public LittleGift()
        : base(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 打出时，先对目标造成法术伤害，再为自己获得法术格挡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 先根据当前 Focus 等修正，计算这次实际结算的法术伤害和法术格挡。
        decimal block = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Block.BaseValue);
        decimal damage = SpellHelper.ModifySpellValue(base.Owner.Creature, base.DynamicVars.Damage.BaseValue);

        // 先对选中的敌人造成法术伤害。
        await SpellHelper.Damage(choiceContext, base.Owner.Creature, cardPlay.Target, damage, this);

        // 再为自己结算法术格挡。
        await SpellHelper.GainBlock(base.Owner.Creature, base.Owner.Creature, block, cardPlay);
    }

    // 升级后将伤害和格挡各提高1点。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
        base.DynamicVars.Block.UpgradeValueBy(1m);
    }

    // 给描述补充当前法术修正后的显示值，让卡面数字和实际结算一致。
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        decimal displayedDamage = base.DynamicVars.Damage.BaseValue;
        decimal displayedBlock = base.DynamicVars.Block.BaseValue;

        if (base.IsMutable && base.Owner?.Creature != null)
        {
            displayedDamage = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedDamage);
            displayedBlock = SpellHelper.ModifySpellValue(base.Owner.Creature, displayedBlock);
        }

        description.Add("DisplayedDamage", displayedDamage);
        description.Add("DisplayedBlock", displayedBlock);
    }
}
