using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// Low-poly figures for every hero and enemy, assembled from primitives. Each model faces +Z;
    /// the view turns it towards the other side of the battlefield.
    /// </summary>
    public static class Models
    {
        public sealed class Model
        {
            public GameObject Root;
            /// <summary>Height of the top of the head, for name plates and floating numbers.</summary>
            public float Height;
        }

        public static Model Build(string unitId, GameArt art, Transform parent)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var model = new Model { Root = root };
            Transform t = root.transform;
            switch (unitId)
            {
                case "knight": Knight(t, art); model.Height = 1.95f; break;
                case "mage": Mage(t, art); model.Height = 2.1f; break;
                case "cleric": Cleric(t, art); model.Height = 1.85f; break;
                case "slime": Slime(t, art, "#6fd35a"); model.Height = 0.95f; break;
                case "goblin": Goblin(t, art, shaman: false); model.Height = 1.35f; break;
                case "goblin_shaman": Goblin(t, art, shaman: true); model.Height = 1.6f; break;
                case "wolf": Wolf(t, art); model.Height = 1.3f; break;
                case "skeleton": Skeleton(t, art); model.Height = 1.95f; break;
                case "wraith": Wraith(t, art); model.Height = 2.2f; break;
                case "imp": Imp(t, art); model.Height = 1.25f; break;
                case "cinder_king": CinderKing(t, art); model.Height = 3.5f; break;
                default: Slime(t, art, "#ff00ff"); model.Height = 1f; break;
            }
            return model;
        }

        static GameObject P(Transform t, Mesh mesh, GameArt art, string color, Vector3 position, Vector3 scale, Vector3 euler = default) =>
            Shapes.Part(t, mesh, art.Lit, Shapes.Hex(color), position, scale, euler);

        static GameObject Glow(Transform t, Mesh mesh, GameArt art, string color, Vector3 position, Vector3 scale, float intensity = 2f) =>
            Shapes.Part(t, mesh, art.Lit, Shapes.Hex(color), position, scale, default, Shapes.Hex(color) * intensity);

        static void Eyes(Transform t, GameArt art, string color, float y, float z, float spread, float size)
        {
            Glow(t, art.Sphere, art, color, new Vector3(-spread, y, z), Vector3.one * size, 1.5f);
            Glow(t, art.Sphere, art, color, new Vector3(spread, y, z), Vector3.one * size, 1.5f);
        }

        static void Knight(Transform t, GameArt art)
        {
            P(t, art.Capsule, art, "#9aa6b8", new Vector3(0, 0.85f, 0), new Vector3(0.62f, 0.62f, 0.5f));
            P(t, art.Cylinder, art, "#2f4f8f", new Vector3(0, 0.62f, 0), new Vector3(0.66f, 0.1f, 0.54f));
            P(t, art.Sphere, art, "#c8d0dc", new Vector3(0, 1.62f, 0), Vector3.one * 0.48f);
            P(t, art.Cube, art, "#1c2230", new Vector3(0, 1.62f, 0.2f), new Vector3(0.34f, 0.08f, 0.12f));
            P(t, art.Cube, art, "#c23b3b", new Vector3(0, 1.92f, -0.05f), new Vector3(0.06f, 0.18f, 0.32f));
            P(t, art.Sphere, art, "#c8d0dc", new Vector3(-0.38f, 1.25f, 0), Vector3.one * 0.26f);
            P(t, art.Sphere, art, "#c8d0dc", new Vector3(0.38f, 1.25f, 0), Vector3.one * 0.26f);
            // Shield on the left arm, sword in the right hand.
            P(t, art.Cube, art, "#2f4f8f", new Vector3(-0.42f, 0.95f, 0.22f), new Vector3(0.08f, 0.62f, 0.48f));
            P(t, art.Cube, art, "#e0b84e", new Vector3(-0.47f, 0.95f, 0.22f), new Vector3(0.03f, 0.14f, 0.14f));
            P(t, art.Cube, art, "#dfe6ee", new Vector3(0.45f, 1.05f, 0.42f), new Vector3(0.07f, 0.06f, 0.9f), new Vector3(-20, 0, 0));
            P(t, art.Cube, art, "#e0b84e", new Vector3(0.45f, 0.93f, 0.02f), new Vector3(0.24f, 0.06f, 0.06f));
            P(t, art.Cylinder, art, "#3a3f4a", new Vector3(-0.16f, 0.22f, 0), new Vector3(0.2f, 0.22f, 0.2f));
            P(t, art.Cylinder, art, "#3a3f4a", new Vector3(0.16f, 0.22f, 0), new Vector3(0.2f, 0.22f, 0.2f));
        }

        static void Mage(Transform t, GameArt art)
        {
            P(t, Shapes.Cone, art, "#6a3fa8", new Vector3(0, 0, 0), new Vector3(0.85f, 1.35f, 0.85f));
            P(t, art.Sphere, art, "#f1c9a5", new Vector3(0, 1.4f, 0), Vector3.one * 0.4f);
            P(t, art.Cylinder, art, "#3d2366", new Vector3(0, 1.58f, 0), new Vector3(0.7f, 0.025f, 0.7f));
            P(t, Shapes.Cone, art, "#4b2a7d", new Vector3(0, 1.58f, 0), new Vector3(0.42f, 0.55f, 0.42f), new Vector3(-12, 0, 0));
            P(t, art.Sphere, art, "#f2f0e6", new Vector3(0, 1.26f, 0.15f), new Vector3(0.24f, 0.2f, 0.12f));
            P(t, art.Cylinder, art, "#7b5536", new Vector3(0.42f, 0.95f, 0.15f), new Vector3(0.06f, 0.9f, 0.06f));
            Glow(t, art.Sphere, art, "#58d7ff", new Vector3(0.42f, 1.9f, 0.15f), Vector3.one * 0.2f, 2.5f);
        }

        static void Cleric(Transform t, GameArt art)
        {
            P(t, art.Capsule, art, "#efe9da", new Vector3(0, 0.78f, 0), new Vector3(0.62f, 0.62f, 0.55f));
            P(t, art.Cube, art, "#e0b84e", new Vector3(0, 0.95f, 0.27f), new Vector3(0.1f, 0.42f, 0.03f));
            P(t, art.Cube, art, "#e0b84e", new Vector3(0, 1.05f, 0.27f), new Vector3(0.28f, 0.1f, 0.03f));
            P(t, art.Sphere, art, "#e7b48c", new Vector3(0, 1.52f, 0), Vector3.one * 0.42f);
            P(t, art.Sphere, art, "#8a5a2b", new Vector3(0, 1.6f, -0.04f), new Vector3(0.45f, 0.3f, 0.45f));
            P(t, art.Cylinder, art, "#c9a14a", new Vector3(-0.42f, 0.9f, 0.1f), new Vector3(0.06f, 0.88f, 0.06f));
            Glow(t, art.Sphere, art, "#ffe28a", new Vector3(-0.42f, 1.82f, 0.1f), new Vector3(0.24f, 0.24f, 0.08f), 2f);
        }

        static void Slime(Transform t, GameArt art, string color)
        {
            P(t, art.Sphere, art, color, new Vector3(0, 0.38f, 0), new Vector3(1.0f, 0.75f, 1.0f));
            P(t, art.Sphere, art, color, new Vector3(0, 0.75f, -0.05f), new Vector3(0.5f, 0.35f, 0.5f));
            Eyes(t, art, "#ffffff", 0.52f, 0.42f, 0.17f, 0.18f);
            P(t, art.Sphere, art, "#11151e", new Vector3(-0.17f, 0.52f, 0.5f), Vector3.one * 0.08f);
            P(t, art.Sphere, art, "#11151e", new Vector3(0.17f, 0.52f, 0.5f), Vector3.one * 0.08f);
        }

        static void Goblin(Transform t, GameArt art, bool shaman)
        {
            string skin = shaman ? "#7fa35a" : "#6bbf4e";
            P(t, art.Capsule, art, shaman ? "#5b3a6e" : "#7a5230", new Vector3(0, 0.55f, 0), new Vector3(0.5f, 0.42f, 0.42f));
            P(t, art.Sphere, art, skin, new Vector3(0, 1.08f, 0.02f), Vector3.one * 0.46f);
            P(t, Shapes.Cone, art, skin, new Vector3(-0.3f, 1.12f, 0), new Vector3(0.14f, 0.36f, 0.08f), new Vector3(0, 0, 70));
            P(t, Shapes.Cone, art, skin, new Vector3(0.3f, 1.12f, 0), new Vector3(0.14f, 0.36f, 0.08f), new Vector3(0, 0, -70));
            P(t, art.Sphere, art, skin, new Vector3(0, 1.02f, 0.24f), new Vector3(0.12f, 0.1f, 0.14f));
            Eyes(t, art, "#ffd23f", 1.14f, 0.2f, 0.1f, 0.08f);
            if (shaman)
            {
                P(t, art.Cylinder, art, "#5b4030", new Vector3(0.36f, 0.75f, 0.12f), new Vector3(0.05f, 0.75f, 0.05f));
                P(t, art.Sphere, art, "#eee6cf", new Vector3(0.36f, 1.5f, 0.12f), new Vector3(0.2f, 0.24f, 0.2f));
                Glow(t, art.Sphere, art, "#b46cff", new Vector3(0.36f, 1.5f, 0.24f), Vector3.one * 0.07f, 3f);
                P(t, Shapes.Cone, art, "#3c2a4a", new Vector3(0, 1.26f, -0.02f), new Vector3(0.36f, 0.4f, 0.36f));
            }
            else
            {
                P(t, art.Cube, art, "#c7ccd4", new Vector3(0.33f, 0.62f, 0.3f), new Vector3(0.05f, 0.05f, 0.45f));
                P(t, art.Cube, art, "#5b4030", new Vector3(0.33f, 0.62f, 0.05f), new Vector3(0.12f, 0.05f, 0.05f));
            }
        }

        static void Wolf(Transform t, GameArt art)
        {
            const string fur = "#5d6577";
            P(t, art.Capsule, art, fur, new Vector3(0, 0.7f, -0.1f), new Vector3(0.55f, 0.6f, 0.55f), new Vector3(90, 0, 0));
            P(t, art.Sphere, art, fur, new Vector3(0, 0.98f, 0.55f), new Vector3(0.48f, 0.44f, 0.5f));
            P(t, art.Cube, art, "#454c5b", new Vector3(0, 0.9f, 0.85f), new Vector3(0.22f, 0.18f, 0.32f));
            P(t, art.Sphere, art, "#1b1e26", new Vector3(0, 0.94f, 1.0f), Vector3.one * 0.09f);
            P(t, Shapes.Cone, art, fur, new Vector3(-0.15f, 1.15f, 0.5f), new Vector3(0.14f, 0.26f, 0.1f));
            P(t, Shapes.Cone, art, fur, new Vector3(0.15f, 1.15f, 0.5f), new Vector3(0.14f, 0.26f, 0.1f));
            Eyes(t, art, "#ffcc33", 1.03f, 0.76f, 0.12f, 0.07f);
            foreach (var x in new[] { -0.17f, 0.17f })
            {
                P(t, art.Cylinder, art, "#454c5b", new Vector3(x, 0.24f, 0.28f), new Vector3(0.12f, 0.26f, 0.12f));
                P(t, art.Cylinder, art, "#454c5b", new Vector3(x, 0.24f, -0.45f), new Vector3(0.12f, 0.26f, 0.12f));
            }
            P(t, art.Capsule, art, fur, new Vector3(0, 0.82f, -0.82f), new Vector3(0.14f, 0.3f, 0.14f), new Vector3(-50, 0, 0));
        }

        static void Skeleton(Transform t, GameArt art)
        {
            const string bone = "#e8e2cf";
            P(t, art.Cylinder, art, bone, new Vector3(0, 1.0f, 0), new Vector3(0.1f, 0.35f, 0.1f));
            for (int i = 0; i < 3; i++)
            {
                P(t, art.Cube, art, bone, new Vector3(0, 1.18f - i * 0.15f, 0.02f), new Vector3(0.5f - i * 0.06f, 0.05f, 0.28f));
            }
            P(t, art.Cube, art, bone, new Vector3(0, 0.66f, 0), new Vector3(0.36f, 0.1f, 0.2f));
            P(t, art.Sphere, art, bone, new Vector3(0, 1.6f, 0.02f), new Vector3(0.42f, 0.44f, 0.42f));
            P(t, art.Cube, art, bone, new Vector3(0, 1.42f, 0.08f), new Vector3(0.24f, 0.1f, 0.22f));
            Eyes(t, art, "#ff5a3c", 1.64f, 0.2f, 0.09f, 0.08f);
            P(t, art.Cylinder, art, bone, new Vector3(-0.13f, 0.32f, 0), new Vector3(0.07f, 0.33f, 0.07f));
            P(t, art.Cylinder, art, bone, new Vector3(0.13f, 0.32f, 0), new Vector3(0.07f, 0.33f, 0.07f));
            P(t, art.Cylinder, art, bone, new Vector3(-0.32f, 0.95f, 0.05f), new Vector3(0.06f, 0.3f, 0.06f), new Vector3(0, 0, -12));
            P(t, art.Cylinder, art, bone, new Vector3(0.32f, 0.95f, 0.05f), new Vector3(0.06f, 0.3f, 0.06f), new Vector3(0, 0, 12));
            P(t, art.Cube, art, "#7d838c", new Vector3(0.36f, 0.95f, 0.35f), new Vector3(0.08f, 0.08f, 0.9f), new Vector3(-15, 0, 0));
            P(t, art.Cylinder, art, "#6b4a2f", new Vector3(-0.42f, 1.0f, 0.18f), new Vector3(0.5f, 0.03f, 0.5f), new Vector3(0, 0, 80));
        }

        static void Wraith(Transform t, GameArt art)
        {
            P(t, Shapes.Cone, art, "#262338", new Vector3(0, 0.25f, 0), new Vector3(0.9f, 1.6f, 0.9f));
            P(t, art.Sphere, art, "#2e2a45", new Vector3(0, 1.62f, 0), new Vector3(0.55f, 0.6f, 0.55f));
            P(t, art.Sphere, art, "#0b0a12", new Vector3(0, 1.58f, 0.12f), new Vector3(0.38f, 0.42f, 0.38f));
            Eyes(t, art, "#7ef0ff", 1.62f, 0.3f, 0.09f, 0.08f);
            P(t, art.Capsule, art, "#2e2a45", new Vector3(-0.42f, 1.15f, 0.15f), new Vector3(0.14f, 0.36f, 0.14f), new Vector3(60, 0, 20));
            P(t, art.Capsule, art, "#2e2a45", new Vector3(0.42f, 1.15f, 0.15f), new Vector3(0.14f, 0.36f, 0.14f), new Vector3(60, 0, -20));
            Glow(t, art.Sphere, art, "#7ef0ff", new Vector3(0, 0.3f, 0), new Vector3(0.7f, 0.08f, 0.7f), 0.6f);
        }

        static void Imp(Transform t, GameArt art)
        {
            P(t, art.Sphere, art, "#c2402a", new Vector3(0, 0.62f, 0), new Vector3(0.62f, 0.7f, 0.58f));
            P(t, art.Sphere, art, "#d4532f", new Vector3(0, 1.05f, 0.05f), Vector3.one * 0.44f);
            P(t, Shapes.Cone, art, "#3a2018", new Vector3(-0.14f, 1.2f, 0), new Vector3(0.1f, 0.26f, 0.1f), new Vector3(0, 0, 20));
            P(t, Shapes.Cone, art, "#3a2018", new Vector3(0.14f, 1.2f, 0), new Vector3(0.1f, 0.26f, 0.1f), new Vector3(0, 0, -20));
            Eyes(t, art, "#ffe066", 1.08f, 0.2f, 0.09f, 0.08f);
            P(t, art.Cube, art, "#8a2a1c", new Vector3(-0.38f, 0.8f, -0.15f), new Vector3(0.04f, 0.32f, 0.4f), new Vector3(0, -30, 25));
            P(t, art.Cube, art, "#8a2a1c", new Vector3(0.38f, 0.8f, -0.15f), new Vector3(0.04f, 0.32f, 0.4f), new Vector3(0, 30, -25));
            Glow(t, art.Sphere, art, "#ffb347", new Vector3(0.3f, 0.75f, 0.32f), Vector3.one * 0.16f, 3f);
        }

        static void CinderKing(Transform t, GameArt art)
        {
            P(t, art.Capsule, art, "#2b1f1c", new Vector3(0, 1.45f, 0), new Vector3(1.25f, 1.3f, 0.95f));
            Glow(t, art.Cube, art, "#ff6a1f", new Vector3(0, 1.55f, 0.43f), new Vector3(0.12f, 0.9f, 0.05f), 2.5f);
            Glow(t, art.Cube, art, "#ff6a1f", new Vector3(-0.25f, 1.25f, 0.43f), new Vector3(0.4f, 0.08f, 0.05f), 2.5f);
            P(t, art.Sphere, art, "#3a2924", new Vector3(0, 2.75f, 0.05f), Vector3.one * 0.78f);
            Eyes(t, art, "#ffd23f", 2.8f, 0.36f, 0.16f, 0.12f);
            P(t, art.Cylinder, art, "#e0b84e", new Vector3(0, 3.1f, 0.05f), new Vector3(0.66f, 0.1f, 0.66f));
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                P(t, Shapes.Cone, art, "#e0b84e", new Vector3(Mathf.Sin(angle) * 0.27f, 3.18f, Mathf.Cos(angle) * 0.27f + 0.05f),
                    new Vector3(0.14f, 0.32f, 0.14f));
            }
            P(t, art.Sphere, art, "#3a2924", new Vector3(-0.8f, 2.05f, 0), Vector3.one * 0.62f);
            P(t, art.Sphere, art, "#3a2924", new Vector3(0.8f, 2.05f, 0), Vector3.one * 0.62f);
            P(t, art.Capsule, art, "#2b1f1c", new Vector3(-0.92f, 1.3f, 0.2f), new Vector3(0.4f, 0.6f, 0.4f), new Vector3(20, 0, 10));
            P(t, art.Capsule, art, "#2b1f1c", new Vector3(0.92f, 1.3f, 0.2f), new Vector3(0.4f, 0.6f, 0.4f), new Vector3(20, 0, -10));
            Glow(t, art.Sphere, art, "#ff6a1f", new Vector3(-0.98f, 0.75f, 0.42f), Vector3.one * 0.48f, 0.8f);
            Glow(t, art.Sphere, art, "#ff6a1f", new Vector3(0.98f, 0.75f, 0.42f), Vector3.one * 0.48f, 0.8f);
            P(t, art.Cylinder, art, "#2b1f1c", new Vector3(-0.35f, 0.3f, 0), new Vector3(0.42f, 0.32f, 0.42f));
            P(t, art.Cylinder, art, "#2b1f1c", new Vector3(0.35f, 0.3f, 0), new Vector3(0.42f, 0.32f, 0.42f));
        }
    }
}
