using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Character;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;

[Pool(typeof(AngelinaCardPool))]
public abstract class AngelinaCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    private const string FallbackTestCard = "TestCardBase.png";

    private string CardFileName => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";

    private string SmallPortraitPath => CardFileName.CardImagePath();
    private string BigPortraitFilePath => CardFileName.BigCardImagePath();

    // beta 默认按 images/card_portraits/beta/<id>.png 读取
    private string BetaSmallPortraitPath => $"beta/{CardFileName}".CardImagePath();

    // 如果你以后想单独给 beta 大图，也支持 images/card_portraits/big/beta/<id>.png
    private string BetaBigPortraitPath => $"beta/{CardFileName}".BigCardImagePath();

    private static bool Exists(string path) => ResourceLoader.Exists(path);

    public virtual bool IsSpell => false;
    
    public override string PortraitPath
    {
        get
        {
            if (Exists(SmallPortraitPath))
                return SmallPortraitPath;

            if (Exists(BetaSmallPortraitPath))
                return BetaSmallPortraitPath;

            return FallbackTestCard.CardImagePath();
        }
    }

    public override string CustomPortraitPath
    {
        get
        {
            if (Exists(BigPortraitFilePath))
                return BigPortraitFilePath;

            if (Exists(BetaBigPortraitPath))
                return BetaBigPortraitPath;

            // 没有大图时，允许直接拿 beta 小图顶上
            if (Exists(BetaSmallPortraitPath))
                return BetaSmallPortraitPath;

            return FallbackTestCard.BigCardImagePath();
        }
    }

    public override string BetaPortraitPath
    {
        get
        {
            if (Exists(BetaSmallPortraitPath))
                return BetaSmallPortraitPath;

            if (Exists(SmallPortraitPath))
                return SmallPortraitPath;

            return FallbackTestCard.CardImagePath();
        }
    }
}