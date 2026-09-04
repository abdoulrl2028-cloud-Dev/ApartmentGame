using ApartmentAfterDark.Audio;
using UnityEngine;

namespace ApartmentAfterDark.Interaction
{
    /// <summary>
    /// Interactive wall light switch. Toggles the assigned Light(s) and plays a click
    /// sound. Extends InteractableBase so InteractionSystem can target it.
    /// </summary>
    public class LightSwitch : InteractableBase
    {
        [Tooltip("Lights toggled by this switch.")]
        [SerializeField] private Light[] controlledLights;

        [Tooltip("Click sound (SFX).")]
        [SerializeField] private AudioClip clickSound;

        [Tooltip("Volume of the click (0-1).")]
        [SerializeField, Range(0f, 1f)] private float soundVolume = 0.7f;

        [SerializeField] private bool startOn = true;

        private bool isOn;

        public bool IsOn => isOn;

        protected override void Reset()
        {
            base.Reset();
            interactableName = "Light Switch";
            interactionPrompt = "E: Toggle light";
        }

        private void Awake()
        {
            isOn = startOn;
            ApplyState();
        }

        public override void OnInteract(GameObject interactor)
        {
            base.OnInteract(interactor);
            Toggle();
        }

        public void Toggle()
        {
            isOn = !isOn;
            ApplyState();
            PlayClick();
        }

        public void SetOn(bool on)
        {
            isOn = on;
            ApplyState();
        }

        private void ApplyState()
        {
            if (controlledLights == null) return;
            foreach (Light l in controlledLights)
            {
                if (l != null) l.enabled = isOn;
            }
        }

        private void PlayClick()
        {
            if (clickSound == null) return;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayOneShotSfx(clickSound, soundVolume, 1f, transform.position);
        }
    }
}
