using System.Collections.Generic;

namespace TurnBasedRpg.Core
{
    public enum Team
    {
        Heroes,
        Enemies,
    }

    public enum TargetKind
    {
        /// <summary>One living foe.</summary>
        Foe,
        /// <summary>Every living foe.</summary>
        AllFoes,
        /// <summary>One living ally (the user included).</summary>
        Ally,
        /// <summary>Every living ally (the user included).</summary>
        AllAllies,
        /// <summary>The user only.</summary>
        Self,
        /// <summary>One fallen ally.</summary>
        FallenAlly,
    }

    public enum SkillEffect
    {
        None,
        Physical,
        Magical,
        Heal,
    }

    /// <summary>An action a fighter can take. The basic attack is a skill with no cost.</summary>
    public sealed class SkillDef
    {
        public SkillDef(string id, string name, string description, int mpCost, int cooldown, TargetKind target,
            SkillEffect effect, double power, StatusKind? status = null, int statusTurns = 0, double statusChance = 1.0)
        {
            Id = id;
            Name = name;
            Description = description;
            MpCost = mpCost;
            Cooldown = cooldown;
            Target = target;
            Effect = effect;
            Power = power;
            Status = status;
            StatusTurns = statusTurns;
            StatusChance = statusChance;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int MpCost { get; }
        /// <summary>After use, the skill is unavailable for this many of the user's turns.</summary>
        public int Cooldown { get; }
        public TargetKind Target { get; }
        public SkillEffect Effect { get; }
        /// <summary>Multiplier on attack (physical) or magic (magical, heal).</summary>
        public double Power { get; }
        public StatusKind? Status { get; }
        public int StatusTurns { get; }
        public double StatusChance { get; }

        public bool IsDamage => Effect == SkillEffect.Physical || Effect == SkillEffect.Magical;
        public bool HitsAll => Target == TargetKind.AllFoes || Target == TargetKind.AllAllies;
        public bool TargetsFoes => Target == TargetKind.Foe || Target == TargetKind.AllFoes;
    }

    public enum ItemId
    {
        Potion,
        Ether,
        Remedy,
        Bomb,
        Feather,
    }

    public sealed class ItemDef
    {
        public ItemDef(ItemId id, string name, string description, TargetKind target, int amount)
        {
            Id = id;
            Name = name;
            Description = description;
            Target = target;
            Amount = amount;
        }

        public ItemId Id { get; }
        public string Name { get; }
        public string Description { get; }
        public TargetKind Target { get; }
        /// <summary>HP healed, MP restored, damage dealt, or percent of max HP on revival.</summary>
        public int Amount { get; }
    }

    /// <summary>An archetype: a hero class or an enemy kind, with its level-1 stats and growth.</summary>
    public sealed class UnitDef
    {
        public UnitDef(string id, string name, string role, Stats baseStats, Stats growth, IReadOnlyList<SkillDef> skills,
            int xpReward = 0, bool isBoss = false)
        {
            Id = id;
            Name = name;
            Role = role;
            BaseStats = baseStats;
            Growth = growth;
            Skills = skills;
            XpReward = xpReward;
            IsBoss = isBoss;
        }

        public string Id { get; }
        public string Name { get; }
        public string Role { get; }
        public Stats BaseStats { get; }
        public Stats Growth { get; }
        /// <summary>Skills in menu order; the first is always the basic attack.</summary>
        public IReadOnlyList<SkillDef> Skills { get; }
        /// <summary>Experience given per level of this enemy.</summary>
        public int XpReward { get; }
        public bool IsBoss { get; }

        public Stats StatsAt(int level) => BaseStats.Grow(Growth, level - 1);
    }
}
