using UnityEngine;

namespace ApartmentAfterDark.Audio
{
    /// <summary>
    /// Trigger zone that crossfades the ambient audio when the player enters/exits.
    /// Place on a GameObject with a BoxCollider/SphereCollider marked as trigger,
    /// sized to cover a room.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AudioZone : MonoBehaviour
    {
        [Tooltip("Ambient clip to crossfade to while the player is inside this zone.")]
        [SerializeField] private AudioClip ambientClip;

        [Tooltip("Crossfade duration in seconds.")]
        [SerializeField] private float fadeDuration = 1.5f;

        [Tooltip("If true, plays the clip only once on entry (not looping).")]
        [SerializeField] private bool playOnce = false;

        private bool wasInside;
        private bool hasFired;

        private void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            wasInside = true;
            if (playOnce && hasFired) return;
            hasFired = true;

            if (AudioManager.Instance != null)
            {
                if (playOnce)
                    AudioManager.Instance.PlayOneShotSfx(ambientClip, 0.8f, 1f, transform.position);
                else
                    AudioManager.Instance.CrossfadeAmbient(ambientClip, fadeDuration);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            wasInside = false;
        }

        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") ||
                   other.GetComponent<Player.PlayerController>() != null ||
                   other.attachedRigidbody != null;
        }
    }
}
