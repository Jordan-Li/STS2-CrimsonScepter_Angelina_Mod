using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Powers;

/// <summary>
/// Power名：湮灭粒子无卡牌奖励
/// Power类型：状态型Power
/// 效果：标记本场战斗因湮灭粒子斩杀而不再获得卡牌奖励。
/// </summary>
public sealed class AnnihilationParticleNoCardRewardPower : AngelinaPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;
}