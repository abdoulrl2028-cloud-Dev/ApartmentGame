using ApartmentAfterDark.Audio;
using UnityEngine;

namespace ApartmentAfterDark.Interaction
{
    /// <summary>
    /// Interactive television. Toggles a visible screen (emissive material / Image) and
    /// plays looped ambient room noise / channel zapping sound while on.
    /// </summary>
    public class TelevisionController : InteractableBase
    {
        [Tooltip("Renderer whose material shows the 'screen on' emission. Optional.")]
        [SerializeField] private Renderer screenRenderer;

        [Tooltip("Material used when the TV is on (emissive). Optional.")]
        [SerializeField] private Material onScreenMaterial;

        [Tooltip("Default material used when the TV is off. Optional.")]
        [SerializeField] private Material offScreenMaterial;

        [Tooltip("Spacey room tone played while on (looped).")]
        [SerializeField] private AudioClip onLoop;

        [Tooltip("Zapping/click sound played on power toggle (SFX).")]
        [SerializeField] private AudioClip toggleSound;

        [Tooltip("Volume of the on-loop (0-1).")]
        [SerializeField, Range(0f, 1f)] private float loopVolume = 0.4f;

        [SerializeField] private bool startOn = false;

        private AudioSource loopSource;
        private bool isOn;

        public bool IsOn => isOn;

        protected override void Reset()
        {
            base.Reset();
            interactableName = "Television";
            interactionPrompt = "E: Toggle TV";
        }

        private void Awake()
        {
            SetupLoopSource();
            SetOn(startOn);
        }

        private void SetupLoopSource()
        {
            loopSource = gameObject.GetComponent<AudioSource>();
            if (loopSource == null)
                loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.volume = loopVolume;
            loopSource.spatialBlend = 0.8f;
            loopSource.maxDistance = 15f;
        }

        public override void OnInteract(GameObject interactor)
        {
            base.OnInteract(interactor);
            Toggle();
        }

        public void Toggle() => SetOn(!isOn);

        public void SetOn(bool on)
        {
            isOn = on;
            ApplyVisuals();
            ApplyAudio();
            if (toggleSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayOneShotSfx(toggleSound, 0.7f, 1f, transform.position);
        }

        private void ApplyVisuals()
        {
            if (screenRenderer == null || onScreenMaterial == null) return;
            screenRenderer.sharedMaterial = isOn ? onScreenMaterial : offScreenMaterial;
            screenRenderer.enabled = true;
        }

        private void ApplyAudio()
        {
            if (loopSource == null) return;

            if (isOn && onLoop != null && !loopSource.isPlaying)
            {
                loopSource.clip = onLoop;
                loopSource.Play();
            }
            else if (!isOn)
            {
                loopSource.Stop();
            }
        }
    }
}
