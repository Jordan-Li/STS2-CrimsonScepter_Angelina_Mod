using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;

/// <summary>
/// 卡牌名：小礼物
/// 卡牌类型：攻击牌
/// 稀有度：基础
/// 费用：0费
/// 效果：造成3点伤害，并获得3点格挡
/// 升级后效果：造成4点伤害，并获得4点格挡
/// 备注：当前是可运行版，后续可接回法术体系
/// </summary>
public sealed class LittleGift : AngelinaCard
{
    // 这张牌会提供格挡，供游戏UI和系统识别
    public override bool GainsBlock => true;

    // 定义两个动态变量：
    // 第一个是伤害，初始值为3点
    // 第二个是格挡，初始值为3点
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
        new BlockVar(3m, ValueProp.Move)
    };

    // 费用：0费，类型：攻击牌，稀有度：基础，打击目标：任选一个目标
    public LittleGift()
        : base(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果没有目标，就直接报错，避免空引用
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 先给自己格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        // 再对目标造成伤害
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)                                  // 说明这次伤害来自这张牌
            .Targeting(cardPlay.Target)                      // 指定攻击目标
            .WithHitFx("vfx/vfx_flying_slash")           // 命中特效
            .Execute(choiceContext);                         // 真正执行
    }

    // 升级后，将动态变量 DynamicVars 的伤害和格挡各提高1点
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
        base.DynamicVars.Block.UpgradeValueBy(1m);
    }
}