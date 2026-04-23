using BaseLib.Abstracts;
using BaseLib.Extensions;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;

public abstract class AngelinaEnchantment : CustomEnchantmentModel
{
    protected override string? CustomIconPath
    {
        get
        {
            string path = $"enchantments/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".ImagePath();
            return ResourceLoader.Exists(path) ? path : null;
        }
    }

    protected LocString ResolveEnchantLoc(string suffix)
    {
        string prefixedKey = $"{Id.Entry}.{suffix}";
        if (LocString.Exists("enchantments", prefixedKey))
        {
            return new LocString("enchantments", prefixedKey);
        }

        return new LocString("enchantments", $"{Id.Entry.RemovePrefix()}.{suffix}");
    }
}
