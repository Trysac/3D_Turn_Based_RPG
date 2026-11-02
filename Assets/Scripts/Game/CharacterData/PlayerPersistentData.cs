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

        /// <summary>Restores every party member to full health; empty slots are left alone.</summary>
        public void ResetCharacters()
        {
            if (characters == null)
            {
                return;
            }
            foreach (PlayerPersistentCharacter character in characters)
            {
                Character fighter = character?.CharacterPrefab != null ? character.CharacterPrefab.GetComponent<Character>() : null;
                if (fighter == null)
                {
                    continue;
                }
                character.Health = fighter.MaxHP;
                character.IsDead = false;
            }
        }
    }
}
