using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>Where the run, the records and the settings are kept.</summary>
    public interface ISaveStore
    {
        string Load(string key);
        void Save(string key, string value);
        void Delete(string key);
    }

    /// <summary>PlayerPrefs on desktop; browser storage on WebGL.</summary>
    public sealed class PlayerPrefsStore : ISaveStore
    {
        const string Prefix = "embervale.";

        public string Load(string key) => PlayerPrefs.GetString(Prefix + key, null);

        public void Save(string key, string value)
        {
            PlayerPrefs.SetString(Prefix + key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(Prefix + key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Keeps everything in memory, for tests and screenshots.</summary>
    public sealed class MemoryStore : ISaveStore
    {
        readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public string Load(string key) => values.TryGetValue(key, out string value) ? value : null;

        public void Save(string key, string value) => values[key] = value;

        public void Delete(string key) => values.Remove(key);
    }

    /// <summary>Player options, saved as one "key=value" text.</summary>
    public sealed class Settings
    {
        public const string Key = "settings";
        public static readonly float[] Speeds = { 1f, 1.5f, 2f, 3f };

        public int MusicPercent = 60;
        public int EffectsPercent = 90;
        public int SpeedIndex = 1;
        public bool Shake = true;

        public float Speed => Speeds[Mathf.Clamp(SpeedIndex, 0, Speeds.Length - 1)];

        public static Settings Load(ISaveStore store)
        {
            var settings = new Settings();
            string text = store.Load(Key);
            if (string.IsNullOrEmpty(text))
            {
                return settings;
            }
            foreach (string line in text.Split('\n'))
            {
                int equals = line.IndexOf('=');
                if (equals <= 0 || !int.TryParse(line.Substring(equals + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    continue;
                }
                switch (line.Substring(0, equals))
                {
                    case "music": settings.MusicPercent = Mathf.Clamp(value, 0, 100); break;
                    case "effects": settings.EffectsPercent = Mathf.Clamp(value, 0, 100); break;
                    case "speed": settings.SpeedIndex = Mathf.Clamp(value, 0, Speeds.Length - 1); break;
                    case "shake": settings.Shake = value != 0; break;
                }
            }
            return settings;
        }

        public void Save(ISaveStore store) =>
            store.Save(Key, $"music={MusicPercent}\neffects={EffectsPercent}\nspeed={SpeedIndex}\nshake={(Shake ? 1 : 0)}\n");
    }
}
