using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public enum Team
    {
        Player,
        Enemy
    }

    [Header("Stats")]
    [SerializeField] Team team;
    [SerializeField] string displayName;
    [SerializeField] int currentHP;
    [SerializeField] int maxHP;

    [Header("Componentes")]
    [SerializeField] CharacterEffects characterEffects;
    [SerializeField] CharacterUI characterUI;
    [SerializeField] GameObject selectionVisual;
    [SerializeField] DamageFlash damageFlash;

    [Header("Prefabs")]
    [SerializeField] GameObject healParticlePrefab;

    private Vector3 standingPosition;
}
