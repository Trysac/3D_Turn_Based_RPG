using System.Collections.Generic;
using System.Linq;
using TurnBasedRpg.Core;

namespace TurnBasedRpg.Tests
{
    /// <summary>Small hand-made fighters so each rule can be checked with round numbers.</summary>
    static class TestUnits
    {
        public static UnitDef Def(string id, int hp = 100, int mp = 50, int attack = 20, int defense = 10, int magic = 10, int speed = 10,
            params SkillDef[] extra)
        {
            var skills = new List<SkillDef> { Catalog.Attack };
            skills.AddRange(extra);
            return new UnitDef(id, id, "Test", new Stats(hp, mp, attack, defense, magic, speed), new Stats(10, 5, 2, 1, 2, 1), skills, xpReward: 10);
        }

        public static Combatant Hero(UnitDef def, int slot = 0, int? hp = null, int? mp = null) => new Combatant(def, Team.Heroes, slot, 1, hp, mp);

        public static Combatant Enemy(UnitDef def, int slot = 0, int? hp = null, int? mp = null) => new Combatant(def, Team.Enemies, slot, 1, hp, mp);

        public static Battle Battle(IEnumerable<Combatant> heroes, IEnumerable<Combatant> enemies, Inventory items = null, int seed = 1) =>
            new Battle(heroes, enemies, items ?? new Inventory(), new Rng(seed));

        public static Battle Battle(Combatant hero, Combatant enemy, Inventory items = null, int seed = 1) =>
            Battle(new[] { hero }, new[] { enemy }, items, seed);

        /// <summary>Plays turns with the AI for both sides until it is <paramref name="unit"/>'s turn (and begins it).</summary>
        public static void AdvanceTo(Battle battle, Combatant unit)
        {
            for (int guard = 0; guard < 100 && !battle.IsOver; guard++)
            {
                if (battle.Current == unit)
                {
                    if (!battle.BeginTurn().Skipped)
                    {
                        return;
                    }
                    continue;
                }
                if (!battle.BeginTurn().Skipped)
                {
                    battle.Execute(BattleAction.Defend());
                }
            }
        }

        public static int Alive(IEnumerable<Combatant> units) => units.Count(u => u.IsAlive);
    }
}
