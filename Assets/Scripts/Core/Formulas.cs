using System;

namespace TurnBasedRpg.Core
{
    /// <summary>
    /// Damage and healing maths.
    /// Physical: attack x power - defense / 2. Magical: magic x power - defense / 4.
    /// Both are at least 1, vary by -10%..+10%, and are halved against a defending target.
    /// Physical hits crit 10% of the time for x1.5. Heals: magic x power, varied the same way.
    /// </summary>
    public static class Formulas
    {
        public const double CritChance = 0.10;
        public const double CritMultiplier = 1.5;
        public const int PoisonPercent = 8;
        public const int RegenPercent = 10;
        public const int DefendMpPercent = 10;

        /// <summary>Damage before variance, crits and defending.</summary>
        public static double BaseDamage(Combatant attacker, Combatant target, SkillDef skill)
        {
            switch (skill.Effect)
            {
                case SkillEffect.Physical:
                    return Math.Max(1.0, attacker.Attack * skill.Power - target.Defense / 2.0);
                case SkillEffect.Magical:
                    return Math.Max(1.0, attacker.Magic * skill.Power - target.Defense / 4.0);
                default:
                    return 0.0;
            }
        }

        /// <summary>The damage the AI expects: no variance or crits, defending applied.</summary>
        public static int ExpectedDamage(Combatant attacker, Combatant target, SkillDef skill)
        {
            double damage = BaseDamage(attacker, target, skill);
            if (damage <= 0)
            {
                return 0;
            }
            if (target.Has(StatusKind.Defending))
            {
                damage *= 0.5;
            }
            return Math.Max(1, (int)Math.Round(damage));
        }

        /// <summary>Rolls the damage of one hit; draws variance first, then the crit roll for physical skills.</summary>
        public static int RollDamage(Combatant attacker, Combatant target, SkillDef skill, Rng rng, out bool critical)
        {
            critical = false;
            double damage = BaseDamage(attacker, target, skill);
            if (damage <= 0)
            {
                return 0;
            }
            damage *= Variance(rng);
            if (skill.Effect == SkillEffect.Physical && rng.Chance(CritChance))
            {
                critical = true;
                damage *= CritMultiplier;
            }
            if (target.Has(StatusKind.Defending))
            {
                damage *= 0.5;
            }
            return Math.Max(1, (int)Math.Round(damage));
        }

        public static int RollHeal(Combatant healer, SkillDef skill, Rng rng) =>
            Math.Max(1, (int)Math.Round(healer.Magic * skill.Power * Variance(rng)));

        public static int ExpectedHeal(Combatant healer, SkillDef skill) => Math.Max(1, (int)Math.Round(healer.Magic * skill.Power));

        public static int PercentOf(int max, int percent) => Math.Max(1, max * percent / 100);

        static double Variance(Rng rng) => rng.Range(90, 111) / 100.0;
    }
}
