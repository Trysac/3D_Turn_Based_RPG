namespace TurnBasedRpg.Core
{
    /// <summary>The six numbers that describe a fighter.</summary>
    public readonly struct Stats
    {
        public readonly int MaxHp;
        public readonly int MaxMp;
        public readonly int Attack;
        public readonly int Defense;
        public readonly int Magic;
        public readonly int Speed;

        public Stats(int maxHp, int maxMp, int attack, int defense, int magic, int speed)
        {
            MaxHp = maxHp;
            MaxMp = maxMp;
            Attack = attack;
            Defense = defense;
            Magic = magic;
            Speed = speed;
        }

        /// <summary>These stats after <paramref name="levels"/> level-ups of <paramref name="growth"/>.</summary>
        public Stats Grow(Stats growth, int levels) => new Stats(
            MaxHp + growth.MaxHp * levels,
            MaxMp + growth.MaxMp * levels,
            Attack + growth.Attack * levels,
            Defense + growth.Defense * levels,
            Magic + growth.Magic * levels,
            Speed + growth.Speed * levels);
    }

    public enum StatusKind
    {
        /// <summary>Loses 8% of max HP at the start of each of its turns.</summary>
        Poison,
        /// <summary>Recovers 10% of max HP at the start of each of its turns.</summary>
        Regen,
        /// <summary>Loses its next turn.</summary>
        Stun,
        /// <summary>Attack and magic x1.5.</summary>
        Strength,
        /// <summary>Defense halved.</summary>
        Weakness,
        /// <summary>Takes half damage until the start of its next turn.</summary>
        Defending,
    }

    /// <summary>
    /// A status on a fighter. <see cref="Turns"/> counts down at the end of each of the owner's
    /// turns and the status ends at zero; Defending instead ends when the owner's next turn starts.
    /// </summary>
    public sealed class StatusEffect
    {
        public StatusEffect(StatusKind kind, int turns)
        {
            Kind = kind;
            Turns = turns;
        }

        public StatusKind Kind { get; }
        public int Turns { get; internal set; }

        public static bool IsHarmful(StatusKind kind) => kind == StatusKind.Poison || kind == StatusKind.Stun || kind == StatusKind.Weakness;
    }
}
