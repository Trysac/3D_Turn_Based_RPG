using System;
using System.Linq;
using NUnit.Framework;
using TurnBasedRpg.Core;

namespace TurnBasedRpg.Tests
{
    public class TurnOrderTests
    {
        [Test]
        public void FastestActsFirst_TiesGoToHeroesThenSlot()
        {
            var slowHero = TestUnits.Hero(TestUnits.Def("slowHero", speed: 5), 0);
            var tieHero = TestUnits.Hero(TestUnits.Def("tieHero", speed: 12), 1);
            var fastEnemy = TestUnits.Enemy(TestUnits.Def("fastEnemy", speed: 20), 0);
            var tieEnemyA = TestUnits.Enemy(TestUnits.Def("tieEnemyA", speed: 12), 1);
            var tieEnemyB = TestUnits.Enemy(TestUnits.Def("tieEnemyB", speed: 12), 2);
            var battle = TestUnits.Battle(new[] { slowHero, tieHero }, new[] { tieEnemyB, fastEnemy, tieEnemyA });

            Assert.That(battle.TurnQueue, Is.EqualTo(new[] { fastEnemy, tieHero, tieEnemyA, tieEnemyB, slowHero }));
            Assert.That(battle.Current, Is.SameAs(fastEnemy));
            Assert.That(battle.Round, Is.EqualTo(1));
        }

        [Test]
        public void NewRound_StartsAfterEveryoneActed_WithoutTheFallen()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", attack: 500, speed: 20));
            var weak = TestUnits.Enemy(TestUnits.Def("weak", hp: 10, speed: 5), 0);
            var tough = TestUnits.Enemy(TestUnits.Def("tough", hp: 5000, speed: 1), 1);
            var battle = TestUnits.Battle(new[] { hero }, new[] { weak, tough });

            battle.BeginTurn();
            battle.Execute(BattleAction.UseSkill(Catalog.Attack, weak));
            Assert.That(weak.IsAlive, Is.False);
            Assert.That(battle.TurnQueue, Is.EqualTo(new[] { tough }), "the fallen leave the queue at once");

