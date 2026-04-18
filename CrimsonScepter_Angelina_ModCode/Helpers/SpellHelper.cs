using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

/// <summary>
/// 法术词条-辅助工具：
/// 1. 统一判断一张牌是否属于法术体系
/// 2. 对法术伤害/格挡提供旧版法术结算封装
/// 3. 法术仅受 Focus 影响，不走常规力量/敏捷等修正
/// </summary>
public static class SpellHelper
{
    private const ValueProp SpellProps = ValueProp.Unpowered | ValueProp.Move;

    /// <summary>
    /// 判断一张牌是否属于法术。
    /// </summary>
    public static bool IsSpell(CardModel card)
    {
        return card is AngelinaCard angelinaCard && angelinaCard.IsSpell;
    }

    /// <summary>
    /// 兼容旧调用点。
    /// 法术仅额外附加 Focus 修正，并保持结果非负。
    /// </summary>
    public static decimal ModifySpellValue(Creature? source, decimal baseValue)
    {
        decimal focus = source?.GetPower<FocusPower>()?.Amount ?? 0m;
        return decimal.Max(0m, baseValue + focus);
    }

    /// <summary>
    /// 按当前法术规则造成伤害。
    /// </summary>
    public static async Task Damage(
        PlayerChoiceContext choiceContext,
        Creature? source,
        Creature? target,
        decimal amount,
        CardModel? cardSource)
    {
        if (target == null || amount <= 0m)
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            target,
            amount,
            SpellProps,
            source,
            cardSource
        );
    }

    /// <summary>
    /// 按当前法术规则获得格挡。
    /// </summary>
    public static async Task<decimal> GainBlock(
        Creature? source,
        Creature? target,
        decimal amount,
        CardPlay cardPlay)
    {
        _ = source;

        if (target == null || amount <= 0m)
        {
            return 0m;
        }

        return await CreatureCmd.GainBlock(
            target,
            amount,
            SpellProps,
            cardPlay
        );
    }
}
