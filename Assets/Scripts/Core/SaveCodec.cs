using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TurnBasedRpg.Core
{
    /// <summary>
    /// Turns a run and the records into short "key=value" text and back. Reading never throws:
    /// anything missing, malformed or from another version gives null (or empty records).
    /// </summary>
    public static class SaveCodec
    {
        public const int Version = 1;

        public static string WriteRun(Run run)
        {
            var text = new StringBuilder();
            Line(text, "version", Version.ToString(CultureInfo.InvariantCulture));
            Line(text, "seed", Number(run.Seed));
            Line(text, "stage", Number(run.Stage));
            Line(text, "attempt", Number(run.Attempt));
            Line(text, "turns", Number(run.TotalTurns));
            Line(text, "camp", run.AwaitingCamp ? "1" : "0");
            Line(text, "items", string.Join(",", Catalog.Items.Select(i => Number(run.Inventory.Count(i.Id)))));
            for (int i = 0; i < run.Party.Count; i++)
            {
                PartyMember m = run.Party[i];
                Line(text, "hero" + i, string.Join(",", m.Def.Id, Number(m.Level), Number(m.Xp), Number(m.Hp), Number(m.Mp)));
            }
            return text.ToString();
        }

        public static Run ReadRun(string text)
        {
            Dictionary<string, string> values = Parse(text);
            if (values == null || !Int(values, "version", out int version) || version != Version)
            {
                return null;
            }
            if (!Int(values, "seed", out int seed) || !Int(values, "stage", out int stage) || !Int(values, "attempt", out int attempt) ||
                !Int(values, "turns", out int turns) || !Int(values, "camp", out int camp) || !values.TryGetValue("items", out string items))
            {
                return null;
            }

            string[] counts = items.Split(',');
            if (counts.Length != Catalog.Items.Count)
            {
                return null;
            }
            var inventory = new Inventory();
            for (int i = 0; i < counts.Length; i++)
            {
                if (!TryNumber(counts[i], out int count) || count < 0)
                {
                    return null;
                }
                inventory.Add((ItemId)i, count);
            }

            var party = new List<PartyMember>();
            for (int i = 0; values.TryGetValue("hero" + i, out string hero); i++)
            {
                string[] parts = hero.Split(',');
                UnitDef def = i < Catalog.Party.Count && parts.Length == 5 && Catalog.Party[i].Id == parts[0] ? Catalog.Party[i] : null;
                if (def == null || !TryNumber(parts[1], out int level) || !TryNumber(parts[2], out int xp) ||
                    !TryNumber(parts[3], out int hp) || !TryNumber(parts[4], out int mp))
                {
                    return null;
                }
                party.Add(new PartyMember(def, level, xp, hp, mp));
            }
            if (party.Count != Catalog.Party.Count)
            {
                return null;
            }

            var run = new Run(seed, party, inventory);
            if (stage < 0 || stage >= Campaign.Encounters.Count)
            {
                // The game deletes the run once the campaign is over, so it never saves one past the end.
                return null;
            }
            run.Restore(stage, attempt, turns, camp == 1);
            return run;
        }

        public static string WriteRecords(Records records)
        {
            var text = new StringBuilder();
            Line(text, "version", Version.ToString(CultureInfo.InvariantCulture));
            Line(text, "clears", Number(records.Clears));
            Line(text, "best", Number(records.BestTurns));
            Line(text, "furthest", Number(records.FurthestStage));
            return text.ToString();
        }

        public static Records ReadRecords(string text)
        {
            var records = new Records();
            Dictionary<string, string> values = Parse(text);
            if (values != null && Int(values, "version", out int version) && version == Version &&
                Int(values, "clears", out int clears) && Int(values, "best", out int best) && Int(values, "furthest", out int furthest))
            {
                records.Restore(clears, best, furthest);
            }
            return records;
        }

        static Dictionary<string, string> Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    return null;
                }
                values[line.Substring(0, equals)] = line.Substring(equals + 1);
            }
            return values;
        }

        static bool Int(Dictionary<string, string> values, string key, out int value)
        {
            value = 0;
            return values.TryGetValue(key, out string text) && TryNumber(text, out value);
        }

        static bool TryNumber(string text, out int value) =>
            int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        static void Line(StringBuilder text, string key, string value) => text.Append(key).Append('=').Append(value).Append('\n');
    }
}
