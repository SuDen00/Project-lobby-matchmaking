using UnityEngine;

[CreateAssetMenu(menuName = "Character", fileName = "New Character")]
public class CharacterNameData : ScriptableObject
{
    public string abilityName;
    public CharacterName type;
    public string description;
    public int value;
}
