using System;
using UnityEngine;

namespace ApartmentAfterDark.Player
{
    /// <summary>
    /// Simple health component for the player. Emits events on damage/death.
    /// Death triggers a respawn via the registered respawn point (or origin).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Damage/Invulnerability")]
        [Tooltip("Seconds of invulnerability after taking damage.")]
        [SerializeField] private float invulnerabilityDuration = 0.5f;

        [Tooltip("If true, re-enables invulnerability visuals via a blinking renderer.")]
        [SerializeField] private bool flashOnHit = true;

        [Header("Respawn")]
        [Tooltip("If true, the player respawns instead of the game ending.")]
        [SerializeField] private bool respawnOnDeath = true;

        private float lastDamageTime = -999f;

        public event Action<int, int> OnHealthChanged;   // (current, max)
        public event Action OnDamaged;
        public event Action OnDied;
        public event Action OnRespawned;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float HealthNormalized => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInvulnerable => invulnerabilityDuration > 0f && Time.time - lastDamageTime < invulnerabilityDuration;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        /// <summary>Applies damage, respecting invulnerability. Returns true if damage was applied.</summary>
        public bool ApplyDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return false;
            if (IsInvulnerable) return false;

            lastDamageTime = Time.time;
            currentHealth = Mathf.Max(0, currentHealth - amount);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke();

            if (currentHealth <= 0)
                Die();

            return true;
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void RestoreFull()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            OnDied?.Invoke();

            if (respawnOnDeath)
                Respawn();
        }

        private void Respawn()
        {
            SpawnPoint point = FindObjectOfType<SpawnPoint>();
            Vector3 pos = point != null ? point.transform.position : Vector3.zero;

            var controller = GetComponent<PlayerController>();
            if (controller != null)
                controller.Teleport(pos);

            RestoreFull();
            OnRespawned?.Invoke();
        }
    }
}
