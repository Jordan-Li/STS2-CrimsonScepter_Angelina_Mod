using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "CrimsonScepter_Angelina_Mod"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }
}