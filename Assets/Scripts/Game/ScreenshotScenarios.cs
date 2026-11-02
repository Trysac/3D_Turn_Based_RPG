using System;
using System.Collections;
using System.IO;
using System.Linq;
using TurnBasedRpg.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// Opt-in capture of README screenshots: <c>-screenshot &lt;folder&gt; [-scenario title,map,...]</c>.
    /// Without the -screenshot argument nothing is created, so normal runs and WebGL builds are
    /// unaffected. In the editor (ProjectBuilder.CaptureScreenshots) the camera and the UI are
    /// rendered off screen at 1280x720 and blended, so no window is needed.
    /// </summary>
    public sealed class ScreenshotScenarios : MonoBehaviour
    {
        const string DefaultScenarios = "title,map,command,target,battle,boss,camp,victory";
        const int Width = 1280;
        const int Height = 720;

        string folder;
        string[] scenarios;
        RenderTexture sceneTarget;
        RenderTexture uiTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            string target = Argument("-screenshot");
            if (string.IsNullOrEmpty(target))
            {
                return;
            }
            var host = new GameObject(nameof(ScreenshotScenarios)).AddComponent<ScreenshotScenarios>();
            host.folder = target;
            host.scenarios = (Argument("-scenario") ?? DefaultScenarios).Split(',');
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            var director = FindAnyObjectByType<GameDirector>();
            director.UseMemoryStore();
            if (Application.isEditor)
            {
                RenderOffscreen(director);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (string raw in scenarios)
            {
                string scenario = raw.Trim();
                yield return Stage(director, scenario);
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(folder, scenario + ".png"), shot.EncodeToPNG());
                Destroy(shot);
                Debug.Log($"Saved screenshot {scenario}.png");
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        void RenderOffscreen(GameDirector director)
        {
            sceneTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            uiTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Camera.main.targetTexture = sceneTarget;
            var document = director.Hud.GetComponent<UIDocument>();
            PanelSettings panel = Instantiate(document.panelSettings);
            panel.targetTexture = uiTarget;
            panel.clearColor = true;
            panel.colorClearValue = Color.clear;
            document.panelSettings = panel;
        }

        Texture2D CaptureOffscreen()
        {
            Color[] scene = Read(sceneTarget);
            Color[] ui = Read(uiTarget);
            for (int i = 0; i < scene.Length; i++)
            {
                Color over = ui[i];
                Color result = Color.Lerp(scene[i], new Color(over.r, over.g, over.b), over.a);
                result.a = 1f;
                scene[i] = result;
            }
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.SetPixels(scene);
            shot.Apply();
            return shot;
        }

        static Color[] Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color[] pixels = texture.GetPixels();
            Destroy(texture);
            return pixels;
        }

        static IEnumerator Stage(GameDirector director, string scenario)
        {
            switch (scenario)
            {
                case "title":
                    director.ToTitle();
                    yield return new WaitForSecondsRealtime(1.2f);
                    break;
                case "map":
                    director.LoadRun(PreparedRun(2, 3));
                    yield return new WaitForSecondsRealtime(1.2f);
                    break;
                case "command":
                    yield return BattleAt(director, 1, 2);
                    yield return WaitForPlayer(director, Catalog.Mage);
                    director.OpenSkills();
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                case "target":
                    yield return BattleAt(director, 2, 3);
                    yield return WaitForPlayer(director, Catalog.Knight);
                    director.Hud.CommandMenu.Choose("Attack");
                    director.Hud.CommandMenu.SetFocus(1, false);
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                case "battle":
                    yield return BattleAt(director, 3, 4);
                    yield return WaitForPlayer(director, Catalog.Mage);
                    director.Hud.CommandMenu.Choose("Skills");
                    director.Hud.CommandMenu.Choose("Fireball");
                    director.Hud.CommandMenu.Choose("All foes");
                    yield return new WaitForSeconds(0.75f);
                    break;
                case "boss":
                    yield return BattleAt(director, 4, 6);
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return WaitForPlayer(director, Catalog.Mage);
                    director.Hud.CommandMenu.Choose("Skills");
                    yield return new WaitForSecondsRealtime(0.4f);
                    break;
                case "camp":
                    director.LoadRun(Restore(PreparedRun(2, 3, 61, 24, 70), 2, awaitingCamp: true));
                    yield return new WaitForSecondsRealtime(1f);
                    break;
                case "victory":
                    yield return BattleAt(director, 1, 2);
                    if (!director.AutoBattle)
                    {
                        director.ToggleAuto();
                    }
                    director.SpeedOverride = 6f;
                    float waited = 0f;
                    while (director.Phase != GamePhase.Result && waited < 120f)
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    director.ToggleAuto();
                    director.SpeedOverride = 0f;
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                default:
                    Debug.LogWarning($"Unknown screenshot scenario '{scenario}'.");
                    break;
            }
        }

        /// <summary>A run at a stage with heroes at a given level, as if the earlier battles were won.</summary>
        static Run PreparedRun(int stage, int level, params int[] hp)
        {
            var party = Catalog.Party.Select((def, i) => new PartyMember(def, level, 0, i < hp.Length ? hp[i] : (int?)null)).ToList();
            var run = new Run(7, party, Inventory.Starting());
            return Restore(run, stage, awaitingCamp: false);
        }

        static Run Restore(Run run, int stage, bool awaitingCamp)
        {
            string text = SaveCodec.WriteRun(run)
                .Replace("stage=0", $"stage={stage}")
                .Replace("camp=0", awaitingCamp ? "camp=1" : "camp=0");
            return SaveCodec.ReadRun(text);
        }

        static IEnumerator BattleAt(GameDirector director, int stage, int level)
        {
            director.LoadRun(PreparedRun(stage, level));
            director.EnterBattle();
            yield return new WaitForSecondsRealtime(0.2f);
        }

        /// <summary>Lets the AI play until it is <paramref name="hero"/>'s turn to choose.</summary>
        static IEnumerator WaitForPlayer(GameDirector director, UnitDef hero)
        {
            float waited = 0f;
            director.SpeedOverride = 4f;
            while (waited < 60f)
            {
                if (director.AwaitingPlayer)
                {
                    if (director.PendingActor.Def == hero)
                    {
                        break;
                    }
                    director.Choose(BattleAction.Defend());
                }
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            director.SpeedOverride = 0f;
            director.Hud.HideBanner();
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
