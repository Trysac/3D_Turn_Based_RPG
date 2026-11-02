using System;
using System.Collections.Generic;

namespace TurnBasedRpg.Core
{
    /// <summary>A fighter inside one battle: stats at its level, current HP/MP, statuses and cooldowns.</summary>
    public sealed class Combatant
    {
        readonly List<StatusEffect> statuses = new List<StatusEffect>();
        readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>();
        int hp;
        int mp;

        public Combatant(UnitDef def, Team team, int slot, int level, int? hp = null, int? mp = null)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }
            Def = def;
            Team = team;
            Slot = slot;
            Level = level;
            Stats = def.StatsAt(level);
            Hp = hp ?? Stats.MaxHp;
            Mp = mp ?? Stats.MaxMp;
        }

        public UnitDef Def { get; }
        public Team Team { get; }
        /// <summary>Position in its line, from left to right.</summary>
        public int Slot { get; }
        public int Level { get; }
        public Stats Stats { get; }
        public string Name => Def.Name;

        public int Hp
        {
            get => hp;
            internal set => hp = Math.Max(0, Math.Min(Stats.MaxHp, value));
        }

        public int Mp
        {
            get => mp;
            internal set => mp = Math.Max(0, Math.Min(Stats.MaxMp, value));
        }

        public bool IsAlive => hp > 0;
        public double HpRatio => (double)hp / Stats.MaxHp;
        public IReadOnlyList<StatusEffect> Statuses => statuses;

        public int Attack => Has(StatusKind.Strength) ? (int)Math.Round(Stats.Attack * 1.5) : Stats.Attack;
        public int Magic => Has(StatusKind.Strength) ? (int)Math.Round(Stats.Magic * 1.5) : Stats.Magic;
        public int Defense => Has(StatusKind.Weakness) ? Stats.Defense / 2 : Stats.Defense;

        public bool Has(StatusKind kind) => Find(kind) != null;

        public StatusEffect Find(StatusKind kind)
        {
            foreach (StatusEffect status in statuses)
            {
                if (status.Kind == kind)
                {
                    return status;
                }
            }
            return null;
        }

        /// <summary>Turns left before <paramref name="skill"/> can be used again (0 = ready).</summary>
        public int CooldownOf(SkillDef skill) => cooldowns.TryGetValue(skill.Id, out int turns) ? turns : 0;

        /// <summary>Adds a status, or refreshes it to the longer duration if already present.</summary>
        internal void Apply(StatusKind kind, int turns)
        {
            StatusEffect existing = Find(kind);
            if (existing != null)
            {
                existing.Turns = Math.Max(existing.Turns, turns);
                return;
            }
            statuses.Add(new StatusEffect(kind, turns));
        }

        internal bool Remove(StatusKind kind) => statuses.RemoveAll(s => s.Kind == kind) > 0;

        internal void ClearStatuses() => statuses.Clear();

        internal void StartCooldown(SkillDef skill)
        {
            if (skill.Cooldown > 0)
            {
                // +1 because the turn the skill is used on also counts down.
                cooldowns[skill.Id] = skill.Cooldown + 1;
            }
        }

        /// <summary>End of the owner's turn: statuses and cooldowns count down.</summary>
        internal void TickEndOfTurn()
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                StatusEffect status = statuses[i];
                if (status.Kind == StatusKind.Defending)
                {
                    continue;
                }
                status.Turns--;
                if (status.Turns <= 0)
                {
                    statuses.RemoveAt(i);
                }
            }
            var keys = new List<string>(cooldowns.Keys);
            foreach (string key in keys)
            {
                cooldowns[key] = Math.Max(0, cooldowns[key] - 1);
            }
        }

        public override string ToString() => $"{Name} ({Team} {Slot}) {hp}/{Stats.MaxHp}";
    }

    /// <summary>The party's shared bag of items.</summary>
    public sealed class Inventory
    {
        readonly int[] counts = new int[Catalog.Items.Count];

        public int Count(ItemId id) => counts[(int)id];

        public void Add(ItemId id, int amount = 1)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }
            counts[(int)id] = Math.Min(99, counts[(int)id] + amount);
        }

        /// <summary>Takes one item; false when there is none.</summary>
        public bool Take(ItemId id)
        {
            if (counts[(int)id] <= 0)
            {
                return false;
            }
            counts[(int)id]--;
            return true;
        }

        public Inventory Clone()
        {
            var copy = new Inventory();
            Array.Copy(counts, copy.counts, counts.Length);
            return copy;
        }

        /// <summary>The bag a new campaign starts with.</summary>
        public static Inventory Starting()
        {
            var bag = new Inventory();
            bag.Add(ItemId.Potion, 3);
            bag.Add(ItemId.Ether, 1);
            bag.Add(ItemId.Remedy, 1);
            bag.Add(ItemId.Bomb, 1);
            bag.Add(ItemId.Feather, 1);
            return bag;
        }
    }
}
