using System;
using System.Linq;
using NUnit.Framework;
using TurnBasedRpg.Core;

namespace TurnBasedRpg.Tests
{
    public class RngTests
    {
        [Test]
        public void SameSeed_GivesTheSameSequence()
        {
            var a = new Rng(42);
            var b = new Rng(42);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()));
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentSequences()
        {
            var a = new Rng(1);
            var b = new Rng(2);
            Assert.That(Enumerable.Range(0, 5).Select(_ => a.NextUInt()), Is.Not.EqualTo(Enumerable.Range(0, 5).Select(_ => b.NextUInt())));
        }

        [Test]
        public void Range_StaysInBounds_AndReachesBothEnds()
        {
            var rng = new Rng(7);
            var seen = Enumerable.Range(0, 2000).Select(_ => rng.Range(90, 111)).ToList();
            Assert.That(seen.Min(), Is.EqualTo(90));
            Assert.That(seen.Max(), Is.EqualTo(110));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Range(5, 5));
        }

        [Test]
        public void Chance_ZeroNeverHits_OneAlwaysHits()
        {
            var rng = new Rng(3);
            Assert.That(Enumerable.Range(0, 200).Any(_ => rng.Chance(0)), Is.False);
            Assert.That(Enumerable.Range(0, 200).All(_ => rng.Chance(1)), Is.True);
        }
    }

    public class FormulaTests
    {
        static readonly SkillDef Blast = new SkillDef("blast", "Blast", "", 0, 0, TargetKind.Foe, SkillEffect.Magical, 2.0);

        [Test]
        public void Physical_IsAttackTimesPowerMinusHalfDefense()
        {
            var attacker = TestUnits.Hero(TestUnits.Def("a", attack: 30));
            var target = TestUnits.Enemy(TestUnits.Def("t", defense: 10));
            Assert.That(Formulas.BaseDamage(attacker, target, Catalog.Attack), Is.EqualTo(25.0));
            Assert.That(Formulas.ExpectedDamage(attacker, target, Catalog.Attack), Is.EqualTo(25));
        }

        [Test]
        public void Magical_IsMagicTimesPowerMinusQuarterDefense()
        {
            var attacker = TestUnits.Hero(TestUnits.Def("a", magic: 15));
            var target = TestUnits.Enemy(TestUnits.Def("t", defense: 8));
            Assert.That(Formulas.ExpectedDamage(attacker, target, Blast), Is.EqualTo(28));
        }

        [Test]
        public void Damage_IsAtLeastOne()
        {
            var attacker = TestUnits.Hero(TestUnits.Def("a", attack: 1));
            var target = TestUnits.Enemy(TestUnits.Def("t", defense: 200));
            Assert.That(Formulas.ExpectedDamage(attacker, target, Catalog.Attack), Is.EqualTo(1));
        }

        [Test]
        public void Rolls_StayWithinTenPercent_AndMagicNeverCrits()
        {
            var attacker = TestUnits.Hero(TestUnits.Def("a", attack: 40, magic: 40));
            var target = TestUnits.Enemy(TestUnits.Def("t", defense: 0));
            var rng = new Rng(11);
            int crits = 0;
            for (int i = 0; i < 500; i++)
            {
                int physical = Formulas.RollDamage(attacker, target, Catalog.Attack, rng, out bool crit);
                if (crit)
                {
                    crits++;
                    Assert.That(physical, Is.InRange(54, 66));
                }
                else
                {
                    Assert.That(physical, Is.InRange(36, 44));
                }
                Formulas.RollDamage(attacker, target, Blast, rng, out bool magicCrit);
                Assert.That(magicCrit, Is.False);
            }
            Assert.That(crits, Is.InRange(25, 80), "about 10% of 500 physical hits should crit");
        }

        [Test]
        public void SameSeed_RollsTheSameDamage()
        {
            var attacker = TestUnits.Hero(TestUnits.Def("a"));
            var target = TestUnits.Enemy(TestUnits.Def("t"));
            var a = new Rng(5);
            var b = new Rng(5);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(Formulas.RollDamage(attacker, target, Catalog.Attack, a, out bool ca),
                    Is.EqualTo(Formulas.RollDamage(attacker, target, Catalog.Attack, b, out bool cb)));
                Assert.That(ca, Is.EqualTo(cb));
            }
        }
    }
}
