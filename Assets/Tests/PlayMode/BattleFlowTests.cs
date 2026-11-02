using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TurnBasedRpg.Core;
using TurnBasedRpg.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TurnBasedRpg.Tests
{
    /// <summary>The game flow in the real scene: start a battle, win, lose and retry, pause.</summary>
    public class BattleFlowTests
    {
        GameDirector director;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("Battle");
            yield return null;
            yield return null;
            director = UnityEngine.Object.FindAnyObjectByType<GameDirector>();
            Assert.That(director, Is.Not.Null);
            director.UseMemoryStore();
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Title));
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
        }

        static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > seconds)
                {
                    Assert.Fail($"Timed out waiting for {what}.");
                }
                yield return null;
            }
        }

        IEnumerator AutoPlayToResult()
        {
            if (!director.AutoBattle)
            {
                director.ToggleAuto();
            }
            director.SpeedOverride = 10f;
            yield return Until(() => director.Phase == GamePhase.Result, 120f, "the result screen");
            director.SpeedOverride = 0f;
        }

        static Run BossRunAtLevelOne()
        {
            string text = SaveCodec.WriteRun(Run.New(5)).Replace("stage=0", $"stage={Campaign.Encounters.Count - 1}");
            return SaveCodec.ReadRun(text);
        }

        [UnityTest]
        public IEnumerator StartingABattle_WaitsForTheFirstHeroCommand()
        {
            director.NewCampaign(1);
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Map));
            Assert.That(director.HasSavedRun, Is.True, "a new campaign is saved at once");

            director.EnterBattle();
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Battle));
            Assert.That(director.Views.Count, Is.EqualTo(6));
            yield return Until(() => director.AwaitingPlayer, 20f, "the player's turn");

            Assert.That(director.PendingActor.Team, Is.EqualTo(Team.Heroes));
            Assert.That(director.Battle.Current, Is.SameAs(director.PendingActor));
            Assert.That(director.Hud.CommandsVisible, Is.True);
            Assert.That(director.Hud.CommandMenu.Count, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator ChoosingDefendFromTheMenu_EndsTheHerosTurn()
        {
            director.NewCampaign(1);
            director.EnterBattle();
            yield return Until(() => director.AwaitingPlayer, 20f, "the player's turn");
            Combatant hero = director.PendingActor;

            Assert.That(director.Hud.CommandMenu.Choose("Defend"), Is.True);
            yield return Until(() => director.PendingActor != hero, 10f, "the turn to pass");
            Assert.That(hero.Has(StatusKind.Defending), Is.True);
            Assert.That(director.Battle.TurnsTaken, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator WinningTheFirstBattle_ShowsVictory_SavesAndOpensTheCamp()
        {
            director.NewCampaign(1);
            director.EnterBattle();
            yield return AutoPlayToResult();

            Assert.That(director.LastWon, Is.True);
            Assert.That(director.Hud.OverlayHeading, Is.EqualTo("Victory!"));
            Assert.That(director.Run.Stage, Is.EqualTo(1));
            Assert.That(director.Run.AwaitingCamp, Is.True);
            Assert.That(director.Records.FurthestStage, Is.EqualTo(1));
            Assert.That(director.Run.Party.All(m => m.Xp > 0 || m.Level > 1), Is.True);

            director.ContinueFromResult();
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Camp));
            int potions = director.Run.Inventory.Count(ItemId.Potion);
            director.ChooseCamp(CampChoice.Supplies);
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Map));
            Assert.That(director.Run.Inventory.Count(ItemId.Potion), Is.EqualTo(potions + 2));

            director.ToTitle();
            director.ContinueCampaign();
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Map));
            Assert.That(director.Run.Stage, Is.EqualTo(1), "the saved run continues at the second battle");
        }

        [UnityTest]
        public IEnumerator LosingToTheBoss_OffersARetryAtFullStrength()
        {
            director.LoadRun(BossRunAtLevelOne());
            director.EnterBattle();
            yield return AutoPlayToResult();

            Assert.That(director.LastWon, Is.False);
            Assert.That(director.Hud.OverlayHeading, Is.EqualTo("Defeat"));
            Assert.That(director.Run.Attempt, Is.EqualTo(1));

            director.Retry();
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Battle));
            Assert.That(director.Battle.TurnsTaken, Is.Zero);
            Assert.That(director.Battle.Heroes.All(h => h.Hp == h.Stats.MaxHp), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTheBattle_AndLeavingNeverShowsAStaleResult()
        {
            director.NewCampaign(2);
            director.EnterBattle();
            yield return Until(() => director.AwaitingPlayer, 20f, "the player's turn");

            director.TogglePause();
            Assert.That(director.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(director.Hud.OverlayHeading, Is.EqualTo("Paused"));

            director.TogglePause();
            Assert.That(director.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.GreaterThan(0f));
            Assert.That(director.Hud.OverlayVisible, Is.False);

            // Let the AI finish the battle, but leave for the title during the victory beat.
            director.ToggleAuto();
            director.SpeedOverride = 10f;
            yield return Until(() => director.Battle.IsOver, 120f, "the battle to end");
            director.TogglePause();
            director.ToTitle();
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(director.Phase, Is.EqualTo(GamePhase.Title), "the old battle's result screen must not appear");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}
