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
        LogStartupDiagnostics();
    }

    private static void LogStartupDiagnostics()
    {
        string assemblyVersion = typeof(MainFile).Assembly.GetName().Version?.ToString() ?? "unknown";
        string assemblyLocation = typeof(MainFile).Assembly.Location;
        string assemblyTimestamp = System.IO.File.Exists(assemblyLocation)
            ? File.GetLastWriteTime(assemblyLocation).ToString("yyyy-MM-dd HH:mm:ss")
            : "unknown";

        const string restSiteScenePath = $"{ResPath}/scenes/rest_site/characters/angelina_rest_site.tscn";
        const string merchantScenePath = $"{ResPath}/scenes/merchant/characters/angelina_merchant.tscn";

        Logger.Info(
            $"[Startup] version={assemblyVersion} assemblyTime={assemblyTimestamp} " +
            $"restSiteScene={ResourceLoader.Exists(restSiteScenePath)} " +
            $"merchantScene={ResourceLoader.Exists(merchantScenePath)}");
    }
}
