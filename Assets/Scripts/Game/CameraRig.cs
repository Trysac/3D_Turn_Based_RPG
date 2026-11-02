using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// A fixed three-quarter view of the battlefield that leans toward the action (a small push
    /// toward the acting fighter or the target) and shakes from a trauma value. On the title and
    /// map screens it orbits slowly.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        static readonly Vector3 BasePosition = new Vector3(0f, 5.4f, -12.5f);
        static readonly Vector3 LookPoint = new Vector3(0f, 0.55f, 1.1f);

        float trauma;
        Vector3 focus;
        Vector3 wantedFocus;
        float push;
        float wantedPush;

        /// <summary>Screen shake can be turned off in the settings.</summary>
        public bool ShakeEnabled { get; set; } = true;

        /// <summary>0 = still battle view; above 0 = slow orbit for menus.</summary>
        public float Drift { get; set; }

        public void Shake(float amount)
        {
            if (ShakeEnabled)
            {
                trauma = Mathf.Clamp01(trauma + amount);
            }
        }

        /// <summary>Leans toward a world point; strength 0 returns to the neutral view.</summary>
        public void Focus(Vector3 point, float strength)
        {
            wantedFocus = point;
            wantedPush = strength;
        }

        public void Neutral() => Focus(LookPoint, 0f);

        public void Snap()
        {
            focus = wantedFocus;
            push = wantedPush;
            trauma = 0f;
        }

        void Awake()
        {
            wantedFocus = focus = LookPoint;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            focus = Vector3.Lerp(focus, wantedFocus, 1f - Mathf.Exp(-dt * 5f));
            push = Mathf.Lerp(push, wantedPush, 1f - Mathf.Exp(-dt * 5f));

            float yaw = Drift > 0f ? Mathf.Sin(Time.unscaledTime * 0.15f) * 14f * Drift : 0f;
            Vector3 look = Vector3.Lerp(LookPoint, focus, 0.35f * push);
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * (BasePosition - LookPoint);
            Vector3 position = look + offset * (1f - 0.18f * push);

            trauma = Mathf.Max(0f, trauma - dt * 1.6f);
            float strength = trauma * trauma;
            float t = Time.unscaledTime * 26f;
            var shake = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (0.8f * strength);
            Quaternion rotation = Quaternion.LookRotation(look - position, Vector3.up);
            float roll = (Mathf.PerlinNoise(t * 0.7f, 5.1f) - 0.5f) * 4f * strength;
            transform.SetPositionAndRotation(position + rotation * shake, rotation * Quaternion.Euler(0f, 0f, roll));
        }
    }
}
