using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>A fighter in the battle scene.</summary>
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

        [Header("Components")]
        [SerializeField] CharacterEffects characterEffects;
        [SerializeField] CharacterUI characterUI;
        [SerializeField] GameObject selectionVisual;
        [SerializeField] DamageFlash damageFlash;

        [Header("Prefabs")]
        [SerializeField] GameObject healParticlePrefab;

        private Vector3 standingPosition;

        public int MaxHP { get => maxHP; set => maxHP = value; }
    }
}
