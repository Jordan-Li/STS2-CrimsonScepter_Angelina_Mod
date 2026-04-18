namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;

/// <summary>
/// 用来拼接地址字符串
/// </summary>

public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return $"{MainFile.ResPath}/images/{path}";
    }       // 

    public static string CardImagePath(this string path)
    {
        return $"{MainFile.ResPath}/images/card_portraits/{path}";
    }

    public static string BigCardImagePath(this string path)
    {
        return $"{MainFile.ResPath}/images/card_portraits/big/{path}";
    }

    public static string PowerImagePath(this string path)
    {
        return $"{MainFile.ResPath}/images/powers/{path}";
    }

    public static string BigPowerImagePath(this string path)
    {
        return $"{MainFile.ResPath}/images/powers/big/{path}";
    }

    public static string RelicImagePath(this string path)
    {
        return $"{MainFile.ResPath}/images/relics/{path}";
    }

    public static string BigRelicImagePath(this string path)
    {
        return $"{MainFile.ResPath}/images/relics/big/{path}";
    }

    public static string CharacterUiPath(this string path)
    {
        return $"{MainFile.ResPath}/images/charui/{path}";
    }

    public static string ScenesPath(this string path)
    {
        return $"{MainFile.ResPath}/scenes/{path}";
    }

    public static string CustomVisualPath(this string path)
    {
        return $"{MainFile.ResPath}/scenes/creature_visuals/{path}";
    }

    public static string CustomEnergyCounterPath(this string path)
    {
        return $"{MainFile.ResPath}/scenes/combat/energy_counters/{path}";
    }

    public static string CustomRestSiteAnimPath(this string path)
    {
        return $"{MainFile.ResPath}/scenes/rest_site/characters/{path}";
    }

    public static string CustomCharacterSelectBg(this string path)
    {
        return $"{MainFile.ResPath}/scenes/screens/char_select/{path}";
    }

    public static string CustomMerchantPath(this string path)
    {
        return $"{MainFile.ResPath}/scenes/merchant/characters/{path}";
    }

    public static string HandUiPath(this string path)
    {
        return $"{MainFile.ResPath}/images/hands/{path}";
    }
}