using System.Collections;
using NUnit.Framework;
using TurnBasedRpg.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TurnBasedRpg.Tests
{
    /// <summary>Menu input through a simulated keyboard.</summary>
    public class InputTests : InputTestFixture
    {
        Keyboard keyboard;
        GameDirector director;

        IEnumerator LoadTitle()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("Battle");
            yield return null;
            yield return null;
            director = Object.FindAnyObjectByType<GameDirector>();
            director.UseMemoryStore();
            yield return null;
            // Continue is disabled without a save, so the highlight starts on New Campaign.
            Assert.That(director.Hud.OverlayMenu.FocusedLabel, Is.EqualTo("New Campaign"));
        }

        [UnityTest]
        public IEnumerator ATapThatPressesAndReleasesInOneFrame_StillMovesTheMenu()
        {
            yield return LoadTitle();

            Press(keyboard.downArrowKey, queueEventOnly: true);
            Release(keyboard.downArrowKey, queueEventOnly: true);
            yield return null;
            yield return null;

            Assert.That(director.Hud.OverlayMenu.FocusedLabel, Is.EqualTo("Settings"));
        }

        [UnityTest]
        public IEnumerator EnterOnNewCampaign_OpensTheMap()
        {
            yield return LoadTitle();

            Press(keyboard.enterKey, queueEventOnly: true);
            Release(keyboard.enterKey, queueEventOnly: true);
            yield return null;
            yield return null;

            Assert.That(director.Phase, Is.EqualTo(GamePhase.Map));
            Assert.That(director.Run.Stage, Is.Zero);
        }
    }
}
