using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TurnBasedRpg.Core;
using UnityEngine;

namespace TurnBasedRpg.Game
{
    public enum GamePhase
    {
        Title,
        Map,
        Battle,
        Camp,
        Result,
        Ending,
    }

    /// <summary>
    /// Runs the game: screens, the campaign, and each battle as a coroutine that asks the player
    /// (or the AI) for an action, executes it in the core rules and animates the report.
    /// All rules live in TurnBasedRpg.Core; this class only presents them.
    /// </summary>
    public sealed class GameDirector : MonoBehaviour
    {
        public const string RunKey = "run";
        public const string RecordsKey = "records";

        enum MenuLevel
        {
            Root,
            Skills,
            Items,
            Target,
        }

        static readonly Vector3[] HeroSpots = { new Vector3(-2.4f, 0f, 2.2f), new Vector3(-3.5f, 0f, 1.1f), new Vector3(-4.6f, 0f, 0f) };
        static readonly Vector3[] EnemySpots = { new Vector3(2.4f, 0f, 2.2f), new Vector3(3.5f, 0f, 1.1f), new Vector3(4.6f, 0f, 0f) };
        static readonly Vector3 BossSpot = new Vector3(3.9f, 0f, 1.6f);

        [SerializeField] GameArt art;
        [SerializeField] Hud hud;
        [SerializeField] CameraRig rig;
        [SerializeField] Arena arena;
        [SerializeField] AudioDirector sound;

        readonly GameInput input = new GameInput();
        readonly List<UnitView> views = new List<UnitView>();
        ISaveStore store;
        Settings settings;
        bool settingsOpen;
        bool settingsFromPause;
        Coroutine battleRoutine;
        int battleToken;
        float speedOverride;

        // Player command state.
        Combatant pendingActor;
        BattleAction chosen;
        MenuLevel level;
        MenuLevel targetReturn;
        SkillDef pendingSkill;
        ItemId pendingItem;
        bool pendingIsItem;
        bool targetArea;
        List<Combatant> targetChoices = new List<Combatant>();

        public GamePhase Phase { get; private set; }
        public bool IsPaused { get; private set; }
        public bool SettingsOpen => settingsOpen;
        public Run Run { get; private set; }
        public Battle Battle { get; private set; }
        public Records Records { get; private set; }
        public BattleSummary LastSummary { get; private set; }
        public bool LastWon { get; private set; }
        public bool NewBest { get; private set; }
        public bool AutoBattle { get; private set; }
        /// <summary>When above zero, replaces the battle speed setting (tests run battles fast).</summary>
        public float SpeedOverride
        {
            get => speedOverride;
            set
            {
                speedOverride = value;
                UpdateTimeScale();
            }
        }
        /// <summary>True while the battle waits for the player to choose a hero's action.</summary>
        public bool AwaitingPlayer => pendingActor != null;
        public Combatant PendingActor => pendingActor;
        public bool TargetMenuOpen => pendingActor != null && level == MenuLevel.Target;
        public Hud Hud => hud;
        public IReadOnlyList<UnitView> Views => views;
        public Settings CurrentSettings => settings;
        public bool HasSavedRun => SaveCodec.ReadRun(store.Load(RunKey)) != null;

        void Awake()
        {
            store = new PlayerPrefsStore();
            hud.Build();
            hud.AutoClicked += ToggleAuto;
            hud.PauseClicked += () =>
            {
                if (Phase == GamePhase.Battle)
                {
                    TogglePause();
                }
            };
            hud.CommandMenu.Moved += () => sound.Play(art.Move, 0.6f);
            hud.OverlayMenu.Moved += () => sound.Play(art.Move, 0.6f);
            hud.CommandMenu.Refused += () => sound.Play(art.Cancel, 0.7f);
            hud.OverlayMenu.Refused += () => sound.Play(art.Cancel, 0.7f);
            arena.Init(art, RenderSettings.sun);
            LoadFromStore();
        }

        void Start() => ToTitle();

        /// <summary>Switches to an in-memory store (tests and screenshots must not touch real saves).</summary>
        public void UseMemoryStore()
        {
            store = new MemoryStore();
            LoadFromStore();
            ToTitle();
        }

        void LoadFromStore()
        {
            settings = Settings.Load(store);
            Records = SaveCodec.ReadRecords(store.Load(RecordsKey));
            ApplySettings();
        }

        void ApplySettings()
        {
            sound.MusicVolume = settings.MusicPercent / 100f;
            sound.EffectsVolume = settings.EffectsPercent / 100f;
            rig.ShakeEnabled = settings.Shake;
            UpdateTimeScale();
        }

