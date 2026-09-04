using ApartmentAfterDark.Core;
using ApartmentAfterDark.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ApartmentAfterDark.UI
{
    /// <summary>
    /// Pause menu. Opens on Esc/P, time-scales to 0, offers Resume, Save and Quit.
    /// Wire Buttons or let the scene builder auto-connect by name.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("Canvas/panel shown while paused.")]
        [SerializeField] private Canvas pauseCanvas;

        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button quitButton;

        [Tooltip("Player transform to record position when saving.")]
        [SerializeField] private Transform playerTransform;

        private bool isPaused;

        private void Awake()
        {
            if (pauseCanvas == null) pauseCanvas = GetComponent<Canvas>();
            if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
        }

        private void Start()
        {
            TryFindButtons();
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (saveButton != null) saveButton.onClick.AddListener(SaveGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (isPaused) Resume();
                else Pause();
            }
        }

        private void TryFindButtons()
        {
            if (pauseCanvas == null) return;
            if (resumeButton == null) resumeButton = pauseCanvas.transform.Find("ResumeButton")?.GetComponent<Button>();
            if (saveButton == null) saveButton = pauseCanvas.transform.Find("SaveButton")?.GetComponent<Button>();
            if (quitButton == null) quitButton = pauseCanvas.transform.Find("QuitButton")?.GetComponent<Button>();
        }

        public void Pause()
        {
            if (isPaused) return;
            isPaused = true;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Paused);
            if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            if (!isPaused) return;
            isPaused = false;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SaveGame()
        {
            GameSaveData data = new GameSaveData
            {
                playerPosition = playerTransform != null ? playerTransform.position : Vector3.zero,
                playerHealth = 100f,
                sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            };
            SaveSystem.SaveGame(data);
        }

        public void QuitToMenu()
        {
            Resume();
            if (GameManager.Instance != null)
                GameManager.Instance.LoadScene("MainMenu");
        }
    }
}
