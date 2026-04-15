using BaseLib.Abstracts;
using BaseLib.Extensions;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;
using Godot;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;

public abstract class AngelinaPower : CustomPowerModel
{
    //Loads from CrimsonScepter_Angelina_Mod/images/powers/your_power.png
    public override string CustomPackedIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
            return ResourceLoader.Exists(path) ? path : "power.png".PowerImagePath();
        }
    }

    public override string CustomBigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
            return ResourceLoader.Exists(path) ? path : "power.png".BigPowerImagePath();
        }
    }
}