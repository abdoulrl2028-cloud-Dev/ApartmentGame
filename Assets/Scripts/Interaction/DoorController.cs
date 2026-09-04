using System.Collections;
using ApartmentAfterDark.Audio;
using UnityEngine;

namespace ApartmentAfterDark.Interaction
{
    /// <summary>
    /// Interactive door (hinge rotate or slide). Extends InteractableBase so it works
    /// with InteractionSystem out of the box. Plays open/close sounds via AudioManager.
    /// </summary>
    public class DoorController : InteractableBase
    {
        [Header("Door Settings")]
        [Tooltip("Open angle around local Y (hinge doors).")]
        [SerializeField] private float openAngle = 90f;

        [Tooltip("Seconds to fully open/close.")]
        [SerializeField] private float openSpeed = 2f;

        [Tooltip("If true, the door slides on a rail instead of rotating.")]
        [SerializeField] private bool slidingDoor = false;

        [Tooltip("Slide distance (sliding doors).")]
        [SerializeField] private float slideDistance = 0.9f;

        [Tooltip("Slide direction in local space (sliding doors).")]
        [SerializeField] private Vector3 slideDirection = Vector3.forward;

        [Header("State")]
        [SerializeField] private bool startOpen = false;
        [SerializeField] private bool autoClose = false;
        [SerializeField] private float autoCloseDelay = 5f;

        [Header("Audio")]
        [Tooltip("Sound played when opening (SFX channel).")]
        [SerializeField] private AudioClip openSound;

        [Tooltip("Sound played when closing (SFX channel).")]
        [SerializeField] private AudioClip closeSound;

        [Tooltip("Volume of door sounds (0-1).")]
        [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

        private Vector3 startLocalPos;
        private Quaternion startRotation;
        private Coroutine animateRoutine;
        private bool isOpen;

        public bool IsOpen => isOpen;

        protected override void Reset()
        {
            base.Reset();
            interactableName = "Door";
            interactionPrompt = "E: Open door";
        }

        private void Awake()
        {
            startLocalPos = transform.localPosition;
            startRotation = transform.localRotation;

            if (startOpen)
            {
                isOpen = true;
                ApplyOpenInstant();
            }
        }

        public override void OnInteract(GameObject interactor)
        {
            base.OnInteract(interactor);
            Toggle();
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            PlayAudio(openSound);
            if (animateRoutine != null) StopCoroutine(animateRoutine);
            animateRoutine = StartCoroutine(Animate(true));
            if (autoClose) StartCoroutine(AutoClose());
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            PlayAudio(closeSound);
            if (animateRoutine != null) StopCoroutine(animateRoutine);
            animateRoutine = StartCoroutine(Animate(false));
        }

        private void PlayAudio(AudioClip clip)
        {
            if (clip == null) return;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayOneShotSfx(clip, soundVolume, 1f, transform.position);
        }

        private IEnumerator AutoClose()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            if (isOpen) Close();
        }

        private IEnumerator Animate(bool open)
        {
            float elapsed = 0f;

            Quaternion targetRot = open
                ? Quaternion.Euler(0f, openAngle, 0f) * startRotation
                : startRotation;

            Vector3 targetPos = open
                ? startLocalPos + slideDirection.normalized * (slidingDoor ? slideDistance : 0f)
                : startLocalPos;

            Quaternion sourceRot = transform.localRotation;
            Vector3 sourcePos = transform.localPosition;

            while (elapsed < openSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / openSpeed);

                if (slidingDoor)
                {
                    transform.localPosition = Vector3.Lerp(sourcePos, targetPos, t);
                }
                else
                {
                    transform.localRotation = Quaternion.Slerp(sourceRot, targetRot, t);
                }

                yield return null;
            }

            if (slidingDoor) transform.localPosition = targetPos;
            else transform.localRotation = targetRot;

            animateRoutine = null;
        }

        private void ApplyOpenInstant()
        {
            if (slidingDoor)
                transform.localPosition = startLocalPos + slideDirection.normalized * slideDistance;
            else
                transform.localRotation = Quaternion.Euler(0f, openAngle, 0f) * startRotation;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 dir = Quaternion.Euler(0f, openAngle, 0f) * transform.forward;
            Gizmos.DrawRay(transform.position, dir * 1f);
        }
    }
}
