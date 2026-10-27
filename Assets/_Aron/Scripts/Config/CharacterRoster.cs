// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Config
{
    /// <summary>Lives in Resources/Config so menu + levels share the playable characters list.</summary>
    [CreateAssetMenu(menuName = "Aron/Character Roster", fileName = "CharacterRoster")]
    public class CharacterRoster : ScriptableObject
    {
        public const string ResourcePath = "Config/CharacterRoster";
        public CharacterDefinition[] characters;

        public CharacterDefinition Get(string id)
        {
            if (characters == null || characters.Length == 0) return null;
            foreach (var c in characters) if (c != null && c.characterId == id) return c;
            return characters[0];
        }

        public static CharacterRoster Load() => Resources.Load<CharacterRoster>(ResourcePath);
    }
}
