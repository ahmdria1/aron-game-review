// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Config
{
    [CreateAssetMenu(menuName = "Aron/Character", fileName = "Character")]
    public class CharacterDefinition : ScriptableObject
    {
        public string characterId = "aron";
        public string displayNameAr = "أرون";
        public WeaponDefinition weapon;
        [Tooltip("Optional model prefab (DCC export). If null, a greybox capsule is used.")]
        public GameObject modelPrefab;
        public Color greyboxColor = new Color(0.95f, 0.95f, 0.95f);
        public Color accessoryColor = Color.black;
        public RuntimeAnimatorController animatorController;
    }
}
