using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ApartmentAfterDark.Audio
{
    /// <summary>
    /// Central audio controller. Creates persistent audio sources for Music, Ambient,
    /// SFX and Voice and routes them through an (optional) AudioMixer.
    /// Singleton accessible via <c>AudioManager.Instance</c>. Marked DontDestroyOnLoad.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [Tooltip("Unity Audio Mixer with group names Music/Ambient/SFX/Voice (optional).")]
        [SerializeField] private AudioMixer mixer;

        [Header("Ambient")]
        [Tooltip("Ambient loop. Played automatically on Start if assigned (over park).")]
        [SerializeField] private AudioClip defaultAmbient;
        [Tooltip("Ambient volume (0-1).")]
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;

        [Header("Music")]
        [Tooltip("Background music. Played automatically if assigned.")]
        [SerializeField] private AudioClip defaultMusic;
        [Tooltip("Music volume (0-1).")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("Runtime Volumes")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;

        private AudioSource musicSource;
        private AudioSource ambientSource;
        private AudioSource sfxSource;
        private AudioSource voiceSource;

        // Small object pool of spatial one-shot sources for world-space effects.
        private readonly List<AudioSource> pool = new List<AudioSource>();

        public float SfxVolume => sfxVolume;
        public float VoiceVolume => voiceVolume;
        public float MusicVolume => musicVolume;
        public float AmbientVolume => ambientVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateSources();
        }

        private void CreateSources()
        {
            musicSource = CreateChannel("MusicAudio", musicVolume, true);
            ambientSource = CreateChannel("AmbientAudio", ambientVolume, true);
            sfxSource = CreateChannel("SFXAudio", sfxVolume, false);
            voiceSource = CreateChannel("VoiceAudio", voiceVolume, false);
        }

        /// <summary>
        /// Looks for an existing child AudioSource named <paramref name="objectName"/>
        /// and configures it, or creates one if absent. This lets scene authors pre-place
        /// dedicated AmbientAudio / MusicAudio / SFXAudio / VoiceAudio sources (with clips,
        /// volumes and spatial blend) that this manager adopts instead of duplicating.
        /// </summary>
        private AudioSource CreateChannel(string objectName, float volume, bool loop)
        {
            Transform child = transform.Find(objectName);
            AudioSource src;
            if (child != null)
            {
                src = child.GetComponent<AudioSource>();
            }
            else
            {
                GameObject go = new GameObject(objectName);
                go.transform.SetParent(transform, false);
                src = go.AddComponent<AudioSource>();
                src.spatialBlend = 0f; // 2D for channels
                src.playOnAwake = false;
            }

            src.loop = loop;
            src.volume = volume;

            if (mixer != null)
            {
                // Redirect through mixer groups by name if present
                AudioMixerGroup group = FindGroup(objectName.Replace("Audio", ""));
                if (group != null) src.outputAudioMixerGroup = group;
            }

            return src;
        }

        private AudioMixerGroup FindGroup(string name)
        {
            if (mixer == null) return null;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(name);
            return groups != null && groups.Length > 0 ? groups[0] : null;
        }

        private void Start()
        {
            if (defaultMusic != null)
                PlayMusic(defaultMusic);
            if (defaultAmbient != null)
                PlayAmbient(defaultAmbient);
        }

        // ---------- Music ----------
        public void PlayMusic(AudioClip clip, float volume = -1f, bool loop = true)
        {
            if (clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = volume >= 0f ? volume : musicVolume;
            musicSource.Play();
        }

        public void StopMusic() => musicSource.Stop();

        // ---------- Ambient ----------
        public void PlayAmbient(AudioClip clip, float volume = -1f, bool loop = true)
        {
            if (clip == null) return;
            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.volume = volume >= 0f ? volume : ambientVolume;
            ambientSource.Play();
        }

        public void StopAmbient() => ambientSource.Stop();

        public void CrossfadeAmbient(AudioClip newClip, float duration = 1.5f)
        {
            if (newClip == null) return;
            if (ambientSource.clip == newClip) return;
            StartCoroutine(CrossfadeRoutine(ambientSource, newClip, ambientVolume, duration));
        }

        private IEnumerator CrossfadeRoutine(AudioSource source, AudioClip target, float targetVol, float duration)
        {
            float startVol = source.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            source.clip = target;
            source.volume = targetVol;
            source.Play();
        }

        // ---------- SFX (2D) ----------
        public void PlayOneShotSfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume * sfxVolume));
            sfxSource.pitch = 1f;
        }

        // ---------- SFX (3D world space) ----------
        public void PlayOneShotSfx(AudioClip clip, float volume, float pitch, Vector3 position)
        {
            if (clip == null) return;
            AudioSource src = GetPooledSource(position);
            src.pitch = pitch;
            src.PlayOneShot(clip, Mathf.Clamp01(volume * sfxVolume));
        }

        private AudioSource GetPooledSource(Vector3 position)
        {
            AudioSource best = null;
            foreach (AudioSource s in pool)
            {
                if (!s.isPlaying) { best = s; break; }
            }

            if (best == null)
            {
                GameObject go = new GameObject("SpatialSFX");
                go.transform.SetParent(transform, true);
                best = go.AddComponent<AudioSource>();
                best.spatialBlend = 1f;
                best.playOnAwake = false;
                if (mixer != null && FindGroup("SFX") != null) best.outputAudioMixerGroup = FindGroup("SFX");
                pool.Add(best);
            }

            best.transform.position = position;
            return best;
        }

        // ---------- Voice (dialogue/subtitles) ----------
        public void PlayVoice(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.volume = Mathf.Clamp01(volume * voiceVolume);
            voiceSource.Play();
        }

        public void StopVoice() => voiceSource.Stop();
        public bool VoicePlaying => voiceSource != null && voiceSource.isPlaying;
        public float VoiceProgress => voiceSource != null && voiceSource.clip != null ? voiceSource.time / voiceSource.clip.length : 0f;

        // ---------- Mixer exposure (used by SettingsMenu) ----------
        public void SetMixerVolume(string exposedParam, float linearVolume)
        {
            if (mixer == null) return;
            float db = linearVolume <= 0.0001f ? -80f : Mathf.Lerp(-48f, 0f, linearVolume);
            mixer.SetFloat(exposedParam, db);
        }
    }
}
