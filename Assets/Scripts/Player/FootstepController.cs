using ApartmentAfterDark.Audio;
using UnityEngine;

namespace ApartmentAfterDark.Player
{
    /// <summary>
    /// Plays footstep sounds based on horizontal movement speed. Attach to the Player.
    /// Readings go through the AudioManager SFX channel.
    /// </summary>
    public class FootstepController : MonoBehaviour
    {
        [Header("Movement Reference")]
        [Tooltip("PlayerController used to detect movement. Auto-detected if empty.")]
        [SerializeField] private PlayerController player;

        [Tooltip("Distance in world units between footstep triggers (walking).")]
        [SerializeField] private float stepInterval = 2f;

        [Tooltip("Multiplier applied to the step interval while sprinting (longer strides).")]
        [SerializeField] private float sprintStepMultiplier = 1.35f;

        [Header("Sounds")]
        [Tooltip("Wood/parquet footstep clips, played at random.")]
        [SerializeField] private AudioClip[] woodFootsteps;

        [Tooltip("Concrete/tile footstep clips, played at random. Falls back to wood if empty.")]
        [SerializeField] private AudioClip[] concreteFootsteps;

        [Tooltip("Playback volume (0-1).")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

        [Header("Pitch")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField, Range(0.5f, 2f)] private float minPitch = 0.9f;
        [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.1f;

        private float distanceSinceStep;

        private bool CanStep => player != null && player.IsGrounded &&
                                player.HorizontalVelocity.magnitude > 0.5f;

        private void Update()
        {
            if (!CanStep)
            {
                distanceSinceStep = 0f;
                return;
            }

            float interval = stepInterval * (player.IsSprinting ? sprintStepMultiplier : 1f);
            distanceSinceStep += player.HorizontalVelocity.magnitude * Time.deltaTime;

            if (distanceSinceStep >= interval)
            {
                distanceSinceStep = 0f;
                PlayStep();
            }
        }

        private void PlayStep()
        {
            AudioClip[] pool = concreteFootsteps != null && concreteFootsteps.Length > 0
                ? concreteFootsteps
                : woodFootsteps;

            if (pool == null || pool.Length == 0) return;

            AudioClip clip = pool[Random.Range(0, pool.Length)];
            if (clip == null) return;

            float pitch = randomizePitch ? Random.Range(minPitch, maxPitch) : 1f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayOneShotSfx(clip, volume, pitch, transform.position);
            }
            else
            {
                // Fallback: temporary audio source
                AudioSource temp = gameObject.AddComponent<AudioSource>();
                temp.spatialBlend = 0.9f;
                temp.pitch = pitch;
                temp.PlayOneShot(clip, volume);
                Destroy(temp, clip.length + 1f);
            }
        }
    }
}
