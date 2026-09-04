using System.Collections;
using ApartmentAfterDark.Audio;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ApartmentAfterDark.Characters
{
    /// <summary>
    /// Simple NPC that patrols between waypoints using NavMeshAgent, plays Idle/Walk
    /// animations via Animator, optionally plays a looping ambient voice and reacts to
    /// the player in range. Works without NavMesh baked (falls back to teleport-free
    /// movement if no NavMesh is available).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CharacterControllerNPC : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Walk speed.")]
        [SerializeField] private float walkSpeed = 1.5f;

        [Tooltip("Waypoints visited in order. Empty = stays put.")]
        [SerializeField] private List<Transform> waypoints;

        [Tooltip("Wait time at each waypoint (seconds).")]
        [SerializeField] private float waitAtWaypoint = 2f;

        [Header("Animator")]
        [Tooltip("Animator with parameters: Speed (float). Auto-detected.")]
        [SerializeField] private Animator animator;

        [Header("Audio / Voice")]
        [Tooltip("Optional looping ambient voice/breathing clip.")]
        [SerializeField] private AudioClip loopVoice;

        [Tooltip("Volume of the looped voice (0-1).")]
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.4f;

        [Header("Detection (optional)")]
        [Tooltip("If > 0, the NPC notices the player within this radius.")]
        [SerializeField] private float noticeRadius = 4f;

        [SerializeField] private LayerMask playerMask = ~0;

        private NavMeshAgent agent;
        private int waypointIndex;
        private AudioSource voiceSource;
        private float speed;

        public float CurrentSpeed => speed;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            voiceSource = gameObject.GetComponent<AudioSource>();
            if (voiceSource == null)
                voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = true;
            voiceSource.volume = voiceVolume;
            voiceSource.spatialBlend = 1f;
        }

        private void Start()
        {
            ConfigureAgent();
            if (waypoints.Count > 0)
                MoveToNext();
            if (loopVoice != null)
                voiceSource.Play();
        }

        private void ConfigureAgent()
        {
            agent.speed = walkSpeed;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = 0.2f;
        }

        private void Update()
        {
            if (agent.enabled && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
            {
                StartCoroutine(WaitAndMove());
            }

            // Update animation speed
            speed = agent.enabled && agent.hasPath ? agent.desiredVelocity.magnitude : 0f;
            if (animator != null)
                animator.SetFloat("Speed", speed);

            // Face player when noticed (optional)
            if (noticeRadius > 0f)
                DetectPlayer();
        }

        private IEnumerator WaitAndMove()
        {
            yield return new WaitForSeconds(waitAtWaypoint);
            MoveToNext();
        }

        private void MoveToNext()
        {
            if (waypoints.Count == 0) return;
            waypointIndex = (waypointIndex + 1) % waypoints.Count;
            Transform wp = waypoints[waypointIndex];
            if (wp == null) return;
            agent.SetDestination(wp.position);
        }

        private void DetectPlayer()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, noticeRadius, playerMask);
            foreach (Collider c in hits)
            {
                if (c.CompareTag("Player") || c.GetComponent<Player.PlayerController>() != null)
                {
                    Vector3 dir = c.transform.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
                    break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (noticeRadius <= 0f) return;
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, noticeRadius);
        }
    }
}
