using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Helpers;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;

/// <summary>
/// 遗物名：秘杖·绯红权杖
/// 稀有度：初始（隐藏/特殊用）
/// 效果：
/// 1. 当敌人失去浮空时，使其获得15点失衡值。
/// 2. 每回合开始时，给予所有单位1层临时飞行。
/// </summary>
public sealed class SecretCrimsonScepter : AngelinaRelic
{
    // 当前项目图片文件名还是旧 typo：sercet_secrimson_scepter.png
    public override string PackedIconPath
    {
        get
        {
            string typoPath = "sercet_secrimson_scepter.png".RelicImagePath();
            return ResourceLoader.Exists(typoPath) ? typoPath : base.PackedIconPath;
        }
    }

    protected override string BigIconPath
    {
        get
        {
            string typoPath = "sercet_secrimson_scepter.png".RelicImagePath();
            return ResourceLoader.Exists(typoPath) ? typoPath : base.BigIconPath;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<ImbalancePower>(15m),
        new PowerVar<TemporaryFlyPower>(1m)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<ImbalancePower>(),
        HoverTipFactory.FromPower<TemporaryFlyPower>(),
        HoverTipFactory.FromPower<FlyPower>()
    };

    public override async Task BeforePowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        _ = applier;
        
        if (TemporaryFlyPower.IsResolvingExpiration)
        {
            return;
        }
        
        if (!CombatManager.Instance.IsInProgress || power is not FlyPower || amount >= 0m)
        {
            return;
        }

        if (target.Side == base.Owner.Creature.Side || !target.IsAlive)
        {
            return;
        }

        decimal currentAmount = power.Amount;
        decimal nextAmount = currentAmount + amount;
        if (currentAmount <= 0m || nextAmount > 0m)
        {
            return;
        }

        if (!AirborneHelper.BecameGroundedByFlyChange(target, amount))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<ImbalancePower>(
            target,
            base.DynamicVars["ImbalancePower"].BaseValue,
            base.Owner.Creature,
            cardSource
        );
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        _ = choiceContext;

        if (side != base.Owner.Creature.Side)
        {
            return;
        }

        List<Creature> creatures = combatState.Creatures
            .Where(creature => creature.IsAlive)
            .ToList();

        if (creatures.Count == 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<TemporaryFlyPower>(
            creatures,
            base.DynamicVars["TemporaryFlyPower"].BaseValue,
            base.Owner.Creature,
            null
        );
    }
}