            battle.BeginTurn();
            battle.Execute(BattleAction.Defend());
            Assert.That(battle.Round, Is.EqualTo(2));
            Assert.That(battle.TurnQueue, Is.EqualTo(new[] { hero, tough }));
            Assert.That(battle.TurnsTaken, Is.EqualTo(2));
        }
    }

    public class BattleActionTests
    {
        [Test]
        public void Attack_TakesTheReportedDamage_AndTheLastBlowWins()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", attack: 30, speed: 20));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", hp: 40, defense: 0));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            ActionReport first = battle.Execute(BattleAction.UseSkill(Catalog.Attack, enemy));
            Hit hit = first.Hits.Single();
            Assert.That(hit.Target, Is.SameAs(enemy));
            Assert.That(enemy.Hp, Is.EqualTo(40 - hit.Damage));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Ongoing));

            for (int guard = 0; guard < 20 && !battle.IsOver; guard++)
            {
                if (battle.BeginTurn().Skipped) continue;
                battle.Execute(battle.Current == hero ? BattleAction.UseSkill(Catalog.Attack, enemy) : BattleAction.Defend());
            }
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(enemy.Hp, Is.Zero);
            Assert.That(battle.Current, Is.Null);
            Assert.Throws<InvalidOperationException>(() => battle.BeginTurn());
        }

        [Test]
        public void LosingEveryHero_IsADefeat()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", hp: 5, defense: 0, speed: 1));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", attack: 100, speed: 20));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            ActionReport report = battle.Execute(BattleAction.UseSkill(Catalog.Attack, hero));
            Assert.That(report.Hits.Single().Defeated, Is.True);
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Defeat));
        }

        [Test]
        public void Execute_RejectsActionsOutOfTurnOrOnBadTargets()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 20));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy"));
            var battle = TestUnits.Battle(hero, enemy);

            Assert.Throws<InvalidOperationException>(() => battle.Execute(BattleAction.Defend()), "before BeginTurn");
            battle.BeginTurn();
            Assert.Throws<InvalidOperationException>(() => battle.BeginTurn());
            Assert.Throws<InvalidOperationException>(() => battle.Execute(BattleAction.UseSkill(Catalog.Attack, hero)), "attacking yourself");
            Assert.Throws<InvalidOperationException>(() => battle.Execute(BattleAction.UseSkill(Catalog.Attack)), "no target");
            Assert.That(enemy.Hp, Is.EqualTo(100), "rejected actions change nothing");
            Assert.That(battle.Current, Is.SameAs(hero));
        }

        [Test]
        public void Skills_CostMp_AndAreRefusedWithoutIt()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 20, extra: Catalog.Fireball), mp: 15);
            var enemies = new[] { TestUnits.Enemy(TestUnits.Def("a", hp: 500), 0), TestUnits.Enemy(TestUnits.Def("b", hp: 500), 1) };
            var battle = TestUnits.Battle(new[] { hero }, enemies);

            battle.BeginTurn();
            ActionReport report = battle.Execute(BattleAction.UseSkill(Catalog.Fireball));
            Assert.That(report.Hits.Select(h => h.Target), Is.EqualTo(enemies), "an area skill hits every foe");
            Assert.That(enemies.All(e => e.Hp < 500), Is.True);
            Assert.That(hero.Mp, Is.EqualTo(5));

            TestUnits.AdvanceTo(battle, hero);
            Assert.That(battle.CanUse(hero, Catalog.Fireball), Is.False);
            Assert.Throws<InvalidOperationException>(() => battle.Execute(BattleAction.UseSkill(Catalog.Fireball)));
        }

        [Test]
        public void Cooldown_BlocksTheSkillForThatManyOfTheUsersTurns()
        {
            // Smite has a cooldown of 1: used on turn 1, unavailable on turn 2, ready on turn 3.
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 20, extra: Catalog.Smite));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", hp: 5000));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            battle.Execute(BattleAction.UseSkill(Catalog.Smite, enemy));
            Assert.That(hero.CooldownOf(Catalog.Smite), Is.EqualTo(1));

            TestUnits.AdvanceTo(battle, hero);
            Assert.That(battle.CanUse(hero, Catalog.Smite), Is.False);
            battle.Execute(BattleAction.Defend());

            TestUnits.AdvanceTo(battle, hero);
            Assert.That(hero.CooldownOf(Catalog.Smite), Is.Zero);
            Assert.That(battle.CanUse(hero, Catalog.Smite), Is.True);
        }

        [Test]
        public void Defend_HalvesTheNextHit_RestoresMp_AndEndsOnTheNextTurn()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", mp: 50, defense: 0, speed: 20), mp: 0);
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", attack: 40, speed: 1));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            ActionReport defend = battle.Execute(BattleAction.Defend());
            Assert.That(defend.Hits.Single().StatusApplied, Is.EqualTo(StatusKind.Defending));
            Assert.That(defend.Hits.Single().MpRestored, Is.EqualTo(5));
            Assert.That(hero.Mp, Is.EqualTo(5));
            Assert.That(hero.Has(StatusKind.Defending), Is.True);
            Assert.That(Formulas.ExpectedDamage(enemy, hero, Catalog.Attack), Is.EqualTo(20));

            battle.BeginTurn();
            ActionReport hit = battle.Execute(BattleAction.UseSkill(Catalog.Attack, hero));
            Assert.That(hit.Hits.Single().Damage, Is.LessThanOrEqualTo(33), "40 +-10%, crit at most 66, halved");

            battle.BeginTurn();
            Assert.That(hero.Has(StatusKind.Defending), Is.False);
        }
    }

    public class StatusTests
    {
        static SkillDef Inflict(StatusKind kind, int turns) =>
            new SkillDef("inflict", "Inflict", "", 0, 0, TargetKind.Foe, SkillEffect.None, 0, kind, turns);

        [Test]
        public void Poison_TicksEightPercentAtTheStartOfEachTurn_ThenWearsOff()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 20, extra: Inflict(StatusKind.Poison, 2)));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", hp: 200));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            battle.Execute(BattleAction.UseSkill(hero.Def.Skills[1], enemy));
            Assert.That(enemy.Has(StatusKind.Poison), Is.True);

            TurnStart first = battle.BeginTurn();
            Assert.That(first.Ticks.Single().Damage, Is.EqualTo(16));
            Assert.That(enemy.Hp, Is.EqualTo(184));
            battle.Execute(BattleAction.Defend());

            TestUnits.AdvanceTo(battle, hero);
            battle.Execute(BattleAction.Defend());
            TurnStart second = battle.BeginTurn();
            Assert.That(second.Ticks.Single().Damage, Is.EqualTo(16));
            battle.Execute(BattleAction.Defend());
            Assert.That(enemy.Has(StatusKind.Poison), Is.False, "two turns of poison are over");
        }

        [Test]
        public void FallingToPoison_SkipsTheTurn_AndCanEndTheBattle()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 20, extra: Inflict(StatusKind.Poison, 3)));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", hp: 100), hp: 5);
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            battle.Execute(BattleAction.UseSkill(hero.Def.Skills[1], enemy));
            TurnStart start = battle.BeginTurn();
            Assert.That(start.Skipped, Is.True);
            Assert.That(start.Ticks.Single().Defeated, Is.True);
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [Test]
        public void Stun_SkipsOneTurn()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 20, extra: Inflict(StatusKind.Stun, 1)));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy"));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            ActionReport report = battle.Execute(BattleAction.UseSkill(hero.Def.Skills[1], enemy));
            Assert.That(report.Hits.Single().StatusApplied, Is.EqualTo(StatusKind.Stun));

            TurnStart stunned = battle.BeginTurn();
            Assert.That(stunned.Actor, Is.SameAs(enemy));
            Assert.That(stunned.Skipped && stunned.Stunned, Is.True);
            Assert.That(enemy.Has(StatusKind.Stun), Is.False);
            Assert.That(battle.Current, Is.SameAs(hero), "the round moved on");

            battle.BeginTurn();
            battle.Execute(BattleAction.Defend());
            Assert.That(battle.BeginTurn().Skipped, Is.False, "the stun lasted one turn");
        }

        [Test]
        public void Regen_HealsTenPercentAtTheStartOfTheTurn()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", hp: 200, speed: 20, extra: Catalog.Sanctuary), hp: 50);
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy"));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            battle.Execute(BattleAction.UseSkill(Catalog.Sanctuary));
            int afterHeal = hero.Hp;
            Assert.That(hero.Has(StatusKind.Regen), Is.True);

            TestUnits.AdvanceTo(battle, hero);
            Assert.That(hero.Hp, Is.EqualTo(afterHeal + 20));
        }

        [Test]
        public void StrengthAndWeakness_ChangeTheNumbers()
        {
            var unit = TestUnits.Hero(TestUnits.Def("unit", attack: 20, defense: 10, magic: 30));
            var other = TestUnits.Enemy(TestUnits.Def("other", extra: new[] { Inflict(StatusKind.Strength, 2), Inflict(StatusKind.Weakness, 2) }));
            var battle = TestUnits.Battle(unit, other);
            // Applied directly through skills aimed by the enemy at the hero.
            TestUnits.AdvanceTo(battle, other);
            battle.Execute(BattleAction.UseSkill(other.Def.Skills[1], unit));
            TestUnits.AdvanceTo(battle, other);
            battle.Execute(BattleAction.UseSkill(other.Def.Skills[2], unit));

            Assert.That(unit.Attack, Is.EqualTo(30));
            Assert.That(unit.Magic, Is.EqualTo(45));
            Assert.That(unit.Defense, Is.EqualTo(5));
        }

        [Test]
        public void StatusChance_CanBeResisted()
        {
            var resisted = new SkillDef("maybe", "Maybe", "", 0, 0, TargetKind.Foe, SkillEffect.Physical, 0.01, StatusKind.Poison, 3, 0.0);
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 20, extra: resisted));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy"));
            var battle = TestUnits.Battle(hero, enemy);

            battle.BeginTurn();
            Hit hit = battle.Execute(BattleAction.UseSkill(resisted, enemy)).Hits.Single();
            Assert.That(hit.StatusResisted, Is.True);
            Assert.That(hit.StatusApplied, Is.Null);
            Assert.That(enemy.Has(StatusKind.Poison), Is.False);
        }

        [Test]
        public void DefeatedFighters_LoseTheirStatuses()
        {
            var hero = TestUnits.Hero(TestUnits.Def("hero", attack: 500, speed: 20, extra: Inflict(StatusKind.Weakness, 5)));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", hp: 50));
            var spare = TestUnits.Enemy(TestUnits.Def("spare", hp: 5000), 1);
            var battle = TestUnits.Battle(new[] { hero }, new[] { enemy, spare });
            battle.BeginTurn();
            battle.Execute(BattleAction.UseSkill(hero.Def.Skills[1], enemy));
            TestUnits.AdvanceTo(battle, hero);
            battle.Execute(BattleAction.UseSkill(Catalog.Attack, enemy));
            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(enemy.Statuses, Is.Empty);
        }
    }

    public class ItemTests
    {
        static Battle Setup(out Combatant hero, out Combatant ally, out Combatant enemyA, out Combatant enemyB, Inventory items, int allyHp = 10)
        {
            hero = TestUnits.Hero(TestUnits.Def("hero", hp: 100, speed: 20), 0, hp: 30);
            ally = TestUnits.Hero(TestUnits.Def("ally", hp: 100, mp: 50, speed: 1), 1, hp: allyHp, mp: 0);
            enemyA = TestUnits.Enemy(TestUnits.Def("a", hp: 100), 0);
            enemyB = TestUnits.Enemy(TestUnits.Def("b", hp: 100), 1);
            return TestUnits.Battle(new[] { hero, ally }, new[] { enemyA, enemyB }, items);
        }

        [Test]
        public void Potion_HealsUpToMax_AndIsUsedUp()
        {
            var items = new Inventory();
            items.Add(ItemId.Potion, 1);
            var battle = Setup(out Combatant hero, out _, out _, out _, items);
            battle.BeginTurn();
            Hit hit = battle.Execute(BattleAction.UseItem(ItemId.Potion, hero)).Hits.Single();
            Assert.That(hit.Healed, Is.EqualTo(60));
            Assert.That(hero.Hp, Is.EqualTo(90));
            Assert.That(items.Count(ItemId.Potion), Is.Zero);
            Assert.That(battle.CanUse(hero, ItemId.Potion), Is.False);
        }

        [Test]
        public void Ether_RestoresMp()
        {
            var items = new Inventory();
            items.Add(ItemId.Ether);
            var battle = Setup(out _, out Combatant ally, out _, out _, items);
            battle.BeginTurn();
            Hit hit = battle.Execute(BattleAction.UseItem(ItemId.Ether, ally)).Hits.Single();
            Assert.That(hit.MpRestored, Is.EqualTo(25));
            Assert.That(ally.Mp, Is.EqualTo(25));
        }

        [Test]
        public void Bomb_HitsEveryFoeForAFixedAmount()
        {
            var items = new Inventory();
            items.Add(ItemId.Bomb);
            var battle = Setup(out _, out _, out Combatant a, out Combatant b, items);
            battle.BeginTurn();
            ActionReport report = battle.Execute(BattleAction.UseItem(ItemId.Bomb));
            Assert.That(report.Hits.Select(h => h.Damage), Is.EqualTo(new[] { 35, 35 }));
            Assert.That(a.Hp, Is.EqualTo(65));
            Assert.That(b.Hp, Is.EqualTo(65));
        }

        [Test]
        public void Feather_RevivesOnlyTheFallen_WithFortyPercent()
        {
            var items = new Inventory();
            items.Add(ItemId.Feather);
            var battle = Setup(out Combatant hero, out Combatant ally, out _, out _, items, allyHp: 0);
            battle.BeginTurn();
            Assert.That(battle.ValidTargets(hero, TargetKind.FallenAlly), Is.EqualTo(new[] { ally }));
            Assert.Throws<InvalidOperationException>(() => battle.Execute(BattleAction.UseItem(ItemId.Feather, hero)));
            Hit hit = battle.Execute(BattleAction.UseItem(ItemId.Feather, ally)).Hits.Single();
            Assert.That(hit.Revived, Is.True);
            Assert.That(ally.Hp, Is.EqualTo(40));
        }

        [Test]
        public void Remedy_CuresHarmfulStatuses()
        {
            var hex = new SkillDef("hex", "Hex", "", 0, 0, TargetKind.Foe, SkillEffect.None, 0, StatusKind.Weakness, 5);
            var items = new Inventory();
            items.Add(ItemId.Remedy);
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 1));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", speed: 20, extra: hex));
            var battle = TestUnits.Battle(hero, enemy, items);
            battle.BeginTurn();
            battle.Execute(BattleAction.UseSkill(hex, hero));
            battle.BeginTurn();
            Hit hit = battle.Execute(BattleAction.UseItem(ItemId.Remedy, hero)).Hits.Single();
            Assert.That(hit.Cured, Is.True);
            Assert.That(hero.Has(StatusKind.Weakness), Is.False);
        }

        [Test]
        public void Enemies_CannotUseItems()
        {
            var items = new Inventory();
            items.Add(ItemId.Potion);
            var hero = TestUnits.Hero(TestUnits.Def("hero", speed: 1));
            var enemy = TestUnits.Enemy(TestUnits.Def("enemy", speed: 20));
            var battle = TestUnits.Battle(hero, enemy, items);
            battle.BeginTurn();
            Assert.That(battle.CanUse(enemy, ItemId.Potion), Is.False);
        }
    }
}