        void UpdateTimeScale()
        {
            if (IsPaused)
            {
                Time.timeScale = 0f;
            }
            else if (Phase == GamePhase.Battle)
            {
                Time.timeScale = SpeedOverride > 0f ? SpeedOverride : settings.Speed;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        // ---- Screens ----

        public void ToTitle()
        {
            LeaveBattle();
            SetPhase(GamePhase.Title);
            arena.Build(0);
            rig.Drift = 1f;
            rig.Neutral();
            sound.PlayMusic(art.RoadTheme);
            bool canContinue = HasSavedRun;
            string record = Records.Clears > 0
                ? $"Campaigns cleared: {Records.Clears}   Best clear: {Records.BestTurns} turns"
                : Records.FurthestStage > 0 ? $"Furthest: {Campaign.Encounters[Math.Min(Records.FurthestStage, Campaign.Encounters.Count - 1)].Name}" : "Lead three heroes to the Cinder Throne.";
            hud.ShowOverlay("EMBERVALE", "A turn-based RPG", record, "card--title");
            MenuList menu = hud.OverlayMenu;
            Entry(menu, "Continue", canContinue ? $"Stage {SavedStageLabel()}" : "no save", canContinue, ContinueCampaign, "Resume the saved campaign.");
            Entry(menu, "New Campaign", null, true, () => NewCampaign(), "Start from the Mossy Meadow with fresh heroes.");
            Entry(menu, "Settings", null, true, () => OpenSettings(false), "Volume, battle speed and screen shake.");
#if !UNITY_WEBGL
            Entry(menu, "Quit", null, true, Application.Quit, "Close the game. Progress is saved after every battle.");
#endif
            menu.FocusFirstEnabled();
        }

        string SavedStageLabel()
        {
            Run saved = SaveCodec.ReadRun(store.Load(RunKey));
            return saved == null ? string.Empty : $"{Math.Min(saved.Stage + 1, Campaign.Encounters.Count)}/{Campaign.Encounters.Count}";
        }

        public void NewCampaign(int? seed = null)
        {
            LoadRun(Run.New(seed ?? Environment.TickCount));
        }

        public void ContinueCampaign()
        {
            Run saved = SaveCodec.ReadRun(store.Load(RunKey));
            if (saved != null)
            {
                LoadRun(saved);
            }
        }

        /// <summary>Takes over a run (new, loaded, or prepared by a test) and shows where it stands.</summary>
        public void LoadRun(Run run)
        {
            Run = run;
            SaveRun();
            if (run.AwaitingCamp)
            {
                ShowCamp();
            }
            else
            {
                ShowMap();
            }
        }

        void ShowMap()
        {
            LeaveBattle();
            SetPhase(GamePhase.Map);
            arena.Build(Run.Stage);
            rig.Drift = 1f;
            sound.PlayMusic(art.RoadTheme);
            Encounter next = Run.CurrentEncounter;
            hud.ShowOverlay(next.Name, next.Blurb, null, "card--wide");
            hud.AddMapPath(Run);
            AddPartyLines();
            MenuList menu = hud.OverlayMenu;
            string foes = string.Join(", ", next.Enemies.Select(e => $"{e.Def.Name} Lv {e.Level}"));
            Entry(menu, next.IsBoss ? "Face the Cinder King" : "Fight", $"Battle {Run.Stage + 1}/{Campaign.Encounters.Count}", true, EnterBattle, foes);
            Entry(menu, "Main menu", null, true, ToTitle, "Your progress is saved.");
            menu.FocusFirstEnabled();
        }

        void AddPartyLines()
        {
            foreach (PartyMember member in Run.Party)
            {
                Stats stats = member.Stats;
                string xp = member.Level >= Progression.MaxLevel ? "max" : $"{member.Xp}/{Progression.XpToNext(member.Level)}";
                hud.AddExtraLabel($"<b>{member.Def.Name}</b> the {member.Def.Role}   Lv {member.Level}   HP {member.Hp}/{stats.MaxHp}   MP {member.Mp}/{stats.MaxMp}   XP {xp}", "line");
            }
            string items = string.Join("   ", Catalog.Items.Select(i => $"{i.Name} x{Run.Inventory.Count(i.Id)}"));
            hud.AddExtraLabel(items, "line line--muted");
        }

        void ShowCamp()
        {
            LeaveBattle();
            SetPhase(GamePhase.Camp);
            arena.Build(Math.Max(0, Run.Stage - 1));
            rig.Drift = 1f;
            sound.PlayMusic(art.RoadTheme);
            hud.ShowOverlay("Camp", $"Before {Run.CurrentEncounter.Name}: choose one", null, "card--wide");
            AddPartyLines();
            MenuList menu = hud.OverlayMenu;
            Entry(menu, "Rest", "+60% HP and MP", true, () => ChooseCamp(CampChoice.Rest), "Revive the fallen and restore 60% of everyone's max HP and MP.");
            Entry(menu, "Supplies", "+2 Potion, +1 Ether", true, () => ChooseCamp(CampChoice.Supplies), "Stock up on healing for the road.");
            Entry(menu, "Arsenal", "+2 Fire Bomb, +1 Feather", true, () => ChooseCamp(CampChoice.Arsenal), "Bombs hit every foe; a feather revives a fallen hero.");
            menu.FocusFirstEnabled();
        }

        public void ChooseCamp(CampChoice choice)
        {
            if (Phase != GamePhase.Camp)
            {
                return;
            }
            Run.Camp(choice);
            SaveRun();
            sound.Play(choice == CampChoice.Rest ? art.Heal : art.Item);
            ShowMap();
        }

        void ShowResult()
        {
            SetPhase(GamePhase.Result);
            hud.ShowBattleHud(false);
            hud.HideBanner();
            MenuList menu = hud.OverlayMenu;
            if (LastWon)
            {
                hud.ShowOverlay("Victory!", $"{LastSummary.Turns} turns   +{LastSummary.Xp} XP", null);
                foreach (LevelUp up in LastSummary.LevelUps)
                {
                    hud.AddExtraLabel($"{up.Member.Def.Name} reached level {up.NewLevel}!", "line line--gold");
                }
                if (LastSummary.LevelUps.Count == 0)
                {
                    hud.AddExtraLabel("No level-ups this time.", "line line--muted");
                }
                Entry(menu, "Continue", null, true, ContinueFromResult, LastSummary.CampaignComplete ? "See the ending." : "Make camp before the next battle.");
            }
            else
            {
                hud.ShowOverlay("Defeat", "The party regroups at full strength.", null);
                hud.AddExtraLabel($"Attempt {Run.Attempt + 1} awaits. Items used in the lost battle are returned.", "line line--muted");
                Entry(menu, "Retry battle", null, true, Retry, "Fight the same foes again.");
                Entry(menu, "Main menu", null, true, ToTitle, "Your progress is saved.");
            }
            menu.FocusFirstEnabled();
        }

        public void ContinueFromResult()
        {
            if (Phase != GamePhase.Result || !LastWon)
            {
                return;
            }
            if (Run.IsComplete)
            {
                ShowEnding();
            }
            else
            {
                ShowCamp();
            }
        }

        void ShowEnding()
        {
            SetPhase(GamePhase.Ending);
            arena.Build(4);
            rig.Drift = 1f;
            sound.PlayMusic(art.RoadTheme);
            hud.ShowOverlay("The Cinder King falls", "Embervale is safe again.", null);
            hud.AddExtraLabel($"Campaign cleared in {Run.TotalTurns} turns.", "line line--gold");
            hud.AddExtraLabel(NewBest ? "A new best clear!" : $"Best clear: {Records.BestTurns} turns", "line");
            hud.AddExtraLabel($"Campaigns cleared: {Records.Clears}", "line line--muted");
            MenuList menu = hud.OverlayMenu;
            Entry(menu, "New Campaign", null, true, () => NewCampaign(), "Start again from the Mossy Meadow.");
            Entry(menu, "Main menu", null, true, ToTitle, null);
            menu.FocusFirstEnabled();
        }

        // ---- Pause and settings ----

        public void TogglePause()
        {
            if (Phase != GamePhase.Battle)
            {
                return;
            }
            if (IsPaused)
            {
                Resume();
                return;
            }
            IsPaused = true;
            UpdateTimeScale();
            ShowPauseMenu();
        }

        void ShowPauseMenu()
        {
            hud.ShowOverlay("Paused", Run.CurrentEncounter?.Name, null);
            MenuList menu = hud.OverlayMenu;
            Entry(menu, "Resume", null, true, Resume, null);
            Entry(menu, "Settings", null, true, () => OpenSettings(true), "Volume, battle speed and screen shake.");
            Entry(menu, "Restart battle", null, true, () =>
            {
                Resume();
                EnterBattle();
            }, "Start this battle over from the first turn.");
            Entry(menu, "Main menu", null, true, ToTitle, "Leave the battle. Progress is saved from before it.");
            menu.FocusFirstEnabled();
        }

        void Resume()
        {
            IsPaused = false;
            settingsOpen = false;
            hud.HideOverlay();
            UpdateTimeScale();
        }

        void OpenSettings(bool fromPause)
        {
            settingsOpen = true;
            settingsFromPause = fromPause;
            hud.ShowOverlay("Settings", "Left and right change a value", null);
            MenuList menu = hud.OverlayMenu;
            Entry(menu, "Music", $"{settings.MusicPercent}%", true, () => ChangeSetting(0, 1), "Music volume.", d => ChangeSetting(0, d), quiet: true);
            Entry(menu, "Effects", $"{settings.EffectsPercent}%", true, () => ChangeSetting(1, 1), "Sound effect volume.", d => ChangeSetting(1, d), quiet: true);
            Entry(menu, "Battle speed", $"{settings.Speed:0.#}x", true, () => ChangeSetting(2, 1), "How fast battle animations play.", d => ChangeSetting(2, d), quiet: true);
            Entry(menu, "Screen shake", settings.Shake ? "On" : "Off", true, () => ChangeSetting(3, 1), "Camera shake on heavy hits.", d => ChangeSetting(3, d), quiet: true);
            Entry(menu, "Back", null, true, CloseSettings, null);
            menu.FocusFirstEnabled();
        }

        void ChangeSetting(int index, int delta)
        {
            MenuList menu = hud.OverlayMenu;
            switch (index)
            {
                case 0:
                    settings.MusicPercent = Step(settings.MusicPercent, delta);
                    menu.SetDetail(0, $"{settings.MusicPercent}%");
                    break;
                case 1:
                    settings.EffectsPercent = Step(settings.EffectsPercent, delta);
                    menu.SetDetail(1, $"{settings.EffectsPercent}%");
                    break;
                case 2:
                    settings.SpeedIndex = (settings.SpeedIndex + delta + Settings.Speeds.Length) % Settings.Speeds.Length;
                    menu.SetDetail(2, $"{settings.Speed:0.#}x");
                    break;
                case 3:
                    settings.Shake = !settings.Shake;
                    menu.SetDetail(3, settings.Shake ? "On" : "Off");
                    break;
            }
            settings.Save(store);
            ApplySettings();
            sound.Play(art.Move);
        }

        /// <summary>Volume steps of 10%, wrapping from 100% back to 0% when cycling with confirm.</summary>
        static int Step(int value, int delta)
        {
            int next = value + delta * 10;
            if (next > 100) return delta == 1 && value == 100 ? 0 : 100;
            return Mathf.Clamp(next, 0, 100);
        }

        void CloseSettings()
        {
            settingsOpen = false;
            if (settingsFromPause && IsPaused)
            {
                ShowPauseMenu();
            }
            else
            {
                ToTitle();
            }
        }

        // ---- Battle ----

        public void EnterBattle()
        {
            if (Run == null || Run.IsComplete || Run.AwaitingCamp)
            {
                return;
            }
            LeaveBattle();
            Battle = Run.StartBattle();
            SetPhase(GamePhase.Battle);
            hud.HideOverlay();
            arena.Build(Run.Stage);
            rig.Drift = 0f;
            rig.Neutral();
            rig.Snap();
            SpawnViews();
            hud.ShowBattleHud(true);
            hud.HideCommands();
            hud.SetHint(null);
            hud.SetAuto(AutoBattle);
            hud.SetStage($"{Run.CurrentEncounter.Name.ToUpperInvariant()}   {Run.Stage + 1}/{Campaign.Encounters.Count}");
            hud.UpdateTurnOrder(Battle.Round, Battle.TurnQueue);
            hud.UpdateParty(Battle.Heroes, null);
            sound.PlayMusic(Run.CurrentEncounter.IsBoss ? art.BossTheme : art.BattleTheme);
            UpdateTimeScale();
            battleToken++;
            battleRoutine = StartCoroutine(BattleLoop(battleToken));
        }

        public void Retry()
        {
            if (Phase == GamePhase.Result && !LastWon)
            {
                EnterBattle();
            }
        }

        void LeaveBattle()
        {
            if (battleRoutine != null)
            {
                StopCoroutine(battleRoutine);
                battleRoutine = null;
            }
            battleToken++;
            pendingActor = null;
            chosen = null;
            IsPaused = false;
            settingsOpen = false;
            foreach (UnitView unitView in views)
            {
                if (unitView != null)
                {
                    Destroy(unitView.gameObject);
                }
            }
            views.Clear();
            if (hud != null)
            {
                hud.SetPlates(views);
                hud.ShowBattleHud(false);
                hud.HideBanner();
            }
        }

        void SetPhase(GamePhase phase)
        {
            Phase = phase;
            IsPaused = false;
            UpdateTimeScale();
        }

        void SpawnViews()
        {
            var center = new Vector3(0f, 0f, 1.1f);
            foreach (Combatant hero in Battle.Heroes)
            {
                views.Add(Spawn(hero, HeroSpots[hero.Slot % HeroSpots.Length], center));
            }
            foreach (Combatant enemy in Battle.Enemies)
            {
                Vector3 spot = enemy.Def.IsBoss ? BossSpot : EnemySpots[enemy.Slot % EnemySpots.Length];
                if (Battle.Enemies.Any(e => e.Def.IsBoss) && !enemy.Def.IsBoss)
                {
                    spot += new Vector3(-0.6f, 0f, enemy.Slot == 0 ? 0.6f : -0.6f);
                }
                views.Add(Spawn(enemy, spot, center));
            }
            hud.SetPlates(views);
        }

        UnitView Spawn(Combatant unit, Vector3 spot, Vector3 lookAt)
        {
            var host = new GameObject(unit.Name);
            var unitView = host.AddComponent<UnitView>();
            unitView.Init(unit, art, spot, new Vector3(lookAt.x, 0f, spot.z));
            return unitView;
        }

        public UnitView ViewOf(Combatant unit) => views.FirstOrDefault(v => v.Unit == unit);

        IEnumerator BattleLoop(int token)
        {
            Encounter encounter = Run.CurrentEncounter;
            hud.Banner(encounter.Name, encounter.Blurb, 1.6f, encounter.IsBoss ? "banner--danger" : "banner--big");
            if (encounter.IsBoss)
            {
                sound.Play(art.Roar);
                rig.Shake(0.5f);
            }
            yield return new WaitForSeconds(1.4f);

            while (!Battle.IsOver)
            {
                Combatant actor = Battle.Current;
                RefreshHud(actor);
                SetRings(actor, null);
                TurnStart start = Battle.BeginTurn();
                if (start.Ticks.Count > 0)
                {
                    ShowTicks(start);
                    yield return new WaitForSeconds(0.6f);
                }
                if (start.Skipped)
                {
                    if (start.Stunned)
                    {
                        hud.Floater(ViewOf(actor).HeadPoint, "Stunned!", "floater--status");
                        sound.Play(art.Stun);
                    }
                    yield return new WaitForSeconds(0.6f);
                    continue;
                }

                BattleAction action;
                if (actor.Team == Team.Heroes && !AutoBattle)
                {
                    pendingActor = actor;
                    chosen = null;
                    sound.Play(art.YourTurn, 0.7f);
                    rig.Focus(ViewOf(actor).Center, 0.4f);
                    OpenRootMenu();
                    while (chosen == null)
                    {
                        if (AutoBattle)
                        {
                            chosen = BattleAi.Choose(Battle, actor);
                        }
                        yield return null;
                    }
                    action = chosen;
                    chosen = null;
                    pendingActor = null;
                    hud.HideCommands();
                    hud.SetHint(null);
                    hud.HighlightPlate(null);
                }
                else
                {
                    rig.Focus(ViewOf(actor).Center, 0.4f);
                    yield return new WaitForSeconds(0.45f);
                    action = BattleAi.Choose(Battle, actor);
                }

                SetRings(actor, null);
                ActionReport report = Battle.Execute(action);
                yield return Animate(report);
                if (token != battleToken)
                {
                    yield break;
                }
            }

            rig.Neutral();
            SetRings(null, null);
            RefreshHud(null);
            yield return new WaitForSeconds(0.6f);
            LastWon = Battle.Outcome == BattleOutcome.Victory;
            LastSummary = Run.Finish(Battle);
            NewBest = Records.Register(Run);
            SaveRun();
            store.Save(RecordsKey, SaveCodec.WriteRecords(Records));
            if (LastWon)
            {
                hud.Banner("VICTORY", null, 1.6f, "banner--big");
                sound.Play(art.Victory);
                if (LastSummary.LevelUps.Count > 0)
                {
                    sound.Play(art.LevelUp, 0.8f);
                }
            }
            else
            {
                hud.Banner("DEFEAT", null, 1.6f, "banner--danger");
                sound.Play(art.Defeat);
            }
            sound.PlayMusic(null);
            yield return new WaitForSeconds(1.5f);
            battleRoutine = null;
            ShowResult();
        }

        void SaveRun()
        {
            if (Run == null)
            {
                return;
            }
            if (Run.IsComplete)
            {
                store.Delete(RunKey);
            }
            else
            {
                store.Save(RunKey, SaveCodec.WriteRun(Run));
            }
        }

        void RefreshHud(Combatant current)
        {
            hud.UpdateParty(Battle.Heroes, current);
            hud.UpdateTurnOrder(Battle.Round, Battle.TurnQueue);
        }

        void SetRings(Combatant actor, IEnumerable<Combatant> targets)
        {
            var targetSet = targets != null ? new HashSet<Combatant>(targets) : null;
            foreach (UnitView unitView in views)
            {
                if (targetSet != null && targetSet.Contains(unitView.Unit))
                {
                    unitView.SetRing(new Color(1f, 0.35f, 0.25f, 0.85f));
                }
                else if (unitView.Unit == actor)
                {
                    unitView.SetRing(new Color(1f, 0.8f, 0.3f, 0.85f));
                }
                else
                {
                    unitView.SetRing(null);
                }
            }
        }

        void ShowTicks(TurnStart start)
        {
            foreach (Hit tick in start.Ticks)
            {
                UnitView target = ViewOf(tick.Target);
                if (tick.Damage > 0)
                {
                    hud.Floater(target.HeadPoint, $"-{tick.Damage}", "floater--poison");
                    Effects.Poison(art, target.Center);
                    target.Flash(new Color(0.5f, 1f, 0.3f), 0.8f);
                    sound.Play(art.Curse, 0.6f);
                }
                if (tick.Healed > 0)
                {
                    hud.Floater(target.HeadPoint, $"+{tick.Healed}", "floater--heal");
                    Effects.Sparkle(art, target.Center, new Color(0.5f, 1f, 0.6f));
                    sound.Play(art.Heal, 0.5f);
                }
                if (tick.Defeated)
                {
                    sound.Play(art.UnitDown);
                }
            }
            RefreshHud(start.Actor);
        }

        IEnumerator Animate(ActionReport report)
        {
            UnitView actor = ViewOf(report.Actor);
            BattleAction action = report.Action;
            bool heroSide = report.Actor.Team == Team.Heroes;
            if (action.Kind != ActionKind.Defend)
            {
                hud.Banner(action.Name, $"{report.Actor.Name}", 1.0f, heroSide ? null : "banner--enemy");
            }

            switch (action.Kind)
            {
                case ActionKind.Defend:
                    actor.Flash(new Color(0.4f, 0.7f, 1f), 0.8f);
                    hud.Floater(actor.HeadPoint, "Defend", "floater--status");
                    sound.Play(art.Guard);
                    ShowHits(report);
                    yield return new WaitForSeconds(0.5f);
                    break;
                case ActionKind.Item:
                    sound.Play(art.Item);
                    yield return actor.Cast(new Color(1f, 0.85f, 0.4f), 0.4f);
                    ShowHits(report);
                    yield return new WaitForSeconds(0.6f);
                    break;
                default:
                    SkillDef skill = action.Skill;
                    if (skill.Effect == SkillEffect.Physical)
                    {
                        Vector3 aim = report.Hits.Count == 1
                            ? ViewOf(report.Hits[0].Target).Home
                            : Average(report.Hits.Select(h => ViewOf(h.Target).Home));
                        rig.Focus(aim, 0.6f);
                        yield return actor.Lunge(aim, 0.5f, () => ShowHits(report));
                    }
                    else
                    {
                        yield return actor.Cast(CastColor(skill), 0.45f);
                        ShowHits(report);
                    }
                    yield return new WaitForSeconds(0.65f);
                    break;
            }
            rig.Neutral();
            RefreshHud(null);
        }

        static Vector3 Average(IEnumerable<Vector3> points)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Vector3 p in points)
            {
                sum += p;
                count++;
            }
            return count > 0 ? sum / count : Vector3.zero;
        }

