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
/// 卡牌名：防御
/// 卡牌类型：技能牌
/// 稀有度：基础
/// 费用：1费
/// 效果：获得5点格挡
/// 升级后效果：获得8点格挡
/// 备注：基础卡牌
/// </summary>
public sealed class DefendAngelina : AngelinaCard
{
    // 这张牌会提供格挡，供游戏UI和系统识别
    public override bool GainsBlock => true;

    // 添加卡牌标签：防御
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    // 定义一个动态格挡变量，初始值为5点
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move)
    };

    // 费用：1费，类型：技能牌，稀有度：基础，目标：自己
    public DefendAngelina()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    // 打出时的效果
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    // 升级后，将动态变量 DynamicVars 的格挡提高3点
    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}