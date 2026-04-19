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

public class Angelina : PlaceholderCharacterModel
{
    public const string CharacterId = "Angelina";                       // 定义角色ID
    public static readonly Color Color = new("6CB8F6");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;                               //起始血量 

    // 定义起始卡组
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

    // 初始遗物
    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<CrimsonScepter>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<AngelinaCardPool>();             //重定义卡池
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AngelinaRelicPool>();         //重定义遗物池
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AngelinaPotionPool>();     //重定义药水池
    
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }   //角色icon

    public override string CustomIconTexturePath => "character_icon_angelina.png".CharacterUiPath();                    //左上角icon
    public override string CustomCharacterSelectIconPath => "char_select_angelina.png".CharacterUiPath();               //选择页面图片
    public override string CustomCharacterSelectLockedIconPath => "char_select_angelina_locked.png".CharacterUiPath();  //选择页面未解锁图片
    public override string CustomMapMarkerPath => "map_marker_angelina.png".CharacterUiPath();                          //地图上指针
    
    
    public override string CustomVisualPath => "angelina.tscn".CustomVisualPath();                                      
    public override string CustomEnergyCounterPath => "angelina_energy_counter.tscn".CustomEnergyCounterPath();         
    public override string CustomRestSiteAnimPath => "angelina_rest_site.tscn".CustomRestSiteAnimPath();
    public override string CustomCharacterSelectBg => "char_select_bg_angelina.tscn".CustomCharacterSelectBg();
    public override string CustomMerchantAnimPath => "angelina_merchant.tscn".CustomMerchantPath();
    
    public override string CustomArmPointingTexturePath => "multiplayer_hand_angelina_point.png".HandUiPath();
    public override string CustomArmRockTexturePath => "multiplayer_hand_angelina_rock.png".HandUiPath();
    public override string CustomArmPaperTexturePath => "multiplayer_hand_angelina_paper.png".HandUiPath();
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_angelina_scissors.png".HandUiPath();

    
}