        static Color CastColor(SkillDef skill)
        {
            if (skill.Effect == SkillEffect.Heal) return new Color(0.5f, 1f, 0.6f);
            if (skill.Effect == SkillEffect.None) return new Color(1f, 0.85f, 0.3f);
            return skill.Id.Contains("shadow") || skill.Id.Contains("wail") || skill.Id.Contains("hex") ? new Color(0.6f, 0.4f, 1f) : new Color(1f, 0.55f, 0.2f);
        }

        void ShowHits(ActionReport report)
        {
            bool damage = false, critical = false, healed = false, buffed = false, cursed = false, stunned = false, defeated = false, boss = false;
            int biggest = 0;
            SkillDef skill = report.Action.Skill;
            foreach (Hit hit in report.Hits)
            {
                UnitView target = ViewOf(hit.Target);
                Vector3 head = target.HeadPoint;
                if (hit.Damage > 0)
                {
                    damage = true;
                    critical |= hit.Critical;
                    biggest = Mathf.Max(biggest, hit.Damage);
                    hud.Floater(head, hit.Critical ? $"{hit.Damage}!" : hit.Damage.ToString(), hit.Critical ? "floater--crit" : "floater--damage");
                    target.Flash(Color.white, 1f);
                    target.StartCoroutine(target.Recoil(0.25f));
                    if (skill != null && skill.Effect == SkillEffect.Magical)
                    {
                        if (CastColor(skill).b > 0.9f) Effects.Shadow(art, target.Center);
                        else Effects.Flames(art, target.Center);
                    }
                    else if (skill == null)
                    {
                        Effects.Flames(art, target.Center);
                    }
                    else
                    {
                        Effects.Sparks(art, target.Center, hit.Critical);
                    }
                }
                if (hit.Healed > 0)
                {
                    healed = true;
                    hud.Floater(head, $"+{hit.Healed}", "floater--heal");
                    Effects.Sparkle(art, target.Center, new Color(0.5f, 1f, 0.6f));
                    target.Flash(new Color(0.5f, 1f, 0.6f), 0.6f);
                }
                if (hit.MpRestored > 0)
                {
                    hud.Floater(head + Vector3.up * 0.75f, $"+{hit.MpRestored} MP", "floater--mp");
                }
                if (hit.StatusApplied.HasValue && hit.StatusApplied.Value != StatusKind.Defending)
                {
                    StatusKind kind = hit.StatusApplied.Value;
                    hud.Floater(head + Vector3.up * 0.75f, StatusName(kind), "floater--status", 1.4f);
                    if (kind == StatusKind.Stun) stunned = true;
                    else if (StatusEffect.IsHarmful(kind)) cursed = true;
                    else buffed = true;
                    if (!StatusEffect.IsHarmful(kind)) Effects.Sparkle(art, target.Center, new Color(1f, 0.8f, 0.3f));
                }
                if (hit.StatusResisted)
                {
                    hud.Floater(head + Vector3.up * 0.75f, "Resisted", "floater--status");
                }
                if (hit.Cured)
                {
                    hud.Floater(head + Vector3.up * 0.4f, "Cured", "floater--heal");
                }
                if (hit.Revived)
                {
                    hud.Floater(head + Vector3.up * 0.4f, "Revived!", "floater--heal");
                    target.ResetPose();
                }
                if (hit.Defeated)
                {
                    defeated = true;
                    boss |= hit.Target.Def.IsBoss;
                    Effects.Dust(art, target.transform.position + Vector3.up * 0.3f);
                }
            }

            if (damage)
            {
                sound.Play(critical ? art.Critical : skill != null && skill.Effect == SkillEffect.Magical || skill == null ? art.Fire : art.Slash);
                rig.Shake(Mathf.Clamp(biggest / 60f, 0.12f, 0.5f) + (critical ? 0.2f : 0f));
            }
            if (healed) sound.Play(art.Heal);
            if (buffed) sound.Play(art.Buff);
            if (cursed) sound.Play(art.Curse, 0.8f);
            if (stunned) sound.Play(art.Stun);
            if (defeated)
            {
                sound.Play(art.UnitDown);
                rig.Shake(boss ? 0.9f : 0.3f);
            }
            RefreshHud(pendingActor);
        }

