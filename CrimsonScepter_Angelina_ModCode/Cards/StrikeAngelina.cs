using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：打击
/// 费用：1
/// 稀有度：其他
/// 卡牌类型：攻击
/// 效果：造成6点伤害。
/// 升级后效果：造成9点伤害。
/// 备注：初始卡牌
/// </summary>
public sealed class StrikeAngelina : AngelinaCard
{
    // 这张牌带有 Strike 标签，供其他“打击”相关效果识别。
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 定义这张牌的基础伤害数值，供卡面显示和结算共用。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    // 初始化卡牌的基础信息：1费、攻击牌、其他、目标为单体敌人。
    public StrikeAngelina()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 打出时，对选中的敌人造成一次普通攻击伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 攻击牌必须有目标，这里先做空检查，避免后续结算时报错。
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 执行攻击指令，并附带基础斩击特效。
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_flying_slash")
            .Execute(choiceContext);
    }

    // 升级后将伤害提高3点，对应卡面从6提升到9。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}