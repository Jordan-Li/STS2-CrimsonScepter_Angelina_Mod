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
    // 大图：优先找具体卡图，找不到就回退到默认图
    public override string CustomPortraitPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
            return ResourceLoader.Exists(path) ? path : "card.png".BigCardImagePath();
        }
    }

    // 小图：优先找具体卡图，找不到就回退到默认图
    public override string PortraitPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
            return ResourceLoader.Exists(path) ? path : "card.png".CardImagePath();
        }
    }

    // beta 图：优先 beta 图；没有就回退普通图；再没有就回退默认图
    public override string BetaPortraitPath
    {
        get
        {
            var betaPath = $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
            if (ResourceLoader.Exists(betaPath))
                return betaPath;

            var normalPath = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
            if (ResourceLoader.Exists(normalPath))
                return normalPath;

            return "card.png".CardImagePath();
        }
    }
}