        static string StatusName(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return "Poisoned";
                case StatusKind.Regen: return "Regen";
                case StatusKind.Stun: return "Stunned";
                case StatusKind.Strength: return "Strength up";
                case StatusKind.Weakness: return "Defense down";
                default: return kind.ToString();
            }
        }

        // ---- Player commands ----

        void OpenRootMenu()
        {
            level = MenuLevel.Root;
            Combatant actor = pendingActor;
            hud.ShowCommands($"{actor.Name}  Lv {actor.Level}");
            hud.SetHint("Enter choose   Esc back   T auto");
            SetRings(actor, null);
            hud.HighlightPlate(null);
            MenuList menu = hud.CommandMenu;
            menu.Clear();
            Entry(menu, "Attack", null, true, () => ChooseSkill(Catalog.Attack), Catalog.Attack.Description);
            bool anySkill = actor.Def.Skills.Skip(1).Any(s => Battle.CanUse(actor, s));
            Entry(menu, "Skills", null, actor.Def.Skills.Count > 1, OpenSkills, anySkill ? "Spend MP on a special move." : "No skill is ready right now.");
            bool anyItem = Catalog.Items.Any(i => Battle.CanUse(actor, i.Id));
            Entry(menu, "Items", null, true, OpenItems, anyItem ? "Use something from the shared bag." : "Nothing in the bag can be used now.");
            Entry(menu, "Defend", null, true, () => Choose(BattleAction.Defend()), "Halve damage until your next turn and recover 10% MP.");
            menu.FocusFirstEnabled();
        }

