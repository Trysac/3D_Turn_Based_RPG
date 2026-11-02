using System.Collections.Generic;
using System.Linq;

namespace TurnBasedRpg.Core
{
    /// <summary>
    /// A deterministic, rule-based opponent (it never rolls dice, so the same state always gives
    /// the same choice). In priority order:
    /// 1. A boss below half HP uses its self-buff once.
    /// 2. Heal: a party heal when two allies are under 60% HP, else a single heal on the most
    ///    hurt ally under 50%.
    /// 3. A party buff when nobody on the team has it and two or more allies stand.
    /// 4. The damaging skill and target with the best score: expected damage (capped by the
    ///    target's HP) + a bonus for a kill + a bonus for a new harmful status.
    /// Ties keep the earlier skill and the earlier target. It works for either team.
    /// </summary>
    public static class BattleAi
    {
        public const double SelfBuffBelow = 0.5;
        public const double GroupHealBelow = 0.6;
        public const double SingleHealBelow = 0.5;

        public static BattleAction Choose(Battle battle, Combatant actor)
        {
            List<SkillDef> usable = actor.Def.Skills.Where(s => battle.CanUse(actor, s)).ToList();
            List<Combatant> allies = battle.ValidTargets(actor, TargetKind.Ally).ToList();

            SkillDef rage = usable.FirstOrDefault(s => s.Target == TargetKind.Self && s.Status == StatusKind.Strength);
            if (rage != null && actor.HpRatio < SelfBuffBelow && !actor.Has(StatusKind.Strength))
            {
                return BattleAction.UseSkill(rage);
            }

            SkillDef groupHeal = usable.FirstOrDefault(s => s.Effect == SkillEffect.Heal && s.Target == TargetKind.AllAllies);
            if (groupHeal != null && allies.Count(a => a.HpRatio < GroupHealBelow) >= 2)
            {
                return BattleAction.UseSkill(groupHeal);
            }
            SkillDef heal = usable.FirstOrDefault(s => s.Effect == SkillEffect.Heal && s.Target == TargetKind.Ally);
            Combatant hurt = allies.Where(a => a.HpRatio < SingleHealBelow).OrderBy(a => a.HpRatio).ThenBy(a => a.Slot).FirstOrDefault();
            if (heal != null && hurt != null)
            {
                return BattleAction.UseSkill(heal, hurt);
            }

            SkillDef buff = usable.FirstOrDefault(s => s.Target == TargetKind.AllAllies && s.Effect == SkillEffect.None && s.Status.HasValue);
            if (buff != null && allies.Count >= 2 && allies.All(a => !a.Has(buff.Status.Value)))
            {
                return BattleAction.UseSkill(buff);
            }

            BattleAction best = null;
            double bestScore = double.MinValue;
            foreach (SkillDef skill in usable.Where(s => s.IsDamage))
            {
                IReadOnlyList<Combatant> foes = battle.ValidTargets(actor, skill.Target);
                if (skill.HitsAll)
                {
                    double score = foes.Sum(f => Score(actor, f, skill));
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = BattleAction.UseSkill(skill);
                    }
                    continue;
                }
                foreach (Combatant foe in foes.OrderBy(f => f.Slot))
                {
                    double score = Score(actor, foe, skill);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = BattleAction.UseSkill(skill, foe);
                    }
                }
            }
            return best ?? BattleAction.Defend();
        }

        /// <summary>How good hitting <paramref name="target"/> with <paramref name="skill"/> looks.</summary>
        public static double Score(Combatant actor, Combatant target, SkillDef skill)
        {
            int expected = Formulas.ExpectedDamage(actor, target, skill);
            double score = System.Math.Min(expected, target.Hp);
            if (expected >= target.Hp)
            {
                score += 25 + target.Stats.MaxHp * 0.1;
            }
            else if (skill.Status.HasValue && StatusEffect.IsHarmful(skill.Status.Value) && !target.Has(skill.Status.Value))
            {
                double value = skill.Status.Value == StatusKind.Stun ? 18 : skill.Status.Value == StatusKind.Poison ? 12 : 8;
                score += value * skill.StatusChance;
            }
            return score;
        }
    }
}
