using System.Linq;
using NUnit.Framework;
using TurnBasedRpg.Core;

namespace TurnBasedRpg.Tests
{
    public class AiTests
    {
        [Test]
        public void TakesTheKill_OverAHealthierTarget()
        {
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", attack: 30, speed: 20));
            var healthy = TestUnits.Hero(TestUnits.Def("healthy", defense: 0), 0);
            var nearlyDown = TestUnits.Hero(TestUnits.Def("nearlyDown", defense: 0), 1, hp: 10);
            var battle = TestUnits.Battle(new[] { healthy, nearlyDown }, new[] { enemy });
            battle.BeginTurn();

            BattleAction action = BattleAi.Choose(battle, enemy);
            Assert.That(action.Skill, Is.SameAs(Catalog.Attack));
            Assert.That(action.Target, Is.SameAs(nearlyDown));
        }

        [Test]
        public void HealsTheMostHurtAlly_BelowHalfHealth()
        {
            var shaman = TestUnits.Enemy(TestUnits.Def("shaman", speed: 20, extra: Catalog.DarkMend), 0);
            var hurt = TestUnits.Enemy(TestUnits.Def("hurt"), 1, hp: 30);
            var scratched = TestUnits.Enemy(TestUnits.Def("scratched"), 2, hp: 45);
            var hero = TestUnits.Hero(TestUnits.Def("hero"));
            var battle = TestUnits.Battle(new[] { hero }, new[] { shaman, hurt, scratched });
            battle.BeginTurn();

            BattleAction action = BattleAi.Choose(battle, shaman);
            Assert.That(action.Skill, Is.SameAs(Catalog.DarkMend));
            Assert.That(action.Target, Is.SameAs(hurt));
        }

        [Test]
        public void UsesAPartyHeal_WhenTwoAlliesAreHurt()
        {
            var cleric = TestUnits.Hero(TestUnits.Def("cleric", speed: 20, extra: new[] { Catalog.Mend, Catalog.Sanctuary }), 0, hp: 55);
            var friend = TestUnits.Hero(TestUnits.Def("friend"), 1, hp: 50);
            var battle = TestUnits.Battle(new[] { cleric, friend }, new[] { TestUnits.Enemy(TestUnits.Def("enemy")) });
            battle.BeginTurn();

            Assert.That(BattleAi.Choose(battle, cleric).Skill, Is.SameAs(Catalog.Sanctuary));
        }

        [Test]
        public void BossEnragesOnce_BelowHalfHealth()
        {
            var boss = new Combatant(Catalog.CinderKing, Team.Enemies, 0, 5);
            var hero = TestUnits.Hero(TestUnits.Def("hero", hp: 5000));
            var battle = TestUnits.Battle(new[] { hero }, new[] { boss });
            TestUnits.AdvanceTo(battle, boss);

            Assert.That(BattleAi.Choose(battle, boss).Skill, Is.Not.SameAs(Catalog.InfernoRage), "healthy bosses do not rage");

            var hurtBoss = new Combatant(Catalog.CinderKing, Team.Enemies, 0, 5, hp: 100);
            var second = TestUnits.Battle(new[] { TestUnits.Hero(TestUnits.Def("hero2", hp: 5000)) }, new[] { hurtBoss });
            TestUnits.AdvanceTo(second, hurtBoss);
            BattleAction rage = BattleAi.Choose(second, hurtBoss);
            Assert.That(rage.Skill, Is.SameAs(Catalog.InfernoRage));
            second.Execute(rage);
            Assert.That(hurtBoss.Has(StatusKind.Strength), Is.True);

            TestUnits.AdvanceTo(second, hurtBoss);
            Assert.That(BattleAi.Choose(second, hurtBoss).Skill, Is.Not.SameAs(Catalog.InfernoRage), "it rages only once");
        }

        [Test]
        public void BuffsThePack_WhenNobodyHasTheBuff()
        {
            var wolf = TestUnits.Enemy(TestUnits.Def("wolf", speed: 20, extra: Catalog.Howl), 0);
            var packmate = TestUnits.Enemy(TestUnits.Def("packmate"), 1);
            var battle = TestUnits.Battle(new[] { TestUnits.Hero(TestUnits.Def("hero")) }, new[] { wolf, packmate });
            battle.BeginTurn();

            BattleAction howl = BattleAi.Choose(battle, wolf);
            Assert.That(howl.Skill, Is.SameAs(Catalog.Howl));
            battle.Execute(howl);
            Assert.That(wolf.Has(StatusKind.Strength) && packmate.Has(StatusKind.Strength), Is.True);
        }

        [Test]
        public void PrefersAnAreaSkill_WhenItAddsUpToMore()
        {
            var mage = TestUnits.Hero(TestUnits.Def("mage", magic: 30, speed: 20, extra: Catalog.Fireball));
            var foes = Enumerable.Range(0, 3).Select(i => TestUnits.Enemy(TestUnits.Def("foe" + i, hp: 500), i)).ToArray();
            var battle = TestUnits.Battle(new[] { mage }, foes);
            battle.BeginTurn();

            BattleAction action = BattleAi.Choose(battle, mage);
            Assert.That(action.Skill, Is.SameAs(Catalog.Fireball));
            Assert.That(action.Target, Is.Null);
        }

        [Test]
        public void SameState_GivesTheSameChoice()
        {
            for (int seed = 1; seed <= 5; seed++)
            {
                Run a = Run.New(seed);
                Run b = Run.New(seed);
                Battle first = a.StartBattle();
                Battle second = b.StartBattle();
                for (int turn = 0; turn < 30 && !first.IsOver; turn++)
                {
                    bool skipA = first.BeginTurn().Skipped;
                    bool skipB = second.BeginTurn().Skipped;
                    Assert.That(skipA, Is.EqualTo(skipB));
                    if (skipA) continue;
                    BattleAction x = BattleAi.Choose(first, first.Current);
                    BattleAction y = BattleAi.Choose(second, second.Current);
                    Assert.That(x.Name, Is.EqualTo(y.Name));
                    Assert.That(x.Target?.Slot, Is.EqualTo(y.Target?.Slot));
                    ActionReport rx = first.Execute(x);
                    ActionReport ry = second.Execute(y);
                    Assert.That(rx.Hits.Select(h => h.Damage), Is.EqualTo(ry.Hits.Select(h => h.Damage)));
                }
            }
        }
    }
}
