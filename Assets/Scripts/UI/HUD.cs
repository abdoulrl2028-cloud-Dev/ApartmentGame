using ApartmentAfterDark.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ApartmentAfterDark.UI
{
    /// <summary>
    /// HUD showing health (and optionally interaction prompt text). Connect the prompt
    /// Text and health Slider in the Inspector or let the scene builder auto-connect.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Tooltip("Text showing the current interaction prompt. Hidden when empty.")]
        [SerializeField] private Text promptText;

        [Tooltip("Optional health slider (0-1 value curve).")]
        [SerializeField] private Slider healthSlider;

        [Tooltip("Player reference for health reading. Auto-found if empty.")]
        [SerializeField] private PlayerHealth playerHealth;

        [Tooltip("Default prompt text shown while aiming at an interactable.")]
        [SerializeField] private string promptPrefix = "Press E to ";

        private string prompt;

        private void Awake()
        {
            ClearPrompt();
        }

        /// <summary>Assigns the prompt text element (called by scene builders).</summary>
        public void SetPromptText(Text text)
        {
            promptText = text;
            ClearPrompt();
        }

        private void Start()
        {
            if (playerHealth == null)
                playerHealth = FindObjectOfType<PlayerHealth>();
        }

        public void ShowPrompt(string message)
        {
            prompt = message;
            if (promptText != null)
            {
                promptText.text = string.IsNullOrEmpty(message) ? string.Empty : promptPrefix + message;
                promptText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        public void ClearPrompt()
        {
            prompt = string.Empty;
            if (promptText != null)
            {
                promptText.text = string.Empty;
                promptText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (playerHealth != null && healthSlider != null)
                healthSlider.value = playerHealth.HealthNormalized;
        }
    }
}
