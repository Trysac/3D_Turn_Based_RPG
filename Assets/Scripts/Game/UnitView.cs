using System.Collections;
using TurnBasedRpg.Core;
using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// The 3D figure of one fighter: idle bob, lunges and casts, white hit flashes through property
    /// blocks, a coloured ring for the active fighter and targets, and a fall when defeated.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Transform body;
        Renderer[] renderers;
        Color[] baseColors;
        Color[] baseEmission;
        MaterialPropertyBlock block;
        Renderer ring;
        Color ringColor;
        bool ringOn;
        float flash;
        Color flashColor = Color.white;
        float fallen;
        float bobOffset;
        Vector3 home;
        Quaternion facing;

        public Combatant Unit { get; private set; }
        public float Height { get; private set; }
        public Vector3 Home => home;
        /// <summary>Where floating numbers and name plates go.</summary>
        public Vector3 HeadPoint => transform.position + Vector3.up * (Height + 0.25f);
        public Vector3 Center => transform.position + Vector3.up * (Height * 0.5f);

        public void Init(Combatant unit, GameArt art, Vector3 position, Vector3 lookAt)
        {
            Unit = unit;
            name = unit.Name;
            home = position;
            transform.position = position;
            Vector3 forward = lookAt - position;
            forward.y = 0f;
            facing = Quaternion.LookRotation(forward.normalized, Vector3.up);
            transform.rotation = facing;

            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            Models.Model model = Models.Build(unit.Def.Id, art, body);
            Height = model.Height;
            bobOffset = Random.value * 10f;

            var ringObject = Shapes.Part(transform, art.Cylinder, art.Ring, Color.white, new Vector3(0f, 0.02f, 0f),
                new Vector3(unit.Def.IsBoss ? 2.6f : 1.35f, 0.01f, unit.Def.IsBoss ? 2.6f : 1.35f));
            ring = ringObject.GetComponent<Renderer>();
            ring.enabled = false;

            renderers = body.GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            baseEmission = new Color[renderers.Length];
            block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(block);
                baseColors[i] = block.GetColor(ColorId);
                baseEmission[i] = block.GetColor(EmissionId);
            }
            if (!unit.IsAlive)
            {
                fallen = 1f;
            }
        }

        /// <summary>Shows a ring under the fighter; pass null to hide it.</summary>
        public void SetRing(Color? color)
        {
            ringOn = color.HasValue;
            ring.enabled = ringOn;
            if (ringOn)
            {
                ringColor = color.Value;
            }
        }

        public void Flash(Color color, float strength = 1f)
        {
            flashColor = color;
            flash = Mathf.Max(flash, strength);
        }

        /// <summary>Steps toward <paramref name="target"/>, strikes, and returns. The hit lands at the midpoint.</summary>
        public IEnumerator Lunge(Vector3 target, float duration, System.Action onHit)
        {
            Vector3 toward = target - home;
            toward.y = 0f;
            Vector3 strike = home + toward * 0.62f;
            float half = duration * 0.5f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = t / half;
                transform.position = Vector3.Lerp(home, strike, k * k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.25f;
                yield return null;
            }
            transform.position = strike;
            onHit?.Invoke();
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                float k = t / half;
                transform.position = Vector3.Lerp(strike, home, 1f - (1f - k) * (1f - k));
                yield return null;
            }
            transform.position = home;
        }

        /// <summary>A short hop with a glow while casting.</summary>
        public IEnumerator Cast(Color glow, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                transform.position = home + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
                Flash(glow, 0.5f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            transform.position = home;
        }

        /// <summary>A quick knock back for being hit.</summary>
        public IEnumerator Recoil(float duration)
        {
            Vector3 back = -(facing * Vector3.forward) * 0.25f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                transform.position = home + back * Mathf.Sin(k * Mathf.PI);
                yield return null;
            }
            transform.position = home;
        }

        public void ResetPose()
        {
            transform.position = home;
            fallen = Unit.IsAlive ? 0f : 1f;
        }

        void Update()
        {
            if (body == null)
            {
                return;
            }
            bool alive = Unit.IsAlive;
            fallen = Mathf.MoveTowards(fallen, alive ? 0f : 1f, Time.deltaTime * (alive ? 3f : 2.2f));
            float bob = alive ? Mathf.Sin((Time.time + bobOffset) * 2.4f) * 0.04f : 0f;
            bool stunned = alive && Unit.Has(StatusKind.Stun);
            float wobble = stunned ? Mathf.Sin(Time.time * 9f) * 6f : 0f;
            body.localPosition = new Vector3(0f, bob - fallen * 0.15f, 0f);
            body.localRotation = Quaternion.Euler(-fallen * 82f, 0f, wobble);
            body.localScale = Vector3.one * (1f - fallen * 0.1f);

            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 4f);
            float dim = fallen * 0.55f;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(block);
                block.SetColor(ColorId, Color.Lerp(baseColors[i], new Color(0.25f, 0.25f, 0.28f), dim));
                block.SetColor(EmissionId, Color.Lerp(baseEmission[i] * (1f - fallen), flashColor * 1.6f, flash));
                renderers[i].SetPropertyBlock(block);
            }

            if (ringOn)
            {
                Color pulse = ringColor * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f));
                pulse.a = ringColor.a;
                ring.GetPropertyBlock(block);
                block.SetColor(ColorId, pulse);
                ring.SetPropertyBlock(block);
            }
        }
    }
}
