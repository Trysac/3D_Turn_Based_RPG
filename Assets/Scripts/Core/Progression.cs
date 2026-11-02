using System;
using System.Collections.Generic;

namespace TurnBasedRpg.Core
{
    /// <summary>A hero as it is carried between battles.</summary>
    public sealed class PartyMember
    {
        public PartyMember(UnitDef def, int level = 1, int xp = 0, int? hp = null, int? mp = null)
        {
            Def = def;
            Level = Math.Max(1, Math.Min(Progression.MaxLevel, level));
            Xp = Math.Max(0, xp);
            Hp = Math.Max(0, Math.Min(Stats.MaxHp, hp ?? Stats.MaxHp));
            Mp = Math.Max(0, Math.Min(Stats.MaxMp, mp ?? Stats.MaxMp));
        }

        public UnitDef Def { get; }
        public int Level { get; internal set; }
        /// <summary>Experience towards the next level.</summary>
        public int Xp { get; internal set; }
        public int Hp { get; internal set; }
        public int Mp { get; internal set; }
        public Stats Stats => Def.StatsAt(Level);
        public bool IsAlive => Hp > 0;

        public PartyMember Clone() => new PartyMember(Def, Level, Xp, Hp, Mp);
    }

    public sealed class LevelUp
    {
        public LevelUp(PartyMember member, int newLevel)
        {
            Member = member;
            NewLevel = newLevel;
        }

        public PartyMember Member { get; }
        public int NewLevel { get; }
    }

    public static class Progression
    {
        public const int MaxLevel = 20;

        /// <summary>Experience needed to go from <paramref name="level"/> to the next one.</summary>
        public static int XpToNext(int level) => 40 + 25 * level;

        /// <summary>
        /// Adds experience, levelling up as many times as it pays for. Each level-up adds the
        /// growth to max HP and MP and the same amount to current HP and MP.
        /// </summary>
        public static List<LevelUp> GainXp(PartyMember member, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }
            var ups = new List<LevelUp>();
            if (member.Level >= MaxLevel)
            {
                return ups;
            }
            member.Xp += amount;
            while (member.Level < MaxLevel && member.Xp >= XpToNext(member.Level))
            {
                member.Xp -= XpToNext(member.Level);
                Stats before = member.Stats;
                member.Level++;
                Stats after = member.Stats;
                if (member.IsAlive)
                {
                    member.Hp += after.MaxHp - before.MaxHp;
                }
                member.Mp += after.MaxMp - before.MaxMp;
                ups.Add(new LevelUp(member, member.Level));
            }
            if (member.Level >= MaxLevel)
            {
                member.Xp = 0;
            }
            return ups;
        }
    }
}
