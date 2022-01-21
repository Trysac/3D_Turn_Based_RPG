using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Player Persistent Data", menuName = "New Player Persistent Data")]
public class PlayerPersistentData : ScriptableObject
{
    [SerializeField] PlayerPersistentCharacter[] characters;

#if UNITY_EDITOR

    public void OnValidate()
    {
        ResetCharacters();
    }

#endif

    public void ResetCharacters()
    {
        foreach (PlayerPersistentCharacter character in characters)
        {
            character.Health = character.CharacterPrefab.GetComponent<Character>().MaxHP;
            character.IsDead = false;
        }
    }
}
