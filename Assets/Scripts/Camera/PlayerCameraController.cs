using System;
using ApartmentAfterDark.Core;
using UnityEngine;

namespace ApartmentAfterDark.Camera
{
    /// <summary>
    /// Player camera controller. Supports first-person (default) and an optional
    /// third-person orbit mode toggled with C. Mouse look with configurable sensitivity,
    /// pitch clamp and cursor lock. Attach to the Camera used as the player's eye.
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("Modes")]
        [Tooltip("If true, third-person orbit mode is enabled (toggle with C).")]
        [SerializeField] private bool enableThirdPerson = false;

        [Tooltip("Starts in first-person.")]
        [SerializeField] private bool startInFirstPerson = true;

        [Header("First Person")]
        [Tooltip("Object that yaws with the mouse (the player body). Auto-detected if empty.")]
        [SerializeField] private Transform yawTarget;

        [Tooltip("Invert vertical look.")]
        [SerializeField] private bool invertY = false;

        [Header("Sensitivity")]
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField, Range(-90f, 0f)] private float minPitch = -80f;
        [SerializeField, Range(0f, 90f)] private float maxPitch = 80f;

        [Header("Third Person")]
        [Tooltip("Distance behind the player in third-person mode.")]
        [SerializeField] private float orbitDistance = 3f;

        [Tooltip("Camera height offset in third-person mode.")]
        [SerializeField] private float orbitHeight = 1.5f;

        [Tooltip("Layers that occlude the camera (for third-person collision).")]
        [SerializeField] private LayerMask cameraIgnoreMask = ~0;

        [Header("Cursor")]
        [Tooltip("Locks and hides the cursor on start.")]
        [SerializeField] private bool lockCursor = true;

        private float yaw;
        private float pitch;
        private bool isFirstPerson;
        private Vector3 orbitPos;
        private Quaternion desiredOrbitRot;
        private UnityEngine.Camera cam;

        public bool IsFirstPerson => isFirstPerson;
        public event Action<bool> OnModeChanged;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();

            // If no explicit yaw body is assigned, yaw the camera's parent (the player body)
            // so horizontal look rotates the body and WASD follows the camera direction.
            if (yawTarget == null && transform.parent != null)
            {
                yawTarget = transform.parent;
            }

            if (yawTarget == null && !enableThirdPerson)
            {
                // In pure first-person without a parent body, pitch+yaw both on the camera.
                InitializeFromCurrent();
            }
            else if (yawTarget != null)
            {
                Vector3 e = yawTarget.eulerAngles;
                yaw = e.y;
                isFirstPerson = startInFirstPerson;
            }
            else
            {
                InitializeFromCurrent();
            }
        }

        private void InitializeFromCurrent()
        {
            Vector3 e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x;
            isFirstPerson = true;
        }

        private void Start()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            HandleCursorLock();
            HandleModeToggle();
            ApplyLookInput();
        }

        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleModeToggle()
        {
            if (!enableThirdPerson) return;
            if (Input.GetKeyDown(KeyCode.C))
            {
                isFirstPerson = !isFirstPerson;
                OnModeChanged?.Invoke(isFirstPerson);
            }
        }

        private void ApplyLookInput()
        {
            if (!GameManager.Instance || GameManager.Instance.CurrentState.AllowsPlayerControl())
            {
                float mx = Input.GetAxis("Mouse X") * lookSensitivity;
                float my = Input.GetAxis("Mouse Y") * lookSensitivity * (invertY ? -1f : 1f);

                yaw += mx;
                pitch = Mathf.Clamp(pitch - my, minPitch, maxPitch);
            }

            if (isFirstPerson)
            {
                // Pitch onto the camera, yaw onto the yaw target (player) if provided
                transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                if (yawTarget != null)
                    yawTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
                else
                    transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            else
            {
                ApplyOrbit();
            }
        }

        private void ApplyOrbit()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPos = transform.position
                - rot * Vector3.forward * orbitDistance
                + Vector3.up * orbitHeight;

            // Simple camera snap/clamp: third-person keeps the camera on a pivot above the player
            Vector3 pivot = transform.position + Vector3.up * orbitHeight;
            Vector3 dir = (rot * Vector3.forward);

            if (Physics.Raycast(pivot, -dir, out RaycastHit hit, orbitDistance, cameraIgnoreMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = pivot - dir * (hit.distance - 0.2f);
            }
            else
            {
                transform.position = pivot - dir * orbitDistance;
            }

            transform.LookAt(pivot);
        }
    }
}