        public void OpenSkills()
        {
            if (pendingActor == null)
            {
                return;
            }
            level = MenuLevel.Skills;
            Combatant actor = pendingActor;
            MenuList menu = hud.CommandMenu;
            menu.Clear();
            foreach (SkillDef skill in actor.Def.Skills.Skip(1))
            {
                int cooldown = actor.CooldownOf(skill);
                string detail = cooldown > 0 ? $"wait {cooldown}" : skill.MpCost > 0 ? $"{skill.MpCost} MP" : "free";
                Entry(menu, skill.Name, detail, Battle.CanUse(actor, skill), () => ChooseSkill(skill), skill.Description);
            }
            Entry(menu, "Back", null, true, Back, null, silent: true);
            menu.FocusFirstEnabled();
        }

        void OpenItems()
        {
            level = MenuLevel.Items;
            Combatant actor = pendingActor;
            MenuList menu = hud.CommandMenu;
            menu.Clear();
            foreach (ItemDef item in Catalog.Items)
            {
                Entry(menu, item.Name, $"x{Battle.Inventory.Count(item.Id)}", Battle.CanUse(actor, item.Id), () => ChooseItem(item.Id), item.Description);
            }
            Entry(menu, "Back", null, true, Back, null, silent: true);
            menu.FocusFirstEnabled();
        }

