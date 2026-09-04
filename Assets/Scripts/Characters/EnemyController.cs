using System.Collections;
using UnityEngine;

namespace ApartmentAfterDark.Characters
{
    /// <summary>
    /// A lightweight apartment enemy that patrols waypoints with a CharacterController
    /// (no NavMesh needed), detects the player by distance + field-of-view + line of
    /// sight, chases and attacks the player, has its own health and dies when killed.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Movement & Patrolling")]
        [Tooltip("Waypoints visited in order. Empty = stand still.")]
        [SerializeField] private Transform[] waypoints;

        [Tooltip("Patrol speed.")]
        [SerializeField] private float patrolSpeed = 2f;

        [Tooltip("Chase speed.")]
        [SerializeField] private float chaseSpeed = 4.2f;

        [Tooltip("Idle time (seconds) at each waypoint.")]
        [SerializeField] private float waitAtWaypoint = 1.5f;

        [Tooltip("How far (deg) the enemy can see around its forward axis.")]
        [SerializeField, Range(0f, 180f)] private float detectionAngle = 100f;

        [Header("Detection / Attack")]
        [Tooltip("Radius within which the player is sensed at all.")]
        [SerializeField] private float detectionRadius = 8f;

        [Tooltip("Distance at which the enemy starts attacking.")]
        [SerializeField] private float attackRange = 1.6f;

        [Tooltip("Damage dealt per hit.")]
        [SerializeField] private int attackDamage = 12;

        [Tooltip("Seconds between attacks.")]
        [SerializeField] private float attackCooldown = 1.2f;

        [Tooltip("Stops the enemy from flying through walls: vertical offset used by raycasts per frame.")]
        [SerializeField] private float eyeHeight = 1.5f;

        [Header("Health")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Visuals")]
        [Tooltip("Renderer tinted red while chasing, dark while patrolling.")]
        [SerializeField] private Renderer bodyRenderer;

        [Tooltip("If true, the object is destroyed visually on death (enabled flag off + collider off).")]
        [SerializeField] private bool hideOnDeath = true;

        private CharacterController cc;
        private Transform player;
        private int waypointIndex;
        private float speedRef;
        private float attackTimer;
        private float waitTimer;
        private bool chasing;

        private enum State { Patrol, Chase }
        private State state = State.Patrol;

        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            if (waypoints != null && waypoints.Length > 0)
                MoveToNext();
        }

        private void Update()
        {
            if (!IsAlive) return;

            player = FindPlayer();
            chasing = player != null && CanSee(player);

            state = chasing ? State.Chase : State.Patrol;

            if (state == State.Patrol)
                PatrolUpdate();
            else
                ChaseUpdate();

            UpdateVisual();
        }

        private void PatrolUpdate()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Transform target = waypoints[waypointIndex];
            if (target == null) return;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > 0.3f)
            {
                MoveTowards(target.position, patrolSpeed);
            }
            else
            {
                speedRef = 0f;
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitAtWaypoint)
                {
                    waitTimer = 0f;
                    MoveToNext();
                }
            }
        }

        private void ChaseUpdate()
        {
            if (player == null) return;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (dist > attackRange)
            {
                MoveTowards(player.position, chaseSpeed);
                FaceTowards(player.position);
            }
            else
            {
                speedRef = 0f;
                FaceTowards(player.position);
                TryAttack();
            }
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            cc.Move(dir * speed * Time.deltaTime);
            speedRef = speed;
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir.normalized), 8f * Time.deltaTime);
        }

        private void TryAttack()
        {
            attackTimer += Time.deltaTime;
            if (attackTimer < attackCooldown) return;
            attackTimer = 0f;

            if (player == null) return;

            var hp = player.GetComponent<Player.PlayerHealth>();
            if (hp != null) hp.ApplyDamage(attackDamage);
        }

        private void MoveToNext()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }

        private Transform FindPlayer()
        {
            if (player != null) return player;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            return p != null ? p.transform : null;
        }

        private bool CanSee(Transform target)
        {
            Vector3 toTarget = target.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist > detectionRadius) return false;

            Vector3 flat = toTarget;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return true;

            // FOV cone
            float ang = Vector3.Angle(transform.forward, flat.normalized);
            if (ang > detectionAngle * 0.5f) return false;

            // LOS
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPoint = target.position + Vector3.up * 1f;
            if (Physics.Linecast(origin, targetPoint, out RaycastHit hit))
            {
                return hit.transform == target
                    || hit.transform.IsChildOf(target)
                    || hit.collider.CompareTag("Player");
            }
            return true;
        }

        private void UpdateVisual()
        {
            if (bodyRenderer == null) return;
            var mat = bodyRenderer.material;
            if (mat == null) return;
            mat.color = chasing
                ? new Color(1f, 0.18f, 0.12f)
                : new Color(0.08f, 0.08f, 0.1f);
        }

        /// <summary>Applies damage. Returns true if the enemy died from it.</summary>
        public bool TakeDamage(int amount)
        {
            if (!IsAlive) return false;
            currentHealth = Mathf.Max(0, currentHealth - amount);
            chasing = true; // getting hit reveals you
            if (currentHealth <= 0)
            {
                Die();
                return true;
            }
            return false;
        }

        private void Die()
        {
            if (hideOnDeath)
            {
                foreach (Collider c in GetComponentsInChildren<Collider>())
                    c.enabled = false;
                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                    r.enabled = false;
                if (bodyRenderer != null) bodyRenderer.enabled = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
