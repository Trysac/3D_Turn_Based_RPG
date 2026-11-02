using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>Particle effects made in code; each cleans itself up when it finishes.</summary>
    public static class Effects
    {
        public static void Burst(GameArt art, Vector3 position, Color colorA, Color colorB, int count, float speed, float size,
            float lifetime, float gravity = 0f, bool additive = true, float radius = 0.2f)
        {
            var host = new GameObject("Burst");
            host.transform.position = position;
            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            host.GetComponent<ParticleSystemRenderer>().sharedMaterial = additive ? art.ParticleAdditive : art.ParticleAlpha;
            system.Play();
        }

        public static void Sparks(GameArt art, Vector3 at, bool critical) =>
            Burst(art, at, Color.white, critical ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.6f, 0.4f), critical ? 34 : 18,
                critical ? 6.5f : 4.5f, 0.22f, 0.4f, 1.5f);

        public static void Flames(GameArt art, Vector3 at) =>
            Burst(art, at, new Color(1f, 0.8f, 0.2f), new Color(1f, 0.3f, 0.05f), 30, 3f, 0.5f, 0.7f, -0.6f, radius: 0.45f);

        public static void Shadow(GameArt art, Vector3 at) =>
            Burst(art, at, new Color(0.55f, 0.35f, 1f), new Color(0.3f, 0.9f, 1f), 24, 2.5f, 0.42f, 0.7f, -0.3f, radius: 0.4f);

        public static void Sparkle(GameArt art, Vector3 at, Color color) =>
            Burst(art, at, Color.white, color, 26, 1.6f, 0.26f, 1.0f, -0.5f, radius: 0.55f);

        public static void Poison(GameArt art, Vector3 at) =>
            Burst(art, at, new Color(0.55f, 1f, 0.35f), new Color(0.3f, 0.7f, 0.2f), 16, 1.2f, 0.3f, 0.8f, -0.4f, false, 0.4f);

        public static void Dust(GameArt art, Vector3 at) =>
            Burst(art, at, new Color(0.7f, 0.65f, 0.6f, 0.8f), new Color(0.4f, 0.38f, 0.35f, 0.6f), 22, 2f, 0.55f, 0.9f, 0.2f, false, 0.6f);
    }
}
