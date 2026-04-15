using BaseLib.Abstracts;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;
using Godot;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Character;

// 安洁莉娜的药水池

public class AngelinaPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Angelina.Color;


    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}