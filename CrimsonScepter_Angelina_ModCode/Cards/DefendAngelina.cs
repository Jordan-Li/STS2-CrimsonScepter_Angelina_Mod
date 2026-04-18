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
/// 费用：1
/// 稀有度：其他
/// 卡牌类型：技能
/// 效果：获得5点格挡。
/// 升级后效果：获得8点格挡。
/// 备注：初始卡牌
/// </summary>
public sealed class DefendAngelina : AngelinaCard
{
    // 这张牌会提供格挡，供游戏 UI 和相关系统识别。
    public override bool GainsBlock => true;

    // 这张牌带有 Defend 标签，供其他“防御”相关效果识别。
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    // 定义这张牌的基础格挡数值，供卡面显示和结算共用。
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move)
    };

    // 初始化卡牌的基础信息：1费、技能牌、其他、目标为自己。
    public DefendAngelina()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    // 打出时，为自己提供一次基础格挡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 执行获得格挡的结算。
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    // 升级后将格挡提高3点，对应卡面从5提升到8。
    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}