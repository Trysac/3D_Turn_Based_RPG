using System;
using System.Linq;
using NUnit.Framework;
using TurnBasedRpg.Core;

namespace TurnBasedRpg.Tests
{
    public class CampaignTests
    {
        /// <summary>Lets the AI play both sides to the end.</summary>
        static void AutoPlay(Battle battle)
        {
            for (int guard = 0; guard < 1000 && !battle.IsOver; guard++)
            {
                if (battle.BeginTurn().Skipped)
                {
                    continue;
                }
                battle.Execute(BattleAi.Choose(battle, battle.Current));
            }
            Assert.That(battle.IsOver, Is.True, "the battle should end");
        }

        static void LoseOnPurpose(Battle battle)
        {
            for (int guard = 0; guard < 2000 && !battle.IsOver; guard++)
            {
                if (battle.BeginTurn().Skipped)
                {
                    continue;
                }
                battle.Execute(battle.Current.Team == Team.Heroes ? BattleAction.Defend() : BattleAi.Choose(battle, battle.Current));
            }
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Defeat));
        }

        [Test]
        public void NewRun_StartsAtTheMeadowWithTheThreeHeroesAndStartingItems()
        {
            Run run = Run.New(9);
            Assert.That(run.Stage, Is.Zero);
            Assert.That(run.CurrentEncounter.Name, Is.EqualTo("Mossy Meadow"));
            Assert.That(run.Party.Select(m => m.Def), Is.EqualTo(Catalog.Party));
            Assert.That(run.Party.All(m => m.Level == 1 && m.Hp == m.Stats.MaxHp), Is.True);
            Assert.That(run.Inventory.Count(ItemId.Potion), Is.EqualTo(3));
            Assert.That(Campaign.Encounters.Count, Is.EqualTo(5));
            Assert.That(Campaign.Encounters.Last().IsBoss, Is.True);
        }

        [Test]
        public void LostBattle_RegroupsTheParty_RefundsItems_AndRetriesWithANewSeed()
        {
            Run run = Run.New(4);
            run.Party[0].Hp = 10;
            run.Party[1].Mp = 0;
            int seedBefore = run.BattleSeed;
            Battle battle = run.StartBattle();
            // Spend a potion during the lost battle: it must come back for the retry.
            for (int guard = 0; guard < 20; guard++)
            {
                if (battle.BeginTurn().Skipped) continue;
                if (battle.Current.Team == Team.Heroes)
                {
                    battle.Execute(BattleAction.UseItem(ItemId.Potion, battle.Current));
                    break;
                }
                battle.Execute(BattleAi.Choose(battle, battle.Current));
            }
            Assert.That(battle.Inventory.Count(ItemId.Potion), Is.EqualTo(2));
            LoseOnPurpose(battle);

            BattleSummary summary = run.Finish(battle);
            Assert.That(summary.Won, Is.False);
            Assert.That(summary.Xp, Is.Zero);
            Assert.That(run.Stage, Is.Zero);
            Assert.That(run.Attempt, Is.EqualTo(1));
            Assert.That(run.TotalTurns, Is.EqualTo(battle.TurnsTaken));
            Assert.That(run.Party.All(m => m.Hp == m.Stats.MaxHp && m.Mp == m.Stats.MaxMp), Is.True, "the party regroups at full strength");
            Assert.That(run.Party.All(m => m.Level == 1 && m.Xp == 0), Is.True);
            Assert.That(run.Inventory.Count(ItemId.Potion), Is.EqualTo(3));
            Assert.That(run.BattleSeed, Is.Not.EqualTo(seedBefore));
            Assert.That(run.StartBattle().Heroes.All(h => h.Hp == h.Stats.MaxHp), Is.True);
        }

        [Test]
        public void Victory_GivesXp_RevivesTheFallen_AndOpensTheCamp()
        {
            Run run = Run.New(2);
            Battle battle = run.StartBattle();
            battle.Heroes[2].Hp = 0; // the cleric falls before the fight
            AutoPlay(battle);
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
            int standingHp = battle.Heroes[0].Hp;

            BattleSummary summary = run.Finish(battle);
            Assert.That(summary.Won, Is.True);
            Assert.That(summary.Xp, Is.EqualTo(8 + 10 + 8));
            Assert.That(run.Party[0].Xp, Is.EqualTo(26));
            Assert.That(run.Party[0].Hp, Is.EqualTo(standingHp));
            Assert.That(run.Party[2].Xp, Is.EqualTo(13), "the fallen earn half");
            Assert.That(run.Party[2].Hp, Is.EqualTo(1), "and get back up with 1 HP");
            Assert.That(run.Stage, Is.EqualTo(1));
            Assert.That(run.AwaitingCamp, Is.True);
            Assert.Throws<InvalidOperationException>(() => run.StartBattle());
        }

        [Test]
        public void Camp_Rest_HealsAndRevives()
        {
            Run run = AfterFirstWin(out _);
            run.Party[1].Hp = 1;
            run.Party[1].Mp = 0;
            run.Camp(CampChoice.Rest);
            Stats mage = run.Party[1].Stats;
            Assert.That(run.Party[1].Hp, Is.EqualTo(1 + mage.MaxHp * 60 / 100));
            Assert.That(run.Party[1].Mp, Is.EqualTo(mage.MaxMp * 60 / 100));
            Assert.That(run.AwaitingCamp, Is.False);
            Assert.Throws<InvalidOperationException>(() => run.Camp(CampChoice.Rest));
        }

        [Test]
        public void Camp_SuppliesAndArsenal_AddItems()
        {
            Run supplies = AfterFirstWin(out _);
            int potions = supplies.Inventory.Count(ItemId.Potion);
            int ethers = supplies.Inventory.Count(ItemId.Ether);
            supplies.Camp(CampChoice.Supplies);
            Assert.That(supplies.Inventory.Count(ItemId.Potion), Is.EqualTo(potions + 2));
            Assert.That(supplies.Inventory.Count(ItemId.Ether), Is.EqualTo(ethers + 1));

            Run arsenal = AfterFirstWin(out _);
            int bombs = arsenal.Inventory.Count(ItemId.Bomb);
            int feathers = arsenal.Inventory.Count(ItemId.Feather);
            arsenal.Camp(CampChoice.Arsenal);
            Assert.That(arsenal.Inventory.Count(ItemId.Bomb), Is.EqualTo(bombs + 2));
            Assert.That(arsenal.Inventory.Count(ItemId.Feather), Is.EqualTo(feathers + 1));
        }

        [Test]
        public void WinningBattle_KeepsItemsSpentInIt()
        {
            Run run = Run.New(6);
            Battle battle = run.StartBattle();
            for (int guard = 0; guard < 20; guard++)
            {
                if (battle.BeginTurn().Skipped) continue;
                if (battle.Current.Team == Team.Heroes)
                {
                    battle.Execute(BattleAction.UseItem(ItemId.Bomb));
                    break;
                }
                battle.Execute(BattleAi.Choose(battle, battle.Current));
            }
            AutoPlay(battle);
            run.Finish(battle);
            Assert.That(run.Inventory.Count(ItemId.Bomb), Is.Zero);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TheAiPartyCanClearTheWholeCampaign_ByResting(int seed)
        {
            Run run = Run.New(seed);
            int losses = 0;
            while (!run.IsComplete && losses < 10)
            {
                Battle battle = run.StartBattle();
                AutoPlay(battle);
                BattleSummary summary = run.Finish(battle);
                if (!summary.Won)
                {
                    losses++;
                    continue;
                }
                if (run.AwaitingCamp)
                {
                    run.Camp(CampChoice.Rest);
                }
            }
            Assert.That(run.IsComplete, Is.True, $"stuck at stage {run.Stage} after {losses} losses");
            Assert.That(run.Party.All(m => m.Level >= 3), Is.True);
            TestContext.WriteLine($"seed {seed}: {run.TotalTurns} turns, {losses} losses, levels {string.Join("/", run.Party.Select(m => m.Level))}");
        }

        [Test]
        public void TheBoss_BeatsAFreshParty()
        {
            Run run = Run.New(5);
            run.Restore(Campaign.Encounters.Count - 1, 0, 0, false);
            Battle battle = run.StartBattle();
            AutoPlay(battle);
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Defeat), "the boss should be out of reach at level 1");
        }

        [Test]
        public void Records_KeepTheFewestTurnsAndTheFurthestStage()
        {
            var records = new Records();
            Run partial = Run.New(1);
            partial.Restore(3, 0, 50, false);
            Assert.That(records.Register(partial), Is.False);
            Assert.That(records.FurthestStage, Is.EqualTo(3));

            Run slow = Run.New(2);
            slow.Restore(5, 0, 300, false);
            Assert.That(records.Register(slow), Is.True);
            Run fast = Run.New(3);
            fast.Restore(5, 0, 200, false);
            Assert.That(records.Register(fast), Is.True);
            Run slower = Run.New(4);
            slower.Restore(5, 0, 400, false);
            Assert.That(records.Register(slower), Is.False);

            Assert.That(records.Clears, Is.EqualTo(3));
            Assert.That(records.BestTurns, Is.EqualTo(200));
            Assert.That(records.FurthestStage, Is.EqualTo(5));
        }

        static Run AfterFirstWin(out BattleSummary summary)
        {
            Run run = Run.New(2);
            Battle battle = run.StartBattle();
            AutoPlay(battle);
            summary = run.Finish(battle);
            Assert.That(summary.Won, Is.True);
            return run;
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void XpToNext_GrowsTwentyFivePerLevel()
        {
            Assert.That(Progression.XpToNext(1), Is.EqualTo(65));
            Assert.That(Progression.XpToNext(2), Is.EqualTo(90));
            Assert.That(Progression.XpToNext(5), Is.EqualTo(165));
        }

        [Test]
        public void GainXp_LevelsUpAsManyTimesAsItPays_AndKeepsTheRest()
        {
            var knight = new PartyMember(Catalog.Knight);
            var ups = Progression.GainXp(knight, 65 + 90 + 5);
            Assert.That(ups.Select(u => u.NewLevel), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(knight.Level, Is.EqualTo(3));
            Assert.That(knight.Xp, Is.EqualTo(5));
        }

        [Test]
        public void LevelUp_RaisesStatsAndAddsTheGrowthToCurrentHpAndMp()
        {
            var knight = new PartyMember(Catalog.Knight, hp: 50, mp: 10);
            Progression.GainXp(knight, 65);
            Assert.That(knight.Stats.MaxHp, Is.EqualTo(120 + 14));
            Assert.That(knight.Stats.Attack, Is.EqualTo(18 + 3));
            Assert.That(knight.Hp, Is.EqualTo(50 + 14));
            Assert.That(knight.Mp, Is.EqualTo(10 + 3));
        }

        [Test]
        public void GainXp_StopsAtTheMaxLevel()
        {
            var mage = new PartyMember(Catalog.Mage, level: Progression.MaxLevel);
            Assert.That(Progression.GainXp(mage, 10000), Is.Empty);
            Assert.That(mage.Level, Is.EqualTo(Progression.MaxLevel));
            Assert.That(mage.Xp, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => Progression.GainXp(mage, -1));
        }
    }

    public class SaveCodecTests
    {
        [Test]
        public void Run_RoundTripsEveryField()
        {
            Run run = Run.New(77);
            run.Restore(2, 1, 41, true);
            run.Party[0].Hp = 33;
            run.Party[1].Mp = 4;
            Progression.GainXp(run.Party[2], 60);
            run.Inventory.Add(ItemId.Bomb, 2);

            Run copy = SaveCodec.ReadRun(SaveCodec.WriteRun(run));
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy.Seed, Is.EqualTo(77));
            Assert.That(copy.Stage, Is.EqualTo(2));
            Assert.That(copy.Attempt, Is.EqualTo(1));
            Assert.That(copy.TotalTurns, Is.EqualTo(41));
            Assert.That(copy.AwaitingCamp, Is.True);
            Assert.That(copy.BattleSeed, Is.EqualTo(run.BattleSeed));
            for (int i = 0; i < run.Party.Count; i++)
            {
                Assert.That(copy.Party[i].Def, Is.SameAs(run.Party[i].Def));
                Assert.That(copy.Party[i].Level, Is.EqualTo(run.Party[i].Level));
                Assert.That(copy.Party[i].Xp, Is.EqualTo(run.Party[i].Xp));
                Assert.That(copy.Party[i].Hp, Is.EqualTo(run.Party[i].Hp));
                Assert.That(copy.Party[i].Mp, Is.EqualTo(run.Party[i].Mp));
            }
            foreach (ItemDef item in Catalog.Items)
            {
                Assert.That(copy.Inventory.Count(item.Id), Is.EqualTo(run.Inventory.Count(item.Id)));
            }
            Assert.That(SaveCodec.WriteRun(copy), Is.EqualTo(SaveCodec.WriteRun(run)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("garbage")]
        [TestCase("version=2\nseed=1")]
        public void BrokenSaves_ReadAsNoRun(string text)
        {
            Assert.That(SaveCodec.ReadRun(text), Is.Null);
        }

        [Test]
        public void SaveWithAnUnknownHero_ReadsAsNoRun()
        {
            string text = SaveCodec.WriteRun(Run.New(1)).Replace("knight", "dragon");
            Assert.That(SaveCodec.ReadRun(text), Is.Null);
        }

        [Test]
        public void OutOfRangeValues_AreClamped()
        {
            string text = SaveCodec.WriteRun(Run.New(1)).Replace("hero0=knight,1,0,120,30", "hero0=knight,1,0,9999,30");
            Run run = SaveCodec.ReadRun(text);
            Assert.That(run.Party[0].Hp, Is.EqualTo(120));
        }

        [TestCase("stage=0", "stage=5")]
        [TestCase("stage=0", "stage=99")]
        [TestCase("hero0=knight", "hero0=slime")]
        [TestCase("hero1=mage", "hero1=knight")]
        public void SavesTheGameCouldNotHaveWritten_ReadAsNoRun(string from, string to)
        {
            string text = SaveCodec.WriteRun(Run.New(1)).Replace(from, to);
            Assert.That(text, Does.Contain(to));
            Assert.That(SaveCodec.ReadRun(text), Is.Null);
        }

        [Test]
        public void Records_RoundTrip_AndBrokenTextGivesEmptyRecords()
        {
            var records = new Records();
            Run done = Run.New(1);
            done.Restore(5, 0, 123, false);
            records.Register(done);

            Records copy = SaveCodec.ReadRecords(SaveCodec.WriteRecords(records));
            Assert.That(copy.Clears, Is.EqualTo(1));
            Assert.That(copy.BestTurns, Is.EqualTo(123));
            Assert.That(copy.FurthestStage, Is.EqualTo(5));

            Records empty = SaveCodec.ReadRecords("nonsense");
            Assert.That(empty.Clears + empty.BestTurns + empty.FurthestStage, Is.Zero);
        }
    }
}
