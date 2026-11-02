using System;
using System.Collections.Generic;
using TurnBasedRpg.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// The whole UI, built in code on one UI Toolkit document and styled by Hud.uss: name plates
    /// over the fighters, floating numbers, the turn order, the party panel, the command menu,
    /// banners, and full-screen cards (title, map, camp, results, pause, settings, ending).
    /// Laid out at 1280x720 reference pixels; the panel settings scale it to the screen.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Hud : MonoBehaviour
    {
        const float FloaterLifetime = 1.1f;

        sealed class Plate
        {
            public UnitView View;
            public VisualElement Root;
            public VisualElement Fill;
            public Label Hp;
            public Label Statuses;
        }

        sealed class FloatingText
        {
            public Label Label;
            public Vector3 World;
            public float Age;
            public float Lifetime;
        }

        sealed class PartyRow
        {
            public VisualElement Root;
            public Label Name;
            public Label Level;
            public VisualElement HpFill;
            public Label Hp;
            public VisualElement MpFill;
            public Label Mp;
            public Label Statuses;
        }

        VisualElement root;
        VisualElement plateLayer;
        VisualElement floaterLayer;
        VisualElement battleHud;
        Label stageLabel;
        Label roundLabel;
        VisualElement turnOrder;
        VisualElement partyPanel;
        readonly List<PartyRow> partyRows = new List<PartyRow>();
        VisualElement commandPanel;
        Label commandTitle;
        Label commandDescription;
        Label hint;
        Label banner;
        Label subBanner;
        Button autoButton;
        VisualElement overlay;
        VisualElement card;
        Label overlayHeading;
        Label overlaySub;
        Label overlayBody;
        IVisualElementScheduledItem bannerHide;
        readonly List<Plate> plates = new List<Plate>();
        readonly List<FloatingText> floaters = new List<FloatingText>();
        Camera view;

        public MenuList CommandMenu { get; private set; }
        public MenuList OverlayMenu { get; private set; }
        /// <summary>Extra content of the current card (map path, results), cleared on every ShowOverlay.</summary>
        public VisualElement OverlayExtra { get; private set; }
        public bool OverlayVisible => overlay != null && overlay.style.display == DisplayStyle.Flex;
        public string OverlayHeading => overlayHeading?.text;
        public string BannerText => banner?.text;

        public event Action AutoClicked;
        public event Action PauseClicked;

        public void Build()
        {
            if (root != null)
            {
                return;
            }
            var document = GetComponent<UIDocument>();
            root = document.rootVisualElement;
            root.Clear();
            root.AddToClassList("hud");
            root.pickingMode = PickingMode.Ignore;

            plateLayer = Layer("plates");
            floaterLayer = Layer("floaters");

            battleHud = Layer("battle");
            var top = Child(battleHud, "top");
            stageLabel = Text(top, "top__stage", "");
            var roundRow = Child(top, "top__row");
            roundLabel = Text(roundRow, "top__round", "");
            turnOrder = Child(roundRow, "order");

            var corner = Child(battleHud, "corner");
            autoButton = new Button(() => AutoClicked?.Invoke()) { text = "AUTO: OFF", focusable = false };
            autoButton.AddToClassList("pill");
            corner.Add(autoButton);
            var pauseButton = new Button(() => PauseClicked?.Invoke()) { text = "II", focusable = false };
            pauseButton.AddToClassList("pill");
            pauseButton.AddToClassList("pill--square");
            corner.Add(pauseButton);

            partyPanel = Child(battleHud, "party");
            for (int i = 0; i < 3; i++)
            {
                partyRows.Add(PartyRowFor(partyPanel));
            }

            commandPanel = Child(battleHud, "commands");
            commandTitle = Text(commandPanel, "commands__title", "");
            var list = Child(commandPanel, "menu");
            CommandMenu = new MenuList(list);
            commandDescription = Text(commandPanel, "commands__description", "");
            CommandMenu.FocusChanged += text => commandDescription.text = text ?? string.Empty;

            hint = Text(battleHud, "hint", "");
            banner = Text(root, "banner", "");
            subBanner = Text(root, "subbanner", "");
            banner.style.opacity = 0f;
            subBanner.style.opacity = 0f;

            overlay = Layer("overlay");
            overlay.pickingMode = PickingMode.Position;
            card = Child(overlay, "card");
            overlayHeading = Text(card, "card__heading", "");
            overlaySub = Text(card, "card__sub", "");
            overlayBody = Text(card, "card__body", "");
            overlayBody.enableRichText = true;
            OverlayExtra = Child(card, "card__extra");
            var overlayList = Child(card, "menu");
            overlayList.AddToClassList("menu--card");
            OverlayMenu = new MenuList(overlayList);
            HideOverlay();
            ShowBattleHud(false);
        }

        VisualElement Layer(string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            layer.AddToClassList("layer");
            layer.AddToClassList(name);
            root.Add(layer);
            return layer;
        }

        static void AddClasses(VisualElement element, string classNames)
        {
            foreach (string className in classNames.Split(' '))
            {
                if (className.Length > 0)
                {
                    element.AddToClassList(className);
                }
            }
        }

        static VisualElement Child(VisualElement parent, string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            AddClasses(element, className);
            parent.Add(element);
            return element;
        }

        static Label Text(VisualElement parent, string className, string text)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            AddClasses(label, className);
            parent.Add(label);
            return label;
        }

        static VisualElement Bar(VisualElement parent, string kind, out VisualElement fill)
        {
            var track = Child(parent, "bar");
            track.AddToClassList("bar--" + kind);
            fill = Child(track, "bar__fill");
            return track;
        }

        PartyRow PartyRowFor(VisualElement parent)
        {
            var row = new PartyRow { Root = Child(parent, "member") };
            var head = Child(row.Root, "member__head");
            row.Name = Text(head, "member__name", "");
            row.Level = Text(head, "member__level", "");
            row.Statuses = Text(head, "member__status", "");
            var hpLine = Child(row.Root, "member__line");
            Text(hpLine, "member__tag", "HP");
            Bar(hpLine, "hp", out row.HpFill);
            row.Hp = Text(hpLine, "member__value", "");
            var mpLine = Child(row.Root, "member__line");
            Text(mpLine, "member__tag", "MP");
            Bar(mpLine, "mp", out row.MpFill);
            row.Mp = Text(mpLine, "member__value", "");
            return row;
        }

        // ---- Overlay cards ----

        public void ShowOverlay(string heading, string sub, string body, string cardClass = null)
        {
            overlay.style.display = DisplayStyle.Flex;
            overlayHeading.text = heading;
            overlaySub.text = sub ?? string.Empty;
            overlaySub.style.display = string.IsNullOrEmpty(sub) ? DisplayStyle.None : DisplayStyle.Flex;
            overlayBody.text = body ?? string.Empty;
            overlayBody.style.display = string.IsNullOrEmpty(body) ? DisplayStyle.None : DisplayStyle.Flex;
            OverlayExtra.Clear();
            OverlayMenu.Clear();
            card.ClearClassList();
            card.AddToClassList("card");
            if (!string.IsNullOrEmpty(cardClass))
            {
                card.AddToClassList(cardClass);
            }
        }

        public void HideOverlay()
        {
            overlay.style.display = DisplayStyle.None;
        }

        public Label AddExtraLabel(string text, string className)
        {
            Label label = Text(OverlayExtra, className, text);
            label.enableRichText = true;
            return label;
        }

        /// <summary>The campaign path: one node per encounter, the current one highlighted.</summary>
        public void AddMapPath(Run run)
        {
            var path = Child(OverlayExtra, "path");
            for (int i = 0; i < Campaign.Encounters.Count; i++)
            {
                Encounter encounter = Campaign.Encounters[i];
                if (i > 0)
                {
                    var link = Child(path, "path__link");
                    link.EnableInClassList("path__link--done", i <= run.Stage);
                }
                var node = Child(path, "node");
                node.EnableInClassList("node--done", i < run.Stage);
                node.EnableInClassList("node--current", i == run.Stage);
                node.EnableInClassList("node--boss", encounter.IsBoss);
                Text(node, "node__dot", encounter.IsBoss ? "!" : (i + 1).ToString());
                Text(node, "node__name", encounter.Name);
            }
        }

        // ---- Battle HUD ----

        public void ShowBattleHud(bool visible)
        {
            battleHud.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            plateLayer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
            {
                ClearFloaters();
            }
        }

        public void ShowCommands(string title)
        {
            commandPanel.style.display = DisplayStyle.Flex;
            commandTitle.text = title;
        }

        public void HideCommands()
        {
            commandPanel.style.display = DisplayStyle.None;
        }

        public bool CommandsVisible => commandPanel.style.display == DisplayStyle.Flex;

        public void SetHint(string text)
        {
            hint.text = text ?? string.Empty;
            hint.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetAuto(bool on)
        {
            autoButton.text = on ? "AUTO: ON" : "AUTO: OFF";
            autoButton.EnableInClassList("pill--on", on);
        }

        public void SetStage(string text) => stageLabel.text = text;

        public void UpdateTurnOrder(int round, IReadOnlyList<Combatant> queue)
        {
            roundLabel.text = $"ROUND {round}";
            turnOrder.Clear();
            for (int i = 0; i < queue.Count && i < 7; i++)
            {
                Combatant unit = queue[i];
                Label chip = Text(turnOrder, "chip", Short(unit.Name));
                chip.AddToClassList(unit.Team == Team.Heroes ? "chip--hero" : "chip--enemy");
                chip.EnableInClassList("chip--current", i == 0);
            }
        }

        static string Short(string name) => name.StartsWith("The ") ? name.Substring(4) : name;

        public void UpdateParty(IReadOnlyList<Combatant> heroes, Combatant current)
        {
            for (int i = 0; i < partyRows.Count; i++)
            {
                PartyRow row = partyRows[i];
                if (i >= heroes.Count)
                {
                    row.Root.style.display = DisplayStyle.None;
                    continue;
                }
                Combatant hero = heroes[i];
                row.Root.style.display = DisplayStyle.Flex;
                row.Name.text = hero.Name;
                row.Level.text = $"Lv {hero.Level} {hero.Def.Role}";
                row.Hp.text = $"{hero.Hp}/{hero.Stats.MaxHp}";
                row.Mp.text = $"{hero.Mp}/{hero.Stats.MaxMp}";
                row.HpFill.style.width = Length.Percent(100f * hero.Hp / Mathf.Max(1, hero.Stats.MaxHp));
                row.MpFill.style.width = Length.Percent(100f * hero.Mp / Mathf.Max(1, hero.Stats.MaxMp));
                row.HpFill.EnableInClassList("bar__fill--low", hero.HpRatio < 0.3f);
                row.Statuses.text = StatusText(hero);
                row.Root.EnableInClassList("member--current", hero == current);
                row.Root.EnableInClassList("member--down", !hero.IsAlive);
            }
        }

        public static string StatusText(Combatant unit)
        {
            if (!unit.IsAlive)
            {
                return "DOWN";
            }
            var parts = new List<string>();
            foreach (StatusEffect status in unit.Statuses)
            {
                parts.Add(StatusShort(status.Kind));
            }
            return string.Join(" ", parts);
        }

        public static string StatusShort(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return "PSN";
                case StatusKind.Regen: return "RGN";
                case StatusKind.Stun: return "STN";
                case StatusKind.Strength: return "STR";
                case StatusKind.Weakness: return "WK";
                default: return "DEF";
            }
        }

        public void SetPlates(IEnumerable<UnitView> views)
        {
            plateLayer.Clear();
            plates.Clear();
            foreach (UnitView unitView in views)
            {
                var plate = new Plate { View = unitView, Root = Child(plateLayer, "plate") };
                plate.Root.AddToClassList(unitView.Unit.Team == Team.Heroes ? "plate--hero" : "plate--enemy");
                if (unitView.Unit.Def.IsBoss)
                {
                    plate.Root.AddToClassList("plate--boss");
                }
                Text(plate.Root, "plate__name", unitView.Unit.Name);
                Bar(plate.Root, unitView.Unit.Team == Team.Heroes ? "hp" : "enemy", out plate.Fill);
                var line = Child(plate.Root, "plate__line");
                plate.Hp = Text(line, "plate__hp", "");
                plate.Statuses = Text(line, "plate__status", "");
                plates.Add(plate);
            }
        }

        public void HighlightPlate(Combatant unit)
        {
            foreach (Plate plate in plates)
            {
                plate.Root.EnableInClassList("plate--target", plate.View.Unit == unit);
            }
        }

        // ---- Banners and floating text ----

        public void Banner(string text, string sub = null, float seconds = 1.2f, string className = null)
        {
            banner.text = text;
            banner.ClearClassList();
            banner.AddToClassList("banner");
            if (!string.IsNullOrEmpty(className))
            {
                banner.AddToClassList(className);
            }
            banner.style.opacity = 1f;
            subBanner.text = sub ?? string.Empty;
            subBanner.style.opacity = string.IsNullOrEmpty(sub) ? 0f : 1f;
            bannerHide?.Pause();
            if (seconds > 0f)
            {
                bannerHide = banner.schedule.Execute(HideBanner).StartingIn((long)(seconds * 1000));
            }
        }

        public void HideBanner()
        {
            banner.style.opacity = 0f;
            subBanner.style.opacity = 0f;
        }

        public void Floater(Vector3 world, string text, string className, float lifetime = FloaterLifetime)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("floater");
            label.AddToClassList(className);
            floaterLayer.Add(label);
            floaters.Add(new FloatingText { Label = label, World = world, Lifetime = lifetime });
            Place(label, world, 0f);
        }

        public void ClearFloaters()
        {
            foreach (FloatingText floater in floaters)
            {
                floater.Label.RemoveFromHierarchy();
            }
            floaters.Clear();
        }

        void LateUpdate()
        {
            if (root == null)
            {
                return;
            }
            view = view != null ? view : Camera.main;
            if (view == null)
            {
                return;
            }
            foreach (Plate plate in plates)
            {
                Combatant unit = plate.View.Unit;
                Place(plate.Root, plate.View.HeadPoint, 0f);
                plate.Fill.style.width = Length.Percent(100f * unit.Hp / Mathf.Max(1, unit.Stats.MaxHp));
                plate.Hp.text = unit.IsAlive ? $"{unit.Hp}" : "DOWN";
                plate.Statuses.text = unit.IsAlive ? StatusText(unit) : string.Empty;
                plate.Root.EnableInClassList("plate--down", !unit.IsAlive);
            }
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                FloatingText floater = floaters[i];
                floater.Age += Time.unscaledDeltaTime;
                float k = floater.Age / floater.Lifetime;
                if (k >= 1f)
                {
                    floater.Label.RemoveFromHierarchy();
                    floaters.RemoveAt(i);
                    continue;
                }
                Place(floater.Label, floater.World, -60f * k);
                floater.Label.style.opacity = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                float pop = k < 0.12f ? 1.35f - k / 0.12f * 0.35f : 1f;
                floater.Label.style.scale = new Scale(new Vector3(pop, pop, 1f));
            }
        }

        /// <summary>Centers an absolutely positioned element on a world point, shifted up by <paramref name="rise"/> pixels.</summary>
        void Place(VisualElement element, Vector3 world, float rise)
        {
            if (view == null || element.panel == null || view.WorldToViewportPoint(world).z < 0f)
            {
                return;
            }
            Vector2 point = WorldToPanel(world);
            element.style.left = point.x;
            element.style.top = point.y + rise;
            element.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-100f));
        }

        /// <summary>The panel position of a world point (used for mouse picking).</summary>
        /// <remarks>
        /// Goes through the camera's viewport and the panel's own size, so it holds for any window
        /// size and for the off-screen render used by the screenshot hook.
        /// </remarks>
        public Vector2 WorldToPanel(Vector3 world)
        {
            if (root == null || view == null)
            {
                return Vector2.zero;
            }
            Vector3 viewport = view.WorldToViewportPoint(world);
            Rect size = root.layout;
            return new Vector2(viewport.x * size.width, (1f - viewport.y) * size.height);
        }

        /// <summary>Converts a screen pixel (bottom-left origin) to panel coordinates.</summary>
        public Vector2 ScreenToPanel(Vector2 screen)
        {
            if (root == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return screen;
            }
            Rect size = root.layout;
            return new Vector2(screen.x / Screen.width * size.width, (1f - screen.y / Screen.height) * size.height);
        }
    }
}
