using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>Cross-fades the music between screens and plays one-shot effects at the set volumes.</summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        AudioSource music;
        AudioSource effects;
        AudioClip wanted;
        float fade = 1f;

        public float MusicVolume { get; set; } = 0.6f;
        public float EffectsVolume { get; set; } = 0.9f;

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.ignoreListenerPause = true;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.ignoreListenerPause = true;
        }

        public void PlayMusic(AudioClip clip) => wanted = clip;

        public void Play(AudioClip clip, float volume = 1f, float pitchJitter = 0.04f)
        {
            if (clip == null || effects == null || EffectsVolume <= 0f)
            {
                return;
            }
            effects.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            effects.PlayOneShot(clip, volume * EffectsVolume);
        }

        void Update()
        {
            if (music == null)
            {
                return;
            }
            if (music.clip != wanted)
            {
                fade = Mathf.MoveTowards(fade, 0f, Time.unscaledDeltaTime * 2.5f);
                if (fade <= 0f || !music.isPlaying)
                {
                    music.clip = wanted;
                    if (wanted != null)
                    {
                        music.Play();
                    }
                }
            }
            else
            {
                fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime * 1.5f);
            }
            music.volume = 0.45f * MusicVolume * fade;
        }
    }
}
