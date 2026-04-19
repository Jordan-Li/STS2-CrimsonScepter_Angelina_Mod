using System.Threading.Tasks;
using System.Collections.Generic;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;

/// <summary>
/// 法术相关的公共结算工具。
/// 1. 统一判断某张牌是否属于法术。
/// 2. 统一处理 Focus 对法术伤害/法术格挡的修正。
/// 3. 提供法术伤害和法术格挡的实际结算入口。
/// </summary>
public static class SpellHelper
{
    private const ValueProp SpellProps = ValueProp.Unpowered | ValueProp.Move;

    public static bool IsSpell(CardModel card)
    {
        return card is AngelinaCard angelinaCard && angelinaCard.IsSpell;
    }

    public static decimal ModifySpellValue(Creature? source, decimal baseValue)
    {
        decimal focus = source?.GetPower<FocusPower>()?.Amount ?? 0m;
        return decimal.Max(0m, baseValue + focus);
    }

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

    public static async Task<IEnumerable<DamageResult>> DamageAll(
        PlayerChoiceContext choiceContext,
        Creature? source,
        IEnumerable<Creature> targets,
        decimal amount,
        CardModel? cardSource)
    {
        if (amount <= 0m)
        {
            return [];
        }

        return await CreatureCmd.Damage(
            choiceContext,
            targets,
            amount,
            SpellProps,
            source,
            cardSource
        );
    }
    
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