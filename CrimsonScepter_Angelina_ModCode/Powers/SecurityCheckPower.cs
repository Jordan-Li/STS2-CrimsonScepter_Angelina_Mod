using System.Linq;
using System.Threading.Tasks;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// 效果：当寄送一张状态牌或诅咒牌时，将其消耗，并对所有敌方施加失衡。
/// 备注：新版单图标寄送系统下，这里不会移除整个 DeliveryPower，只拦截当前这张牌。
/// </summary>
public sealed class SecurityCheckPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ImbalancePower>()
    ];

    public async Task<bool> TryInterceptDelivery(DeliveryPower deliveryPower, CardModel card)
    {
        // 这里不直接操作 DeliveryPower 本体，只判断当前这张被寄送的牌是否需要被拦截。
        _ = deliveryPower;

        if (card.Owner != base.Owner.Player || (card.Type != CardType.Status && card.Type != CardType.Curse))
        {
            return false;
        }

        // 若寄送的是状态牌或诅咒牌，则将其改为消耗，并对所有敌方施加失衡。
        Flash();

        foreach (var enemy in (base.CombatState?.HittableEnemies ?? Enumerable.Empty<MegaCrit.Sts2.Core.Entities.Creatures.Creature>())
                 .Where(enemy => enemy.IsAlive))
        {
            await PowerCmd.Apply<ImbalancePower>(enemy, base.Amount, base.Owner, null);
        }

        return true;
    }
}