        void ChooseSkill(SkillDef skill)
        {
            pendingIsItem = false;
            pendingSkill = skill;
            OpenTargets(skill.Target, level);
        }

        void ChooseItem(ItemId item)
        {
            pendingIsItem = true;
            pendingItem = item;
            OpenTargets(Catalog.Item(item).Target, level);
        }

        void OpenTargets(TargetKind kind, MenuLevel from)
        {
            targetReturn = from;
            level = MenuLevel.Target;
            Combatant actor = pendingActor;
            targetChoices = Battle.ValidTargets(actor, kind).ToList();
            MenuList menu = hud.CommandMenu;
            menu.Clear();
            string actionName = pendingIsItem ? Catalog.Item(pendingItem).Name : pendingSkill.Name;
            bool area = kind == TargetKind.AllFoes || kind == TargetKind.AllAllies || kind == TargetKind.Self;
            targetArea = area;
            hud.CommandMenu.FocusChanged -= OnTargetFocus;
            hud.CommandMenu.FocusChanged += OnTargetFocus;
            if (area)
            {
                string label = kind == TargetKind.AllFoes ? "All foes" : kind == TargetKind.AllAllies ? "Whole party" : actor.Name;
                Entry(menu, label, actionName, true, () => Choose(MakeAction(null)), $"Use {actionName}.");
                SetRings(actor, targetChoices);
            }
            else
            {
                foreach (Combatant target in targetChoices.OrderBy(t => t.Team == actor.Team ? 0 : 1).ThenBy(t => t.Slot))
                {
                    Combatant aimed = target;
                    string detail = target.IsAlive ? $"{target.Hp}/{target.Stats.MaxHp}" : "down";
                    Entry(menu, target.Name, detail, true, () => Choose(MakeAction(aimed)), Preview(actor, target));
                }
                targetChoices = targetChoices.OrderBy(t => t.Team == actor.Team ? 0 : 1).ThenBy(t => t.Slot).ToList();
            }
            Entry(menu, "Back", null, true, Back, null, silent: true);
            menu.FocusFirstEnabled();
            OnTargetFocus(null);
            hud.SetHint("Arrows or mouse pick   Enter confirm");
        }

