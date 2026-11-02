using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>A group of character prefabs, such as the enemies of one encounter.</summary>
    [CreateAssetMenu(fileName = "Character Set", menuName = "New Character Set")]
    public class CharacterSet : ScriptableObject
    {
        [SerializeField] GameObject[] characters;

        public GameObject[] Characters { get => characters; set => characters = value; }
    }
}
