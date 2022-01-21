using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerPersistentCharacter
{
    [SerializeField] GameObject characterPrefab;
    [SerializeField] int health;
    [SerializeField] bool isDead;

    public GameObject CharacterPrefab { get => characterPrefab; set => characterPrefab = value; }
    public int Health { get => health; set => health = value; }
    public bool IsDead { get => isDead; set => isDead = value; }
}