        void OnTargetFocus(string _)
        {
            if (level != MenuLevel.Target || pendingActor == null)
            {
                return;
            }
            int index = hud.CommandMenu.Focus;
            if (targetArea)
            {
                SetRings(pendingActor, index == 0 ? targetChoices : null);
                hud.HighlightPlate(null);
                return;
            }
            if (index >= targetChoices.Count)
            {
                SetRings(pendingActor, null);
                hud.HighlightPlate(null);
                return;
            }
            Combatant target = targetChoices[index];
            SetRings(pendingActor, new[] { target });
            hud.HighlightPlate(target);
        }

        string Preview(Combatant actor, Combatant target)
        {
            if (pendingIsItem)
            {
                return Catalog.Item(pendingItem).Description;
            }
            if (pendingSkill.IsDamage)
            {
                int expected = Formulas.ExpectedDamage(actor, target, pendingSkill);
                return $"About {expected} damage{(expected >= target.Hp ? " - enough to finish it" : string.Empty)}.";
            }
            if (pendingSkill.Effect == SkillEffect.Heal)
            {
                return $"Heals about {Formulas.ExpectedHeal(actor, pendingSkill)} HP.";
            }
            return pendingSkill.Description;
        }

        BattleAction MakeAction(Combatant target) =>
            pendingIsItem ? BattleAction.UseItem(pendingItem, target) : BattleAction.UseSkill(pendingSkill, target);

