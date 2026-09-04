using UnityEngine;

namespace ApartmentAfterDark.Player
{
    /// <summary>
    /// Third-person / first-person movement using Unity's CharacterController.
    /// Uses the legacy Input Manager (WASD), with sprint, jump and gravity.
    /// Attach to the Player GameObject along with a CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Walk speed in units/second.")]
        [SerializeField] private float walkSpeed = 4f;

        [Tooltip("Sprint speed in units/second.")]
        [SerializeField] private float sprintSpeed = 7f;

        [Tooltip("Jump upward velocity.")]
        [SerializeField] private float jumpHeight = 1.2f;

        [Tooltip("Gravity applied each frame.")]
        [SerializeField] private float gravity = -20f;

        [Tooltip("Air control multiplier (0-1).")]
        [SerializeField, Range(0f, 1f)] private float airControl = 0.5f;

        [Header("Crouch")]
        [Tooltip("Controller height while standing.")]
        [SerializeField] private float standHeight = 1.8f;
        [Tooltip("Controller height while crouching.")]
        [SerializeField] private float crouchHeight = 1.1f;
        [Tooltip("Walk speed while crouching.")]
        [SerializeField] private float crouchSpeed = 2f;
        [Tooltip("Rate at which the controller/camera lerp between stand and crouch.")]
        [SerializeField] private float crouchLerpSpeed = 12f;
        [Tooltip("Eye height (camera child local Y) while standing. Lowered when crouching.")]
        [SerializeField] private float standEyeHeight = 1.6f;
        [Tooltip("Eye height while crouching.")]
        [SerializeField] private float crouchEyeHeight = 0.9f;

        [Header("Stamina (optional)")]
        [Tooltip("If true, sprint consumes stamina and stops when empty.")]
        [SerializeField] private bool useStamina = false;

        [Tooltip("Max stamina (0-100).")]
        [SerializeField] private float maxStamina = 100f;

        [Tooltip("Stamina drained per second while sprinting.")]
        [SerializeField] private float staminaDrainRate = 20f;

        [Tooltip("Stamina regenerated per second at rest/walk.")]
        [SerializeField] private float staminaRegenRate = 12f;

        [Header("Movement Orientation")]
        [Tooltip("If true, rotates the player to face the camera forward (3rd person orbit).")]
        [SerializeField] private bool rotateWithCamera = false;

        [Header("State (Read Only)")]
        [SerializeField] private float speed;
        [SerializeField] private float currentStamina;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;
        private float eyeHeight;
        private Transform eye;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float CurrentSpeed => speed;
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public float StaminaNormalized => maxStamina <= 0 ? 1f : currentStamina / maxStamina;
        public CharacterController Controller => controller;
        public Vector3 HorizontalVelocity => new Vector3(velocity.x, 0f, velocity.z);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentStamina = maxStamina;
            eyeHeight = standEyeHeight;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<UnityEngine.Camera>() != null)
                {
                    eye = transform.GetChild(i);
                    break;
                }
            }
        }

        private void Start()
        {
            if (UnityEngine.Camera.main != null && rotateWithCamera)
            {
                Vector3 fwd = UnityEngine.Camera.main.transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(fwd.normalized);
            }
        }

        private void Update()
        {
            HandleGravity();
            HandleCrouch();
            HandleMovementInput();
            UpdateStamina();
            ApplyVelocity();
        }

        private void HandleCrouch()
        {
            bool wantCrouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            isCrouching = wantCrouch && isGrounded;

            float targetHeight = isCrouching ? crouchHeight : standHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, crouchLerpSpeed * Time.deltaTime);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

            float goalEye = isCrouching ? crouchEyeHeight : standEyeHeight;
            eyeHeight = Mathf.Lerp(eyeHeight, goalEye, crouchLerpSpeed * Time.deltaTime);
            if (eye != null)
            {
                Vector3 p = eye.localPosition;
                p.y = eyeHeight;
                eye.localPosition = p;
            }
        }

        private void HandleMovementInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && v > 0.1f && (!useStamina || currentStamina > 0f);
            isSprinting = wantsSprint && !isCrouching;

            Vector3 move = Vector3.zero;

            if (rotateWithCamera && UnityEngine.Camera.main != null)
            {
                // Move relative to camera yaw (3rd person)
                Vector3 camForward = UnityEngine.Camera.main.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();
                Vector3 camRight = UnityEngine.Camera.main.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                move = (camForward * v + camRight * h).normalized;
            }
            else
            {
                // Move relative to player's own facing
                move = (transform.forward * v + transform.right * h).normalized;
            }

            float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            float control = isGrounded ? 1f : airControl;

            Vector3 targetVelocity = move * targetSpeed;
            velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, control * 10f * Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, control * 10f * Time.deltaTime);

            speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Rotate player toward movement direction when relevant
            if (move.sqrMagnitude > 0.01f && rotateWithCamera)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(move), 12f * Time.deltaTime);
            }
        }

        private void HandleGravity()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            velocity.y += gravity * Time.deltaTime;
        }

        private void ApplyVelocity()
        {
            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateStamina()
        {
            if (!useStamina) return;

            if (isSprinting)
                currentStamina = Mathf.Max(0f, currentStamina - staminaDrainRate * Time.deltaTime);
            else
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        }

        /// <summary>Teleports the player to a world position (e.g. respawn).</summary>
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            velocity = Vector3.zero;
            controller.enabled = true;
        }
    }
}
