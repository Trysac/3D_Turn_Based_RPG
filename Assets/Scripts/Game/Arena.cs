using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// The battlefield scenery for each campaign stage, generated from primitives with a fixed
    /// seed per stage: ground, props in a ring around the fighters, sky colour, fog and light.
    /// </summary>
    public sealed class Arena : MonoBehaviour
    {
        sealed class Palette
        {
            public string Sky, Ground, GroundEdge, Fog, Sun, Ambient;
        }

        static readonly Palette[] Palettes =
        {
            new Palette { Sky = "#9ccff0", Ground = "#5f9b47", GroundEdge = "#4b7d39", Fog = "#b8dcf0", Sun = "#fff1d6", Ambient = "#8aa0b8" },
            new Palette { Sky = "#e8b98a", Ground = "#a08a52", GroundEdge = "#7e6a3c", Fog = "#e8c9a0", Sun = "#ffd9a8", Ambient = "#a08a78" },
            new Palette { Sky = "#3d5a4a", Ground = "#33533a", GroundEdge = "#26402c", Fog = "#3d5a4a", Sun = "#cfe8c8", Ambient = "#4e6a5c" },
            new Palette { Sky = "#232a3d", Ground = "#4a4f5c", GroundEdge = "#383c47", Fog = "#232a3d", Sun = "#a8c4ff", Ambient = "#4a5270" },
            new Palette { Sky = "#2a1010", Ground = "#3a2622", GroundEdge = "#2a1a17", Fog = "#4a1a10", Sun = "#ffb07a", Ambient = "#6a3a30" },
        };

        GameArt art;
        Light sun;
        Transform props;

        public void Init(GameArt gameArt, Light sunLight)
        {
            art = gameArt;
            sun = sunLight;
        }

        /// <summary>Rebuilds the scenery for a stage (0..4).</summary>
        public void Build(int stage)
        {
            stage = Mathf.Clamp(stage, 0, Palettes.Length - 1);
            if (props != null)
            {
                Destroy(props.gameObject);
            }
            props = new GameObject("Scenery").transform;
            props.SetParent(transform, false);
            Palette palette = Palettes[stage];

            Camera view = Camera.main;
            if (view != null)
            {
                view.backgroundColor = Shapes.Hex(palette.Sky);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Shapes.Hex(palette.Fog);
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 42f;
            RenderSettings.ambientLight = Shapes.Hex(palette.Ambient);
            if (sun != null)
            {
                sun.color = Shapes.Hex(palette.Sun);
            }

            Shapes.Part(props, art.Cylinder, art.Lit, Shapes.Hex(palette.GroundEdge), new Vector3(0, -0.6f, 2f), new Vector3(46f, 0.5f, 34f));
            Shapes.Part(props, art.Cylinder, art.Lit, Shapes.Hex(palette.Ground), new Vector3(0, -0.12f, 0.5f), new Vector3(15f, 0.1f, 9f));

            var random = new System.Random(1000 + stage);
            for (int i = 0; i < 26; i++)
            {
                // Props sit in a band behind and beside the fighters, never between them and the camera.
                float angle = Mathf.Lerp(-20f, 200f, (float)random.NextDouble()) * Mathf.Deg2Rad;
                float radius = Mathf.Lerp(9f, 17f, (float)random.NextDouble());
                var position = new Vector3(Mathf.Cos(angle) * radius * 1.3f, 0f, Mathf.Sin(angle) * radius * 0.8f + 4f);
                float size = Mathf.Lerp(0.7f, 1.4f, (float)random.NextDouble());
                Prop(stage, random, position, size);
            }
            if (stage == 4)
            {
                for (int i = 0; i < 9; i++)
                {
                    var crack = new Vector3(Mathf.Lerp(-9f, 9f, (float)random.NextDouble()), -0.05f, Mathf.Lerp(-2f, 9f, (float)random.NextDouble()));
                    Shapes.Part(props, art.Cube, art.Lit, Shapes.Hex("#ff5a1a"), crack, new Vector3(Mathf.Lerp(0.8f, 2.5f, (float)random.NextDouble()), 0.06f, 0.12f),
                        new Vector3(0, (float)random.NextDouble() * 180f, 0), Shapes.Hex("#ff5a1a") * 2f);
                }
            }
        }

        void Prop(int stage, System.Random random, Vector3 at, float size)
        {
            var root = new GameObject("Prop").transform;
            root.SetParent(props, false);
            root.localPosition = at;
            root.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            root.localScale = Vector3.one * size;
            int kind = random.Next(3);
            switch (stage)
            {
                case 0:
                case 2:
                    if (kind < 2) Tree(root, stage == 2 ? "#2f5d34" : "#4f9a3c", stage == 2 ? 1.5f : 1f, stage == 2);
                    else Rock(root, "#8b8f94");
                    break;
                case 1:
                    if (kind == 0) Tree(root, "#8a8a3a", 0.8f, false);
                    else Rock(root, "#a8865c");
                    break;
                case 3:
                    if (kind == 0) Pillar(root, "#6b7080");
                    else if (kind == 1) Grave(root, "#7d8290");
                    else Rock(root, "#565b66");
                    break;
                default:
                    if (kind == 0) Spire(root, "#2a1d1a");
                    else Rock(root, "#3d2b26");
                    break;
            }
        }

        void Tree(Transform t, string leaves, float height, bool pine)
        {
            Shapes.Part(t, art.Cylinder, art.Lit, Shapes.Hex("#6b4a2f"), new Vector3(0, 0.7f * height, 0), new Vector3(0.28f, 0.7f * height, 0.28f));
            if (pine)
            {
                for (int i = 0; i < 3; i++)
                {
                    Shapes.Part(t, Shapes.Cone, art.Lit, Shapes.Hex(leaves), new Vector3(0, 1.1f * height + i * 0.75f, 0),
                        new Vector3(1.9f - i * 0.45f, 1.3f, 1.9f - i * 0.45f));
                }
            }
            else
            {
                Shapes.Part(t, art.Sphere, art.Lit, Shapes.Hex(leaves), new Vector3(0, 1.9f * height, 0), new Vector3(1.8f, 1.6f, 1.8f));
                Shapes.Part(t, art.Sphere, art.Lit, Shapes.Hex(leaves), new Vector3(0.5f, 1.6f * height, 0.3f), new Vector3(1.1f, 1f, 1.1f));
            }
        }

        void Rock(Transform t, string color)
        {
            Shapes.Part(t, art.Sphere, art.Lit, Shapes.Hex(color), new Vector3(0, 0.25f, 0), new Vector3(1.4f, 0.8f, 1.1f), new Vector3(10, 20, 5));
            Shapes.Part(t, art.Sphere, art.Lit, Shapes.Hex(color), new Vector3(0.6f, 0.15f, 0.2f), new Vector3(0.7f, 0.45f, 0.6f));
        }

        void Pillar(Transform t, string color)
        {
            Shapes.Part(t, art.Cylinder, art.Lit, Shapes.Hex(color), new Vector3(0, 1.6f, 0), new Vector3(0.7f, 1.6f, 0.7f));
            Shapes.Part(t, art.Cube, art.Lit, Shapes.Hex(color), new Vector3(0, 3.3f, 0), new Vector3(1f, 0.2f, 1f));
            Shapes.Part(t, art.Sphere, art.Lit, Shapes.Hex("#7ef0ff"), new Vector3(0, 3.6f, 0), Vector3.one * 0.3f, default, Shapes.Hex("#7ef0ff") * 2f);
        }

        void Grave(Transform t, string color)
        {
            Shapes.Part(t, art.Cube, art.Lit, Shapes.Hex(color), new Vector3(0, 0.5f, 0), new Vector3(0.7f, 1f, 0.18f), new Vector3(0, 0, 6));
            Shapes.Part(t, art.Cube, art.Lit, Shapes.Hex(color), new Vector3(0, 0.9f, 0), new Vector3(0.5f, 0.12f, 0.2f), new Vector3(0, 0, 6));
        }

        void Spire(Transform t, string color)
        {
            Shapes.Part(t, Shapes.Cone, art.Lit, Shapes.Hex(color), Vector3.zero, new Vector3(1.4f, 3.8f, 1.4f));
            Shapes.Part(t, art.Sphere, art.Lit, Shapes.Hex("#ff7a2a"), new Vector3(0, 0.1f, 0), new Vector3(1.5f, 0.2f, 1.5f), default,
                Shapes.Hex("#ff7a2a") * 1.5f);
        }
    }
}
