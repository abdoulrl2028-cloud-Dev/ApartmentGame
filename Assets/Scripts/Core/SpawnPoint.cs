using UnityEngine;

namespace ApartmentAfterDark
{
    /// <summary>
    /// Marks a position where the player can spawn/respawn.
    /// Place on an empty GameObject at the desired location.
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Facing direction of the spawned player (Yaw degrees).")]
        [SerializeField] private float yaw = 0f;

        public float Yaw => yaw;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.25f);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Gizmos.DrawRay(transform.position, dir * 1f);
        }
    }
}
