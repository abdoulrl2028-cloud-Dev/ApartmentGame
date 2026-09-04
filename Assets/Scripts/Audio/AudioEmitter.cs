using UnityEngine;

namespace ApartmentAfterDark.Audio
{
    /// <summary>
    /// Attach to any object (TV, refrigerator, machine, loop source, etc.) to play a
    /// looped/spatial sound. Starts/stops via public methods or Play On Awake toggle.
    /// </summary>
    public class AudioEmitter : MonoBehaviour
    {
        [Tooltip("Clip to play.")]
        [SerializeField] private AudioClip clip;

        [Tooltip("Play on Awake.")]
        [SerializeField] private bool playOnAwake = true;

        [Tooltip("Loop the clip.")]
        [SerializeField] private bool loop = true;

        [Tooltip("Volume (0-1).")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

        [Tooltip("Spatial blend: 0=2D, 1=3D world.")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;

        [Tooltip("Max audible distance for 3D sound.")]
        [SerializeField] private float maxDistance = 20f;

        private AudioSource source;

        public bool IsPlaying => source != null && source.isPlaying;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.priority = 128;
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.maxDistance = maxDistance;
            source.clip = clip;
            source.rolloffMode = AudioRolloffMode.Logarithmic;

            // Route SFX one-shots through AudioManager only for one-shots; loops keep as 2D-ish
        }

        private void Start()
        {
            if (playOnAwake && clip != null)
                Play();
        }

        public void Play()
        {
            if (source == null || clip == null) return;
            source.Play();
        }

        public void Stop()
        {
            if (source != null) source.Stop();
        }

        public void SetClip(AudioClip newClip)
        {
            clip = newClip;
            if (source != null) source.clip = newClip;
        }

        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
            if (source != null) source.volume = volume;
        }
    }
}
