using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Cards;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Extensions;
using CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Character;

/// <summary>
/// 安洁莉娜角色本体。
/// 这一版先完全退回模板最小可运行状态，
/// 只验证“角色能不能出现在选人界面”。
/// </summary>
public class Angelina : PlaceholderCharacterModel
{
    /// <summary>
    /// 角色自己的短 ID。
    /// 这不是 mod id，而是角色 id。
    /// </summary>
    public const string CharacterId = "Angelina";

    /// <summary>
    /// 角色主色。
    /// 这里先保留安洁莉娜的蓝色。
    /// </summary>
    public static readonly Color Color = new("6CB8F6");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;


    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeAngelina>(),
        ModelDb.Card<StrikeAngelina>(),
        ModelDb.Card<StrikeAngelina>(),
        ModelDb.Card<StrikeAngelina>(),
        ModelDb.Card<DefendAngelina>(),
        ModelDb.Card<DefendAngelina>(),
        ModelDb.Card<DefendAngelina>(),
        ModelDb.Card<DefendAngelina>(),
        ModelDb.Card<LittleGift>(),
        ModelDb.Card<AntiGravity>()
    ];


    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<CrimsonScepter>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<AngelinaCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AngelinaRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AngelinaPotionPool>();

    /// <summary>
    /// PlaceholderCharacterModel 会使用很多原版占位资源。
    /// 这里继续沿用模板的占位角色图。
    /// </summary>
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}