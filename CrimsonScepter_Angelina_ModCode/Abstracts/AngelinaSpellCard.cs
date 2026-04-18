using MegaCrit.Sts2.Core.Entities.Cards;

namespace CrimsonScepter_Angelina_Mod.CrimsonScepter_Angelina_ModCode.Abstracts;

public abstract class AngelinaSpellCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : AngelinaCard(cost, type, rarity, target)
{
    public sealed override bool IsSpell => true;
}