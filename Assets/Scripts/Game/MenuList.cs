using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// A list of buttons driven by keyboard, gamepad and mouse alike. The list keeps its own focus
    /// (a highlighted entry) instead of UI Toolkit focus, so a held key can never press a button
    /// by accident; hovering an entry moves the highlight there and clicking chooses it.
    /// </summary>
    public sealed class MenuList
    {
        sealed class Entry
        {
            public Button Button;
            public string Label;
            public Label Detail;
            public bool Enabled;
            public Action Choose;
            public Action<int> Adjust;
            public string Description;
        }

        readonly List<Entry> entries = new List<Entry>();
        int focus;

        public MenuList(VisualElement root)
        {
            Root = root;
        }

        public VisualElement Root { get; }
        public int Count => entries.Count;
        public int Focus => focus;
        public string FocusedLabel => focus < entries.Count ? entries[focus].Label : null;

        /// <summary>Raised with the focused entry's description when the highlight moves.</summary>
        public event Action<string> FocusChanged;
        /// <summary>Raised when the highlight moves by input (for the menu tick sound).</summary>
        public event Action Moved;
        /// <summary>Raised when a disabled entry is chosen.</summary>
        public event Action Refused;

        public void Clear()
        {
            entries.Clear();
            Root.Clear();
            focus = 0;
        }

        /// <param name="adjust">Optional left/right handler, for settings values.</param>
        public void Add(string label, string detail, bool enabled, Action choose, string description = null, Action<int> adjust = null)
        {
            var button = new Button { text = string.Empty, focusable = false };
            button.AddToClassList("entry");
            var nameLabel = new Label(label) { pickingMode = PickingMode.Ignore };
            nameLabel.AddToClassList("entry__label");
            button.Add(nameLabel);
            var detailLabel = new Label(detail ?? string.Empty) { pickingMode = PickingMode.Ignore };
            detailLabel.AddToClassList("entry__detail");
            button.Add(detailLabel);
            var entry = new Entry { Button = button, Label = label, Detail = detailLabel, Enabled = enabled, Choose = choose, Adjust = adjust, Description = description };
            int index = entries.Count;
            button.clicked += () =>
            {
                SetFocus(index, false);
                Confirm();
            };
            button.RegisterCallback<PointerEnterEvent>(_ => SetFocus(index, true));
            button.EnableInClassList("entry--disabled", !enabled);
            entries.Add(entry);
            Root.Add(button);
            Refresh();
        }

        public void SetDetail(int index, string detail)
        {
            if (index >= 0 && index < entries.Count)
            {
                entries[index].Detail.text = detail;
            }
        }

        /// <summary>Moves the highlight to the first entry that can be chosen.</summary>
        public void FocusFirstEnabled()
        {
            int index = entries.FindIndex(e => e.Enabled);
            SetFocus(index < 0 ? 0 : index, false);
        }

        public void SetFocus(int index, bool fromInput)
        {
            if (entries.Count == 0)
            {
                return;
            }
            index = Math.Max(0, Math.Min(entries.Count - 1, index));
            bool changed = index != focus;
            focus = index;
            Refresh();
            if (changed && fromInput)
            {
                Moved?.Invoke();
            }
        }

        public void Move(int delta)
        {
            if (entries.Count == 0)
            {
                return;
            }
            SetFocus((focus + delta + entries.Count) % entries.Count, true);
        }

        /// <summary>Left/right on an adjustable entry; false when the entry has no adjuster.</summary>
        public bool Adjust(int delta)
        {
            if (focus >= entries.Count || entries[focus].Adjust == null)
            {
                return false;
            }
            entries[focus].Adjust(delta);
            return true;
        }

        public void Confirm()
        {
            if (focus >= entries.Count)
            {
                return;
            }
            Entry entry = entries[focus];
            if (!entry.Enabled)
            {
                Refused?.Invoke();
                return;
            }
            entry.Choose?.Invoke();
        }

        /// <summary>Chooses the entry with this label (used by tests and screenshots).</summary>
        public bool Choose(string label)
        {
            int index = entries.FindIndex(e => e.Label == label);
            if (index < 0)
            {
                return false;
            }
            SetFocus(index, false);
            Confirm();
            return true;
        }

        void Refresh()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Button.EnableInClassList("entry--focused", i == focus);
            }
            if (focus < entries.Count)
            {
                FocusChanged?.Invoke(entries[focus].Description);
            }
        }
    }
}
