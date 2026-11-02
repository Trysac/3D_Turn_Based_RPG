using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>The party that is carried from one battle to the next.</summary>
    [CreateAssetMenu(fileName = "Player Persistent Data", menuName = "New Player Persistent Data")]
    public class PlayerPersistentData : ScriptableObject
    {
        [SerializeField] PlayerPersistentCharacter[] characters;

        public PlayerPersistentCharacter[] Characters { get => characters; set => characters = value; }

#if UNITY_EDITOR
        public void OnValidate()
        {
            ResetCharacters();
        }
#endif

        /// <summary>Restores every party member to full health.</summary>
        public void ResetCharacters()
        {
            foreach (PlayerPersistentCharacter character in characters)
            {
                character.Health = character.CharacterPrefab.GetComponent<Character>().MaxHP;
                character.IsDead = false;
            }
        }
    }
}