        void Back()
        {
            sound.Play(art.Cancel, 0.7f);
            switch (level)
            {
                case MenuLevel.Target:
                    hud.HighlightPlate(null);
                    if (targetReturn == MenuLevel.Skills) OpenSkills();
                    else if (targetReturn == MenuLevel.Items) OpenItems();
                    else OpenRootMenu();
                    break;
                case MenuLevel.Skills:
                case MenuLevel.Items:
                    OpenRootMenu();
                    break;
            }
        }

        /// <summary>Submits the current hero's action (also used by tests and screenshots).</summary>
        public void Choose(BattleAction action)
        {
            if (pendingActor == null)
            {
                return;
            }
            chosen = action;
        }

        public void ToggleAuto()
        {
            AutoBattle = !AutoBattle;
            hud.SetAuto(AutoBattle);
            sound.Play(art.Confirm, 0.6f);
        }

        /// <summary>Adds a menu entry that plays the confirm sound when chosen.</summary>
        void Entry(MenuList menu, string label, string detail, bool enabled, Action action, string description,
            Action<int> adjust = null, bool silent = false, bool quiet = false)
        {
            menu.Add(label, detail, enabled, () =>
            {
                if (!silent && !quiet)
                {
                    sound.Play(art.Confirm, 0.7f);
                }
                action();
            }, description, adjust);
        }

        // ---- Input ----

        void Update()
        {
            if (Phase == GamePhase.Battle && !IsPaused && input.ToggleAuto)
            {
                ToggleAuto();
            }

            if (settingsOpen)
            {
                DriveMenu(hud.OverlayMenu, CloseSettings, horizontalAdjusts: true);
                return;
            }
            if (IsPaused)
            {
                if (input.Pause || input.Escape)
                {
                    Resume();
                    return;
                }
                DriveMenu(hud.OverlayMenu, Resume);
                return;
            }
            switch (Phase)
            {
                case GamePhase.Battle:
                    if (input.Pause || (input.Escape && (pendingActor == null || level == MenuLevel.Root)))
                    {
                        TogglePause();
                        return;
                    }
                    if (pendingActor != null)
                    {
                        if (input.Escape)
                        {
                            Back();
                            return;
                        }
                        DriveMenu(hud.CommandMenu, level == MenuLevel.Root ? null : (Action)Back, targetMode: level == MenuLevel.Target);
                        if (level == MenuLevel.Target)
                        {
                            PickWithPointer();
                        }
                    }
                    break;
                default:
                    if (hud.OverlayVisible)
                    {
                        DriveMenu(hud.OverlayMenu, null);
                    }
                    break;
            }
        }

        void DriveMenu(MenuList menu, Action back, bool horizontalAdjusts = false, bool targetMode = false)
        {
            Vector2Int step = input.Navigate();
            if (step.y != 0)
            {
                menu.Move(-step.y);
            }
            else if (step.x != 0)
            {
                if (horizontalAdjusts)
                {
                    menu.Adjust(step.x);
                }
                else if (targetMode)
                {
                    menu.Move(step.x);
                }
            }
            if (input.Confirm)
            {
                menu.Confirm();
            }
            else if (input.Cancel || (input.Escape && back != null))
            {
                back?.Invoke();
            }
        }

        /// <summary>Pointing at a fighter highlights it as the target; clicking it confirms.</summary>
        void PickWithPointer()
        {
            if (targetArea || targetChoices.Count == 0 || hud.CommandMenu.Count != targetChoices.Count + 1)
            {
                return;
            }
            Vector2 pointer = hud.ScreenToPanel(input.PointerPosition);
            int best = -1;
            float bestDistance = 70f;
            for (int i = 0; i < targetChoices.Count; i++)
            {
                UnitView unitView = ViewOf(targetChoices[i]);
                float distance = Vector2.Distance(pointer, hud.WorldToPanel(unitView.Center));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            if (best < 0)
            {
                return;
            }
            if (input.PointerMoved && hud.CommandMenu.Focus != best)
            {
                hud.CommandMenu.SetFocus(best, true);
            }
            if (input.Click)
            {
                hud.CommandMenu.SetFocus(best, false);
                hud.CommandMenu.Confirm();
            }
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
